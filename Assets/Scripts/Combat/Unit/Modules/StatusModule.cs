using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Efeitos de status em tempo real (buffs/debuffs/condições). Tickado pelo CombatModule.
    /// Responsável por: empilhamento por tipo (<see cref="StackMode"/>), imunidade de Condição a
    /// limpeza/dispel, e expiração modular (por duração e por gatilhos do portador via
    /// <see cref="PassiveHooks"/>). Aplica/reverte modificadores de stat por INSTÂNCIA
    /// (<see cref="StatusEffect.InstanceKey"/>) para que várias instâncias do mesmo tipo coexistam.
    /// </summary>
    public class StatusModule : IUnitModule
    {
        private UnitController controller;
        private StatsModule stats;
        private readonly List<StatusEffect> active = new List<StatusEffect>();

        /// <summary>Quantidade de debuffs ativos (cada instância conta). Base para regras universais.</summary>
        public int DebuffCount { get; private set; }

        /// <summary>Disparado quando a lista de efeitos muda (aplicar/renovar/remover) — para widgets.</summary>
        public event System.Action OnEffectsChanged;

        public IReadOnlyList<StatusEffect> ActiveEffects => active;

        private int untargetableCount;
        private int controlCount;
        private StatusEffect forcedFocusEffect;

        // Hooks do portador aos quais estamos inscritos para expiração por gatilho (reassinado por combate).
        private PassiveHooks subscribedHooks;

        public bool IsTargetable => untargetableCount == 0;

        public bool TryGetForcedFocus(out TargetQuery query, out UnitController applier)
        {
            if (forcedFocusEffect != null && forcedFocusEffect.ForcedFocus.HasValue)
            {
                query = forcedFocusEffect.ForcedFocus.Value;
                applier = forcedFocusEffect.AppliedBy;
                return true;
            }
            query = default;
            applier = null;
            return false;
        }

        // === IUnitModule ===

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            stats = controller.GetModule<StatsModule>();
        }

        public void OnEnabled() { }
        public void OnDisabled() { }

        public void Cleanup()
        {
            UnsubscribeExpiryTriggers();
            ClearAll();
            controller = null;
            stats = null;
        }

        // === API ===

        /// <summary>
        /// Ponto ÚNICO de aplicação de status: resolve o tipo <paramref name="id"/> na tabela
        /// (<see cref="StatusCatalog"/> via <see cref="DataManager"/>), rola a chance e escala a duração de
        /// controle quando a aplicação é OFENSIVA (cross-team), constrói o efeito e empilha conforme o
        /// <see cref="StackMode"/> do tipo. Aplicação em si/aliado (mesmo time) nunca é resistida.
        /// </summary>
        /// <param name="id">Id do tipo na tabela de status.</param>
        /// <param name="duration">Duração em segundos (&lt;= 0 = permanente até o fim do combate).</param>
        /// <param name="magnitude">Modificadores desta aplicação (buff/debuff). Null = sem modificadores.</param>
        /// <param name="dps">Dano verdadeiro por segundo (DoT). &lt;= 0 = sem DoT.</param>
        /// <param name="chance">Chance base (0..1) da aplicação ofensiva. Ignorada em aplicação amiga.</param>
        /// <param name="appliedBy">Quem aplica (define ofensiva vs amiga, e resolve TargetSide.Applier).</param>
        public void ApplyStatus(string id, float duration, StatModification[] magnitude = null,
                                float dps = -1f, float chance = 1f, UnitController appliedBy = null)
        {
            if (stats == null || string.IsNullOrEmpty(id)) return;

            StatusTypeDef type = DataManager.GetStatus(id);
            if (type == null)
            {
                DebugManager.LogWarning($"Status '{id}' não encontrado no StatusCatalog.", DebugCategory.Combat);
                return;
            }

            // Ofensiva = cross-team: passa por chance (Efetividade × Resistência) e escala de controle.
            bool offensive = appliedBy != null && controller != null && appliedBy.GetTeam() != controller.GetTeam();

            if (offensive)
            {
                float effectiveness = appliedBy.Stats != null ? appliedBy.Stats.Effectiveness : 0f;
                if (!EffectChanceCalculator.Roll(chance, effectiveness, stats.EffectivenessResistance)) return;
            }

            float effectiveDuration = duration;
            if (type.behavior == StatusBehavior.Control && offensive)
            {
                float control = appliedBy.Stats != null ? appliedBy.Stats.Control : 0f;
                // Control/Tenacity em base 100 (100 = 100%): normaliza para fração antes de escalar a duração.
                effectiveDuration = duration * Mathf.Max(0f, 1f + (control - stats.Tenacity) / 100f);
                if (effectiveDuration <= 0f) return; // alvo imune
            }

            ApplyResolved(StatusEffect.Build(type, effectiveDuration, magnitude, dps), appliedBy);
        }

        // Empilha um efeito já construído conforme o StackMode do seu tipo:
        // Overwrite substitui o do mesmo tipo; Instances adiciona uma instância independente
        // (respeitando MaxStacks); Accumulate acumula stacks num único efeito (magnitude/DoT ×stacks).
        private void ApplyResolved(StatusEffect effect, UnitController appliedBy)
        {
            if (effect == null || stats == null) return;
            if (appliedBy != null) effect.AppliedBy = appliedBy;

            StatusEffect existing = Find(effect.Id);

            switch (effect.StackMode)
            {
                case StackMode.Overwrite:
                    if (existing != null) RemoveAt(active.IndexOf(existing)); // novo sobrescreve o antigo do mesmo tipo
                    AddNewInstance(effect);
                    return;

                case StackMode.Accumulate:
                    if (existing != null) { AddStackTo(existing, effect); return; }
                    AddNewInstance(effect);
                    return;

                case StackMode.Instances:
                    if (effect.MaxStacks > 0 && CountOfType(effect.Id) >= effect.MaxStacks)
                        RemoveOldestOfType(effect.Id); // abre espaço para a nova instância
                    AddNewInstance(effect);
                    return;
            }
        }

        public void RemoveEffect(string id)
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].Id == id) { RemoveAt(i); return; }
        }

        public bool HasEffect(string id) => Find(id) != null;

        /// <summary>Nº de instâncias/stacks de um tipo (Instances conta instâncias; Accumulate soma stacks).</summary>
        public int StacksOf(string id)
        {
            int total = 0;
            for (int i = 0; i < active.Count; i++)
                if (active[i].Id == id) total += active[i].Stacks;
            return total;
        }

        /// <summary>
        /// Purifica: remove até 'count' debuffs NÃO-condição (mais antigos primeiro). Condições são imunes.
        /// Retorna quantos removeu.
        /// </summary>
        public int RemoveDebuffs(int count)
        {
            int removed = 0;
            for (int i = 0; i < active.Count && removed < count; )
            {
                if (active[i].IsDebuff && !active[i].IsCondition) { RemoveAt(i); removed++; }
                else i++;
            }
            return removed;
        }

        /// <summary>Dispel: remove até 'count' buffs NÃO-condição (mais antigos primeiro). Condições são imunes.</summary>
        public int RemoveBuffs(int count)
        {
            int removed = 0;
            for (int i = 0; i < active.Count && removed < count; )
            {
                if (!active[i].IsDebuff && !active[i].IsCondition) { RemoveAt(i); removed++; }
                else i++;
            }
            return removed;
        }

        /// <summary>Avança a duração (só quem tem expiração por tempo) e processa DoT. A cada frame em combate.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = active[i];

                // DoT: dano verdadeiro por tick enquanto ativo.
                if (effect.DamagePerTick > 0f && effect.TickInterval > 0f)
                {
                    effect.TickTimer -= deltaTime;
                    while (effect.TickTimer <= 0f && !stats.IsDead)
                    {
                        effect.TickTimer += effect.TickInterval;
                        ApplyDamageTick(effect);
                    }
                }

                // Expiração por TEMPO — só quem tem a regra Duration.
                if (!effect.HasDurationExpiry) continue;
                effect.Remaining -= deltaTime;
                if (effect.Remaining > 0f) continue;

                StatusExpiryAction action = DurationAction(effect);
                if (action == StatusExpiryAction.RemoveStack)
                {
                    RemoveStacks(i, DurationStackAmount(effect));
                    if (i < active.Count && active[i] == effect) effect.Remaining = effect.Duration; // recarrega o próximo tick de perda
                }
                else RemoveAt(i);
            }
        }

        private void ApplyDamageTick(StatusEffect effect)
        {
            stats.ApplyDamage(new DamageResult
            {
                rawDamage = effect.DamagePerTick,
                finalDamage = effect.DamagePerTick,
                isCritical = false,
                ignoreDamageShare = true
            });
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                RemoveAt(i);
            DebuffCount = 0;
            untargetableCount = 0;
            controlCount = 0;
            forcedFocusEffect = null;
        }

        // === Expiração por gatilho (eventos do portador) ===

        /// <summary>Assina os gatilhos do portador (via PassiveHooks) para expiração por evento.
        /// Chamado no início do combate, depois de InitializeSkills. Idempotente.</summary>
        public void SubscribeExpiryTriggers()
        {
            PassiveHooks hooks = controller?.GetModule<SkillsModule>()?.Hooks;
            if (hooks == null) return;
            UnsubscribeExpiryTriggers();
            subscribedHooks = hooks;
            hooks.onSupremeUsed += OnSupremeUsed;
            hooks.onAfterAttack += OnAfterAttack;
            hooks.onDamageDealt += OnDamageDealt;
            hooks.onDamageTaken += OnDamageTaken;
            hooks.onAttackReceived += OnAttacked;
            hooks.onHealReceived += OnHealReceived;
        }

        private void UnsubscribeExpiryTriggers()
        {
            if (subscribedHooks == null) return;
            subscribedHooks.onSupremeUsed -= OnSupremeUsed;
            subscribedHooks.onAfterAttack -= OnAfterAttack;
            subscribedHooks.onDamageDealt -= OnDamageDealt;
            subscribedHooks.onDamageTaken -= OnDamageTaken;
            subscribedHooks.onAttackReceived -= OnAttacked;
            subscribedHooks.onHealReceived -= OnHealReceived;
            subscribedHooks = null;
        }

        private void OnSupremeUsed() => ProcessTrigger(StatusExpiryTrigger.OwnerSupremeCast);
        private void OnAfterAttack() => ProcessTrigger(StatusExpiryTrigger.OwnerAttack);
        private void OnDamageDealt(float _) => ProcessTrigger(StatusExpiryTrigger.OwnerDamageDealt);
        private void OnDamageTaken(float _) => ProcessTrigger(StatusExpiryTrigger.OwnerDamageTaken);
        private void OnAttacked(UnitController _) => ProcessTrigger(StatusExpiryTrigger.OwnerAttacked);
        private void OnHealReceived(float _) => ProcessTrigger(StatusExpiryTrigger.OwnerHealReceived);

        private void ProcessTrigger(StatusExpiryTrigger trigger)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = active[i];
                StatusExpiry[] rules = effect.Expiries;
                if (rules == null) continue;
                for (int r = 0; r < rules.Length; r++)
                {
                    if (rules[r].trigger != trigger) continue;
                    if (rules[r].action == StatusExpiryAction.RemoveEffect) { RemoveAt(i); break; }
                    else { RemoveStacks(i, rules[r].StackAmountOrOne); break; }
                }
            }
        }

        // === Privado: stacking ===

        private void AddNewInstance(StatusEffect effect)
        {
            active.Add(effect);
            effect.PerStackModifications = effect.Modifications;
            effect.PerStackDamagePerTick = effect.DamagePerTick;

            // Dispatch por família (comportamentos exclusivos).
            if (effect.IsStatModifier) ApplyModifications(effect);
            if (effect.IsDamageOverTime && effect.DamagePerTick > 0f) effect.TickTimer = effect.TickInterval;
            if (effect.IsStealth) untargetableCount++;
            if (effect.IsControl && ++controlCount == 1)
                controller.GetModule<CombatModule>()?.EnterControl();
            if (effect.IsTaunt)
            {
                if (!effect.ForcedFocus.HasValue) effect.ForcedFocus = new TargetQuery { side = TargetSide.Applier };
                forcedFocusEffect = effect;
            }

            if (effect.IsDebuff) DebuffCount++; // classificação Negative, ortogonal à família
            OnEffectsChanged?.Invoke();
        }

        // Accumulate: incrementa stacks (até MaxStacks) e reescala modificadores/DoT linearmente; renova duração.
        private void AddStackTo(StatusEffect existing, StatusEffect incoming)
        {
            existing.Duration = incoming.Duration;
            existing.Remaining = incoming.Duration;

            bool canGrow = existing.MaxStacks <= 0 || existing.Stacks < existing.MaxStacks;
            if (canGrow)
            {
                existing.Stacks++;
                RescaleModifiers(existing);
                existing.DamagePerTick = existing.PerStackDamagePerTick * existing.Stacks;
            }
            OnEffectsChanged?.Invoke();
        }

        // Remove 'n' stacks a partir do efeito no índice. Accumulate decrementa+reescala; Instances/Overwrite removem instâncias.
        private void RemoveStacks(int index, int n)
        {
            if (index < 0 || index >= active.Count) return;
            StatusEffect effect = active[index];

            if (effect.StackMode == StackMode.Accumulate && effect.Stacks > n)
            {
                effect.Stacks -= n;
                RescaleModifiers(effect);
                effect.DamagePerTick = effect.PerStackDamagePerTick * effect.Stacks;
                OnEffectsChanged?.Invoke();
                return;
            }

            if (effect.StackMode == StackMode.Instances)
            {
                // Remove até n instâncias do mesmo tipo (a atual + as mais antigas).
                RemoveAt(index);
                for (int k = 1; k < n; k++) if (!RemoveOldestOfType(effect.Id)) break;
                return;
            }

            RemoveAt(index); // Overwrite ou Accumulate chegando a 0
        }

        private void RescaleModifiers(StatusEffect effect)
        {
            RemoveModifications(effect); // remove pela InstanceKey
            if (effect.PerStackModifications == null) { effect.Modifications = null; return; }
            var scaled = new StatModification[effect.PerStackModifications.Length];
            for (int i = 0; i < scaled.Length; i++)
            {
                StatModification b = effect.PerStackModifications[i];
                scaled[i] = new StatModification(b.stat, b.flat * effect.Stacks, b.percent * effect.Stacks);
            }
            effect.Modifications = scaled;
            ApplyModifications(effect);
        }

        // === Privado: consultas ===

        private StatusEffect Find(string id)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].Id == id) return active[i];
            return null;
        }

        private int CountOfType(string id)
        {
            int c = 0;
            for (int i = 0; i < active.Count; i++) if (active[i].Id == id) c++;
            return c;
        }

        private bool RemoveOldestOfType(string id)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].Id == id) { RemoveAt(i); return true; }
            return false;
        }

        private static StatusExpiryAction DurationAction(StatusEffect effect)
        {
            for (int i = 0; i < effect.Expiries.Length; i++)
                if (effect.Expiries[i].trigger == StatusExpiryTrigger.Duration) return effect.Expiries[i].action;
            return StatusExpiryAction.RemoveEffect;
        }

        private static int DurationStackAmount(StatusEffect effect)
        {
            for (int i = 0; i < effect.Expiries.Length; i++)
                if (effect.Expiries[i].trigger == StatusExpiryTrigger.Duration) return effect.Expiries[i].StackAmountOrOne;
            return 1;
        }

        // === Privado: modificadores/remoção ===

        private void ApplyModifications(StatusEffect effect)
        {
            if (effect.Modifications == null) return;
            for (int i = 0; i < effect.Modifications.Length; i++)
            {
                StatModification m = effect.Modifications[i];
                stats.GetStatObject(m.stat)?.AddModifier(effect.InstanceKey, m.flat, m.percent);
            }
        }

        private void RemoveModifications(StatusEffect effect)
        {
            if (effect.Modifications == null) return;
            for (int i = 0; i < effect.Modifications.Length; i++)
            {
                StatModification m = effect.Modifications[i];
                stats.GetStatObject(m.stat)?.RemoveModifier(effect.InstanceKey);
            }
        }

        private void RemoveAt(int index)
        {
            StatusEffect effect = active[index];
            if (effect.IsStatModifier) RemoveModifications(effect);
            if (effect.IsDebuff && DebuffCount > 0) DebuffCount--;
            if (effect.IsStealth && untargetableCount > 0) untargetableCount--;
            if (effect.IsControl && controlCount > 0 && --controlCount == 0)
                controller.GetModule<CombatModule>()?.ExitControl();
            active.RemoveAt(index);
            if (effect == forcedFocusEffect) RecomputeForcedFocus();
            OnEffectsChanged?.Invoke();
        }

        private void RecomputeForcedFocus()
        {
            forcedFocusEffect = null;
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].ForcedFocus.HasValue) { forcedFocusEffect = active[i]; break; }
        }
    }
}
