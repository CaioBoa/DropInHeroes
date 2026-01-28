using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Módulo de combate com state machine e NavMeshAgent para pathfinding
/// </summary>
public class CombatModule : IUnitModule
{
    private UnitController controller;
    private VisualModule visualModule;
    private StatsModule stats;
    private SkillsModule skills;
    private NavMeshAgent agent;
    private Transform transform;
    private List<UnitController> allTargets;

    // State Machine
    private CombatState currentState = CombatState.Waiting;
    private UnitController currentTarget;

    // Attack timing
    private float nextAttackTime;

    // Attack animation control
    private bool isAttacking;
    private float attackEndTime;

    public CombatState State => currentState;
    public UnitController CurrentTarget => currentTarget;

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        transform = controller.transform;
        stats = controller.GetModule<StatsModule>();
        visualModule = controller.GetModule<VisualModule>();
        skills = controller.GetModule<SkillsModule>();
    }

    public void OnEnabled() { }

    public void OnDisabled()
    {
        SetState(CombatState.Waiting);
        StopMovement();
        currentTarget = null;
        allTargets = null;
        isAttacking = false;

        skills?.ClearSkills();

        if (agent != null)
        {
            Object.Destroy(agent);
            agent = null;
        }
    }

    public void Cleanup()
    {
        controller = null;
        stats = null;
        skills = null;
        allTargets = null;
        currentTarget = null;
    }

    public void StartCombat(List<UnitController> enemies)
    {
        allTargets = enemies;

        stats?.PrepareForCombat();

        agent = controller.gameObject.AddComponent<NavMeshAgent>();
        ConfigureNavMeshAgent();

        nextAttackTime = Time.time + Random.Range(0f, 0.5f);

        skills?.InitializeSkills();

        SetState(CombatState.LookingForTarget);
    }

    private void ConfigureNavMeshAgent()
    {
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = stats?.Speed ?? 3f;
        agent.acceleration = 8f;
        agent.angularSpeed = 0f;
        agent.stoppingDistance = 0.1f;
        agent.radius = 0.25f;
    }

    public void Tick()
    {
        // Regenerar energia
        stats?.RegenerateEnergy(Time.deltaTime);

        switch (currentState)
        {
            case CombatState.Waiting:
                break;
            case CombatState.LookingForTarget:
                TickLookingForTarget();
                break;
            case CombatState.Attacking:
                TickAttacking();
                break;
            case CombatState.Immobilized:
                break;
            case CombatState.Dead:
                break;
        }
    }

    // === STATE TRANSITIONS ===

    public void SetState(CombatState newState)
    {
        if (currentState == newState) return;

        OnExitState(currentState);
        currentState = newState;
        OnEnterState(newState);
    }

    private void OnExitState(CombatState state)
    {
        if (state == CombatState.LookingForTarget)
            if (visualModule != null)
            {
                visualModule.SetMovingAnimation(false);
            }
            StopMovement();
    }

    private void OnEnterState(CombatState state)
    {
        switch (state)
        {
            case CombatState.LookingForTarget:
                if (visualModule != null)
                {
                    visualModule.SetMovingAnimation(true);
                }
                TryFindAndMoveToTarget();
                break;
            case CombatState.Attacking:
                break;
            case CombatState.Dead:
                visualModule?.PlayDeathAnimation();
                controller.GetModule<HealthBarModule>()?.OnDisabled();
                if (agent != null) agent.enabled = false;
                break;
            case CombatState.Victory:
                visualModule?.PlayVictoryAnimation();
                if (agent != null) agent.enabled = false;
                break;
        }
    }

    // === STATE TICK METHODS ===

    private void TickLookingForTarget()
    {
        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            TryFindAndMoveToTarget();
            if (currentTarget == null)
            {
                StopMovement();
                return;
            }
        }

        // Flip baseado na velocidade do agent
        if (agent != null && agent.velocity.sqrMagnitude > 0.01f)
        {
            visualModule?.SetFacingDirection(agent.velocity.x);
        }

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        float range = stats?.Range ?? 1.5f;

        if (distance <= range)
        {
            SetState(CombatState.Attacking);
            return;
        }

        // Movimento via NavMesh
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentTarget.transform.position);
        }
    }

    private void TickAttacking()
    {
        float range = stats?.Range ?? 1.5f;

        if (currentTarget != null)
            visualModule?.FaceTowards(currentTarget.transform.position);

        // Aguardar animação em progresso (interrupção por prioridade é feita via InterruptCurrentSkill)
        if (isAttacking)
        {
            if (Time.time >= attackEndTime)
                FinishAttack();
            return;
        }

        // Verificar target atual
        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            currentTarget = FindClosestInRange(range);
            if (currentTarget == null)
            {
                currentTarget = FindClosestAlive();
                SetState(CombatState.LookingForTarget);
                return;
            }
        }

        // Verificar se target ainda está em range
        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distance > range)
        {
            currentTarget = FindClosestInRange(range);
            if (currentTarget == null)
            {
                currentTarget = FindClosestAlive();
                SetState(CombatState.LookingForTarget);
                return;
            }
        }

        // Supreme pronto: ignorar nextAttackTime, executar imediatamente
        if (skills != null && skills.IsSupremeReady)
        {
            StartAttack();
            return;
        }

        if (Time.time >= nextAttackTime)
            StartAttack();
    }

    // === HELPER METHODS ===

    private void TryFindAndMoveToTarget()
    {
        currentTarget = FindClosestAlive();
        if (currentTarget != null && agent != null && agent.isOnNavMesh)
            agent.SetDestination(currentTarget.transform.position);
    }

    private UnitController FindClosestAlive()
    {
        if (allTargets == null || allTargets.Count == 0) return null;

        UnitController closest = null;
        float minDist = float.MaxValue;

        foreach (var target in allTargets)
        {
            if (!IsTargetValid(target)) continue;

            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = target;
            }
        }
        return closest;
    }

    private UnitController FindClosestInRange(float range)
    {
        if (allTargets == null || allTargets.Count == 0) return null;

        UnitController closest = null;
        float minDist = float.MaxValue;

        foreach (var target in allTargets)
        {
            if (!IsTargetValid(target)) continue;

            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                closest = target;
            }
        }
        return closest;
    }

    private bool IsTargetValid(UnitController target)
    {
        if (target == null) return false;
        var targetStats = target.GetModule<StatsModule>();
        return !targetStats.IsDead;
    }

    // === ATTACK METHODS ===

    private void StartAttack()
    {
        float duration = skills?.ExecuteSkill(currentTarget) ?? 0.5f;

        isAttacking = true;
        attackEndTime = Time.time + duration;
        nextAttackTime = Time.time + duration;
    }

    private void FinishAttack()
    {
        isAttacking = false;
        visualModule?.ResetAnimatorSpeed();
        skills?.OnSkillFinished();
    }

    /// <summary>
    /// Interrompe a skill atual para executar uma de maior prioridade.
    /// Chamado pelo SkillsModule quando supreme fica disponível.
    /// </summary>
    public void InterruptCurrentSkill()
    {
        if (!isAttacking)
        {
            StartAttack();
            return;
        }

        isAttacking = false;
        visualModule?.ResetAnimatorSpeed();
        skills?.OnSkillInterrupted();
        StartAttack();
    }

    private void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }
}

/// <summary>
/// Estados de combate da unidade
/// </summary>
public enum CombatState
{
    Waiting,
    LookingForTarget,
    Attacking,
    Immobilized,
    Dead,
    Victory
}
