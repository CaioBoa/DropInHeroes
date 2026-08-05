using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class SkillsModule : IUnitModule
    {
        private UnitController controller;
        private StatsModule stats;
        private VisualModule visual;
        private CombatModule combat;

        // Runtime clones (estado independente por unidade)
        private ActiveSkill baseSkill;
        private ActiveSkill supremeSkill;
        private PassiveSkill passiveSkill;

        // Hook system
        private PassiveHooks hooks = new PassiveHooks();

        // Skill execution state
        private ActiveSkill executingSkill;
        private bool supremeReady;

        // Forma (stance) ativa — trocada por SetForm; null = conjunto base do CharacterData.
        private string activeFormKey;
        public string ActiveFormKey => activeFormKey;

        private bool CanUseSupreme => controller != null && controller.HasCapability(UnitCapability.UseSupreme);

        public PassiveHooks Hooks => hooks;
        public bool IsSupremeReady => supremeReady && CanUseSupreme;
        public bool HasPendingPriority => IsSupremeReady && executingSkill != supremeSkill;

        // Registro on-hit da unidade (os efeitos da passiva são registrados/removidos por aqui).
        private OnHitModule onHitRegistry;
        private string passiveOnHitId;

        // === IUnitModule ===

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            stats = controller.GetModule<StatsModule>();
            visual = controller.GetModule<VisualModule>();
        }

        public void OnEnabled() { }
        public void OnDisabled() { }

        public void Cleanup()
        {
            ClearSkills();
            controller = null;
            stats = null;
            visual = null;
            combat = null;
        }

        // === COMBAT LIFECYCLE ===

        public void InitializeSkills()
        {
            combat = controller.GetModule<CombatModule>();

            CharacterData data = controller.GetCharacterData();
            if (data == null) return;

            baseSkill = data.baseSkill != null ? Object.Instantiate(data.baseSkill) : null;
            supremeSkill = data.supremeSkill != null ? Object.Instantiate(data.supremeSkill) : null;
            passiveSkill = data.passiveSkill != null ? Object.Instantiate(data.passiveSkill) : null;

            supremeReady = false;
            executingSkill = null;

            SkillContext ctx = BuildContext(null);

            passiveSkill?.Initialize(ctx);
            baseSkill?.Initialize(ctx);   // o ataque registra seus on-hit no OnHitModule aqui
            supremeSkill?.Initialize(ctx);
            RegisterPassiveOnHit();       // on-hit da passiva depois dos do ataque (ordem de aplicação)

            var energy = stats?.GetResourceObject(ResourceType.Energy);
            if (energy != null)
                energy.OnValueChanged += OnEnergyChanged;

            if (visual != null)
                visual.OnAttackHit += HandleSkillHit;

            hooks.InvokeBattleStart();
        }

        public void ClearSkills()
        {
            hooks.InvokeBattleEnd();
            hooks.ClearAll();

            UnregisterPassiveOnHit();
            baseSkill?.Clear();
            supremeSkill?.Clear();
            passiveSkill?.Clear();

            if (baseSkill != null) Object.Destroy(baseSkill);
            if (supremeSkill != null) Object.Destroy(supremeSkill);
            if (passiveSkill != null) Object.Destroy(passiveSkill);

            baseSkill = null;
            supremeSkill = null;
            passiveSkill = null;
            executingSkill = null;
            supremeReady = false;

            var energy = stats?.GetResourceObject(ResourceType.Energy);
            if (energy != null)
                energy.OnValueChanged -= OnEnergyChanged;

            if (visual != null)
                visual.OnAttackHit -= HandleSkillHit;
        }

        // === SKILL EXECUTION ===

        public ActiveSkill GetCurrentSkill()
        {
            if (IsSupremeReady && supremeSkill != null)
                return supremeSkill;
            return baseSkill;
        }

        /// <summary>
        /// Executa a skill de maior prioridade. Retorna duração da animação.
        /// </summary>
        public float ExecuteSkill(UnitController target)
        {
            ActiveSkill skill = GetCurrentSkill();
            if (skill == null) return ActiveSkill.DefaultSkillDuration;

            executingSkill = skill;

            hooks.InvokeBeforeAttack();

            SkillContext ctx = BuildContext(target);
            float duration = skill.Execute(ctx);

            // Identifica o supremo por referência (slot supremeSkill), não pelo enum serializado:
            // a energia zera mesmo que skillType não tenha sido configurado no asset.
            if (supremeSkill != null && skill == supremeSkill)
            {
                stats?.GetResourceObject(ResourceType.Energy)?.SetToMin();
                supremeReady = false;
                hooks.InvokeSupremeUsed();
            }

            return duration;
        }

        private void HandleSkillHit()
        {
            if (executingSkill == null) return;

            UnitController target = combat?.CurrentTarget;
            SkillContext ctx = BuildContext(target);
            executingSkill.OnHit(ctx);

            hooks.InvokeAfterAttack();
        }

        public void OnSkillFinished()
        {
            executingSkill = null;
        }

        public void OnSkillInterrupted()
        {
            executingSkill = null;
        }

        // === CROSS-REFERENCING ===

        public ActiveSkill GetActive(ActiveSkillType type)
        {
            switch (type)
            {
                case ActiveSkillType.Base: return baseSkill;
                case ActiveSkillType.Supreme: return supremeSkill;
                default: return null;
            }
        }

        public PassiveSkill GetPassive() => passiveSkill;

        public T GetPassive<T>() where T : PassiveSkill
        {
            return passiveSkill as T;
        }

        // === PASSIVE LIFECYCLE ===

        public void DeactivatePassive()
        {
            passiveSkill?.Deactivate(BuildContext(null));
        }

        /// <summary>Reaplica a passiva (ex.: após revive / remoção de selo). Entry-point para um futuro módulo de ressurreição.</summary>
        public void ReactivatePassive()
        {
            passiveSkill?.Reactivate(BuildContext(null));
        }

        // === FORMAS (STANCES) ===

        /// <summary>
        /// Ativa uma FORMA: troca o ataque e o supremo ATIVOS pelos overrides da forma (skill nula = volta
        /// à base do <see cref="CharacterData"/>) e o perfil de animação. NÃO decide QUANDO trocar nem aplica
        /// bônus de forma — isso é responsabilidade da passiva. Reinstancia os clones de skill, então chame
        /// FORA da pilha de dano (ex.: em onAfterAttack), nunca de dentro de OnHit.
        /// </summary>
        public void SetForm(string key)
        {
            CharacterData data = controller?.GetCharacterData();
            CharacterForm form = data?.GetForm(key);
            if (form == null)
            {
                DebugManager.LogWarning($"Forma '{key}' não existe em {data?.displayName}.", DebugCategory.Character);
                return;
            }

            SkillContext ctx = BuildContext(null);
            SwapSkill(ref baseSkill, form.baseSkill != null ? form.baseSkill : data.baseSkill, ctx);
            SwapSkill(ref supremeSkill, form.supremeSkill != null ? form.supremeSkill : data.supremeSkill, ctx);

            if (!string.IsNullOrEmpty(form.animationProfileKey)) visual?.SetAnimationProfile(form.animationProfileKey);
            else visual?.ResetAnimationProfile();

            activeFormKey = key;
        }

        // Substitui um clone de skill: limpa/destrói o atual e instancia+inicializa o novo a partir do asset.
        private void SwapSkill(ref ActiveSkill current, ActiveSkill source, SkillContext ctx)
        {
            if (current != null) { current.Clear(); Object.Destroy(current); }
            current = source != null ? Object.Instantiate(source) : null;
            current?.Initialize(ctx);
        }

        // === HOOK NOTIFICATIONS ===

        public void NotifyDamageTaken(float amount) => hooks.InvokeDamageTaken(amount);
        public void NotifyDamageDealt(float amount) => hooks.InvokeDamageDealt(amount);
        public void NotifyHealReceived(float amount) => hooks.InvokeHealReceived(amount);
        public void NotifyAttackReceived(UnitController attacker) => hooks.InvokeAttackReceived(attacker);
        public void NotifyCriticalHit(UnitController target) => hooks.InvokeCriticalHit(target);

        public void NotifyUnitDeath()
        {
            hooks.InvokeUnitDeath();
            DeactivatePassive();
        }

        // === PRIVATE ===

        // A passiva também é uma FONTE de on-hit: seus efeitos entram no registro da unidade no init
        // (fonte Passive) e saem no fim do combate — mesma consulta-no-init que o ataque faz.
        private void RegisterPassiveOnHit()
        {
            var fx = passiveSkill != null ? passiveSkill.OnHitEffects : null;
            if (fx == null || fx.Count == 0) return;

            onHitRegistry = controller.GetModule<OnHitModule>();
            if (onHitRegistry == null) return;

            if (string.IsNullOrEmpty(passiveOnHitId))
                passiveOnHitId = "onhit_passive_" + System.Guid.NewGuid().ToString("N");
            for (int i = 0; i < fx.Count; i++)
                if (fx[i] != null) onHitRegistry.Register(passiveOnHitId + "_" + i, OnHitSource.Passive, fx[i]);
        }

        private void UnregisterPassiveOnHit()
        {
            var fx = passiveSkill != null ? passiveSkill.OnHitEffects : null;
            if (onHitRegistry == null || fx == null) return;
            for (int i = 0; i < fx.Count; i++)
                onHitRegistry.Unregister(passiveOnHitId + "_" + i);
            onHitRegistry = null;
        }

        private void OnEnergyChanged(float currentEnergy)
        {
            if (supremeReady || supremeSkill == null) return;

            var energy = stats?.GetResourceObject(ResourceType.Energy);
            if (energy == null || !energy.IsFull) return;

            supremeReady = true;
            hooks.InvokeEnergyFull();

            // Interromper qualquer skill que não seja o supremo (menor prioridade)
            if (executingSkill != null && executingSkill != supremeSkill)
                combat?.InterruptCurrentSkill();
        }

        private SkillContext BuildContext(UnitController target)
        {
            return new SkillContext
            {
                owner = controller,
                target = target,
                ownerStats = stats,
                ownerVisual = visual,
                skills = this,
                enemies = combat != null ? combat.Targets : null,
                allies = combat != null ? combat.Allies : null
            };
        }
    }
}
