using UnityEngine;
using System;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class StatsModule : IUnitModule
    {
        // Todos os StatType uma vez (evita boxing de Enum.GetValues por unidade na init).
        private static readonly StatType[] AllStatTypes = (StatType[])Enum.GetValues(typeof(StatType));
        // O catálogo é um asset global; resolvido uma vez via GameConfig e compartilhado entre unidades.
        private static StatDefinitionCatalog sharedCatalog;

        private UnitController controller;
        private StatDefinitionCatalog catalog;

        // Cache do array de tipos por referência de CharacterData: só realoca quando a unidade
        // passa a representar outro personagem (reuso de pool), não a cada cálculo de dano.
        private CharacterData cachedTypesData;
        private CharacterType[] cachedTypes = Array.Empty<CharacterType>();

        private Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();
        private Dictionary<ResourceType, Resource> resources = new Dictionary<ResourceType, Resource>();
        // Compartilhamento de dano num módulo reutilizável próprio (não é lógica do Guliver).
        private readonly DamageShareRegistry damageShares = new DamageShareRegistry();

        // Modificadores de dano (regras universais de combate, ex.: passiva da Marda; itens).
        // Cada entrada é um multiplicador avaliado no momento do cálculo de dano, recebendo o
        // DamageContext (fonte/alvo + tags) para regras condicionais.
        private readonly Dictionary<string, Func<DamageContext, float>> outgoingDamageModifiers = new Dictionary<string, Func<DamageContext, float>>();
        private readonly Dictionary<string, Func<DamageContext, float>> incomingDamageModifiers = new Dictionary<string, Func<DamageContext, float>>();

        public event Action OnDeath;
        public bool IsDead { get; private set; }

        // === Acesso rápido ===
        public float Attack => GetStat(StatType.Attack);
        public float Defense => GetStat(StatType.Defense);
        public float Speed => GetStat(StatType.Speed);
        public float Range => GetStat(StatType.Range);
        public float MaxHealth => GetStat(StatType.MaxHealth);
        public float CurrentHealth => GetResource(ResourceType.Health);
        public float MaxEnergy => GetStat(StatType.MaxEnergy);
        public float CurrentEnergy => GetResource(ResourceType.Energy);
        public float EnergyRegeneration => GetStat(StatType.EnergyRegeneration);
        public float CritRate => GetStat(StatType.CritRate);
        public float CritDamage => GetStat(StatType.CritDamage);
        public float CritDamageResistance => GetStat(StatType.CritDamageResistance);
        public float Penetration => GetStat(StatType.Penetration);
        public float PenetrationResistance => GetStat(StatType.PenetrationResistance);
        public float DamageBonus => GetStat(StatType.DamageBonus);
        public float DamageReduction => GetStat(StatType.DamageReduction);
        public float Effectiveness => GetStat(StatType.Effectiveness);
        public float EffectivenessResistance => GetStat(StatType.EffectivenessResistance);
        public float Control => GetStat(StatType.Control);
        public float Tenacity => GetStat(StatType.Tenacity);
        public float Lifesteal => GetStat(StatType.Lifesteal);
        public float Accuracy => GetStat(StatType.Accuracy);
        public float Dodge => GetStat(StatType.Dodge);

        // Acesso à unidade/categorias para regras condicionais de dano.
        public UnitController Controller => controller;
        public UnitTag Tags => controller != null ? controller.Tags : UnitTag.None;

        /// <summary>Catálogo de definições de stat (ícones/cores/formato). Compartilhado entre unidades.</summary>
        public StatDefinitionCatalog Catalog => catalog;

        public CharacterType[] GetTypes()
        {
            var data = controller.GetCharacterData();
            if (data == null) return Array.Empty<CharacterType>();
            if (!ReferenceEquals(data, cachedTypesData))
            {
                cachedTypes = data.secondaryType == CharacterType.None
                    ? new[] { data.primaryType }
                    : new[] { data.primaryType, data.secondaryType };
                cachedTypesData = data;
            }
            return cachedTypes;
        }

        // === IUnitModule ===

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            if (sharedCatalog == null)
                sharedCatalog = GameConfig.Active?.StatDefinitions;
            catalog = sharedCatalog;

            var charData = controller.GetCharacterData();
            InitializeStats(charData);
            InitializeResources();

            resources[ResourceType.Health].OnDepleted += HandleDeath;
        }

        public void OnEnabled() { }

        public void OnDisabled() { }

        public void Cleanup()
        {
            if (resources.ContainsKey(ResourceType.Health))
            {
                resources[ResourceType.Health].OnDepleted -= HandleDeath;
            }
            stats.Clear();
            resources.Clear();
            damageShares.Clear();
            outgoingDamageModifiers.Clear();
            incomingDamageModifiers.Clear();
            controller = null;
        }

        // === Inicialização ===

        private void InitializeStats(CharacterData data)
        {
            foreach (StatType type in AllStatTypes)
            {
                var def = catalog != null ? catalog.GetStatDefinition(type) : null;
                float baseValue = StatBudget.ComputeBaseStat(data, type);
                stats[type] = new Stat(type, baseValue, def);
            }
        }

        private void InitializeResources()
        {
            CreateResource(ResourceType.Health, StatType.MaxHealth);
            CreateResource(ResourceType.Energy, StatType.MaxEnergy);
        }

        private void CreateResource(ResourceType type, StatType maxStatType)
        {
            var definition = catalog?.GetResourceDefinition(type);
            resources[type] = new Resource(type, stats[maxStatType], definition);
        }

        // === API Pública ===

        public float GetStat(StatType type)
        {
            return stats.TryGetValue(type, out var stat) ? stat.CurrentValue : 0f;
        }

        public float GetResource(ResourceType type)
        {
            return resources.TryGetValue(type, out var resource) ? resource.CurrentValue : 0f;
        }

        public Stat GetStatObject(StatType type)
        {
            return stats.TryGetValue(type, out var stat) ? stat : null;
        }

        public Resource GetResourceObject(ResourceType type)
        {
            return resources.TryGetValue(type, out var resource) ? resource : null;
        }

        public void ModifyStat(StatType type, float amount)
        {
            if (stats.TryGetValue(type, out var stat))
            {
                stat.Modify(amount);
            }
        }

        // === DamageShare ===

        // Delegam ao DamageShareRegistry (módulo reutilizável) — qualquer unidade registra o seu share.
        /// <summary>Damage share com protetor fixo.</summary>
        public void AddDamageShare(string id, StatsModule protector, float value) => damageShares.Add(id, protector, value);

        /// <summary>Damage share com protetor resolvido no momento do dano (ex.: "a invocação com mais vida").</summary>
        public void AddDamageShare(string id, Func<StatsModule> protectorResolver, float value) => damageShares.Add(id, protectorResolver, value);

        public void RemoveDamageShare(string id) => damageShares.Remove(id);

        public bool HasDamageShare(string id) => damageShares.Has(id);

        public void ClearDamageShares() => damageShares.Clear();

        // === Modificadores de dano (regras universais) ===

        /// <summary>Registra um multiplicador aplicado ao dano que ESTA unidade causa. Recebe o DamageContext.</summary>
        public void AddOutgoingDamageModifier(string id, Func<DamageContext, float> multiplier) => outgoingDamageModifiers[id] = multiplier;

        public void RemoveOutgoingDamageModifier(string id) => outgoingDamageModifiers.Remove(id);

        /// <summary>Registra um multiplicador aplicado ao dano que ESTA unidade recebe. Recebe o DamageContext.</summary>
        public void AddIncomingDamageModifier(string id, Func<DamageContext, float> multiplier) => incomingDamageModifiers[id] = multiplier;

        public void RemoveIncomingDamageModifier(string id) => incomingDamageModifiers.Remove(id);

        public float GetOutgoingDamageMultiplier(DamageContext ctx) => AggregateDamage(outgoingDamageModifiers, ctx);

        public float GetIncomingDamageMultiplier(DamageContext ctx) => AggregateDamage(incomingDamageModifiers, ctx);

        private static float AggregateDamage(Dictionary<string, Func<DamageContext, float>> source, DamageContext ctx)
        {
            if (source.Count == 0) return 1f;
            float result = 1f;
            foreach (var modifier in source.Values)
                result *= modifier(ctx);
            return result;
        }

        // === Cura e escudo (por stat, base 100: 0 = 100%, aditivo; clampado >= 0) ===

        // Multiplicador percentual de um stat base-100 (0 → 1.0; +20 → 1.2; -20 → 0.8; nunca negativo).
        private float PercentMultiplier(StatType type)
        {
            float v = 1f + GetStat(type) / 100f;
            return v < 0f ? 0f : v;
        }

        /// <summary>Escala uma cura que ESTA unidade CAUSA pelo HealBonus (origem). Não aplica em ninguém.</summary>
        public float CauseHeal(float rawAmount) => rawAmount <= 0f ? 0f : rawAmount * PercentMultiplier(StatType.HealBonus);

        /// <summary>Escala um escudo que ESTA unidade CONCEDE pelo ShieldBonus (origem). Não aplica em ninguém.</summary>
        public float CauseShield(float rawAmount) => rawAmount <= 0f ? 0f : rawAmount * PercentMultiplier(StatType.ShieldBonus);

        /// <summary>Escala um escudo que ESTA unidade RECEBE pelo ShieldReceived (destino). O ShieldModule armazena.</summary>
        public float ReceiveShield(float amount) => amount <= 0f ? 0f : amount * PercentMultiplier(StatType.ShieldReceived);

        // === Dano ===

        public void ApplyDamage(DamageResult result)
        {
            if (IsDead) return;

            float damageToSelf = result.finalDamage;
            float damageToProtector = 0f;
            StatsModule protector = null;

            // Redireciona parte do dano a um protetor. O dano compartilhado é aplicado abaixo com
            // ignoreDamageShare=true → NUNCA é re-compartilhado (evita loop entre protetores mútuos).
            if (!result.ignoreDamageShare && damageShares.TryResolveProtector(this, out var resolvedProtector, out float shareFraction))
            {
                damageToProtector = result.finalDamage * shareFraction;
                damageToSelf = result.finalDamage - damageToProtector;
                protector = resolvedProtector;
            }

            // Escudos absorvem ANTES da vida (módulo próprio — ver ShieldModule). O que sobrar fere.
            var shields = controller.GetModule<ShieldModule>();
            if (shields != null) damageToSelf = shields.AbsorbDamage(damageToSelf);

            resources[ResourceType.Health].Remove(damageToSelf);
            controller.GetModule<VisualModule>()?.PlayDamageFlash();
            controller.GetModule<SkillsModule>()?.NotifyDamageTaken(damageToSelf);

            if (protector != null && damageToProtector > 0)
            {
                var sharedResult = new DamageResult
                {
                    rawDamage = damageToProtector,
                    finalDamage = damageToProtector,
                    isCritical = false,
                    ignoreDamageShare = true
                };
                protector.ApplyDamage(sharedResult);
            }

            if (DebugManager.IsEnabled(DebugCategory.Combat))
            {
                string critTag = result.isCritical ? " [CRIT]" : "";
                string shareTag = protector != null ? $" [SHARE:{damageToProtector:F0}→Protector]" : "";
                DebugManager.Log($"ApplyDamage: Final:{result.finalDamage} Self:{damageToSelf}{shareTag}{critTag} - HP: {CurrentHealth}/{MaxHealth}", DebugCategory.Combat);
            }
        }

        /// <summary>
        /// Aplica cura a ESTA unidade (destino), escalada pelo HealReceived. É o "receber cura" do
        /// módulo — a origem já aplicou o HealBonus via <see cref="CauseHeal"/> (ver HealEffect,
        /// lifesteal no DamageApplier, passiva do Rikurby).
        /// </summary>
        public void ReceiveHeal(float amount)
        {
            amount *= PercentMultiplier(StatType.HealReceived);
            if (amount <= 0f) return;

            resources[ResourceType.Health].Add(amount);
            controller.GetModule<SkillsModule>()?.NotifyHealReceived(amount);
            if (DebugManager.IsEnabled(DebugCategory.Combat))
                DebugManager.Log($"ReceiveHeal({amount}) - HP: {CurrentHealth}/{MaxHealth}", DebugCategory.Combat);
        }

        private void HandleDeath()
        {
            if (IsDead) return;

            IsDead = true;
            DebugManager.Log("Unidade morreu!", DebugCategory.Combat);

            controller.GetModule<SkillsModule>()?.NotifyUnitDeath();
            controller.GetModule<CombatModule>()?.SetState(CombatState.Dead);
            OnDeath?.Invoke();
            CombatController.Instance?.OnUnitDied(controller);
        }

        public void ResetForPool()
        {
            foreach (var stat in stats.Values)
                stat.Reset();

            foreach (var resource in resources.Values)
                resource.SetToMax();

            damageShares.Clear();
            outgoingDamageModifiers.Clear();
            incomingDamageModifiers.Clear();
            IsDead = false;
        }

        public void ApplyCharacterStats(CharacterData data)
        {
            if (data == null) return;

            foreach (var stat in stats.Values)
                stat.SetBaseValue(StatBudget.ComputeBaseStat(data, stat.Type));

            foreach (var resource in resources.Values)
                resource.SetToMax();
        }

        /// <summary>
        /// Aplica a build da unidade em combate: pontos da árvore (até o rank) + grants do artefato como
        /// UM modifier "loadout" por stat (flat = pontos × statPointValue, mesmo balde da base; percent no
        /// fold). Usa <see cref="StatPreviewCalculator.TreePoints"/> → preview == combate. Removido no
        /// retorno ao pool (Stat.Reset limpa modifiers). Não seta vida — o caller decide (LoadoutModule).
        /// </summary>
        public void ApplyBuildStats(Data.CharacterBuild build, StatTreeData tree, ArtifactData artifact, int rank)
        {
            foreach (StatType type in AllStatTypes)
            {
                float points = StatPreviewCalculator.TreePoints(build, tree, type, rank);
                float percent = 0f;
                if (artifact != null && artifact.statGrants != null)
                    for (int i = 0; i < artifact.statGrants.Length; i++)
                        if (artifact.statGrants[i].stat == type)
                        {
                            points += artifact.statGrants[i].points;
                            percent += artifact.statGrants[i].percent;
                        }

                if (points == 0f && percent == 0f) continue;
                GetStatObject(type)?.AddModifier("loadout", StatBudget.PointsToValue(type, points), percent);
            }
        }

        // === Combate ===

        public void PrepareForCombat()
        {
            resources[ResourceType.Energy].SetToMin();
        }

        public void RegenerateEnergy(float deltaTime)
        {
            if (IsDead) return;

            float regenAmount = EnergyRegeneration * deltaTime;
            resources[ResourceType.Energy].Add(regenAmount);
        }
    }
}
