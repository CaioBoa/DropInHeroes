using UnityEngine;

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

    public PassiveHooks Hooks => hooks;
    public bool IsSupremeReady => supremeReady;
    public bool HasPendingPriority => supremeReady && (executingSkill == null || executingSkill.skillType < ActiveSkillType.Supreme);

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
        baseSkill?.Initialize(ctx);
        supremeSkill?.Initialize(ctx);

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
        if (supremeReady && supremeSkill != null)
            return supremeSkill;
        return baseSkill;
    }

    /// <summary>
    /// Executa a skill de maior prioridade. Retorna duração da animação.
    /// </summary>
    public float ExecuteSkill(UnitController target)
    {
        ActiveSkill skill = GetCurrentSkill();
        if (skill == null) return 0.5f;

        executingSkill = skill;

        hooks.InvokeBeforeAttack();

        SkillContext ctx = BuildContext(target);
        float duration = skill.Execute(ctx);

        if (skill.skillType == ActiveSkillType.Supreme)
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

    public T GetActive<T>(ActiveSkillType type) where T : ActiveSkill
    {
        return GetActive(type) as T;
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

    public void ReactivatePassive()
    {
        passiveSkill?.Reactivate(BuildContext(null));
    }

    // === HOOK NOTIFICATIONS ===

    public void NotifyDamageTaken(float amount) => hooks.InvokeDamageTaken(amount);
    public void NotifyDamageDealt(float amount) => hooks.InvokeDamageDealt(amount);
    public void NotifyHealReceived(float amount) => hooks.InvokeHealReceived(amount);

    public void NotifyUnitDeath()
    {
        hooks.InvokeUnitDeath();
        DeactivatePassive();
    }

    // === PRIVATE ===

    private void OnEnergyChanged(float currentEnergy)
    {
        if (supremeReady || supremeSkill == null) return;

        var energy = stats?.GetResourceObject(ResourceType.Energy);
        if (energy == null || !energy.IsFull) return;

        supremeReady = true;
        hooks.InvokeEnergyFull();

        // Interromper skill de menor prioridade imediatamente
        if (executingSkill != null && executingSkill.skillType < ActiveSkillType.Supreme)
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
            skills = this
        };
    }
}
