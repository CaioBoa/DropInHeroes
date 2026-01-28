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
    private bool damageApplied;

    public CombatState State => currentState;

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        transform = controller.transform;
        stats = controller.GetModule<StatsModule>();
        visualModule = controller.GetModule<VisualModule>();

        if (visualModule != null)
            visualModule.OnAttackHit += ApplyDamage;
    }

    public void OnEnabled() { }

    public void OnDisabled()
    {
        SetState(CombatState.Waiting);
        StopMovement();
        currentTarget = null;
        allTargets = null;

        // Limpar estado de animação
        isAttacking = false;
        damageApplied = false;

        if (visualModule != null)
            visualModule.OnAttackHit -= ApplyDamage;

        // Destruir NavMeshAgent para reutilização no pool
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
        allTargets = null;
        currentTarget = null;
    }

    public void StartCombat(List<UnitController> enemies)
    {
        allTargets = enemies;

        // Energia começa em 0
        stats?.PrepareForCombat();

        agent = controller.gameObject.AddComponent<NavMeshAgent>();
        ConfigureNavMeshAgent();

        nextAttackTime = Time.time + Random.Range(0f, 0.5f);

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

        // Flip para olhar o alvo durante ataque
        if (currentTarget != null)
            visualModule?.FaceTowards(currentTarget.transform.position);

        // Aguardar animação em progresso
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

        // Iniciar ataque
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
        float speedMult = (stats?.Speed ?? 3f) / 12f;
        float duration = visualModule?.PlayAttackAnimation(speedMult) ?? 0.5f;

        isAttacking = true;
        damageApplied = false;
        attackEndTime = Time.time + duration;
        nextAttackTime = Time.time + duration;
    }

    private void ApplyDamage()
    {
        if (!isAttacking || damageApplied) return;
        if (currentTarget == null || !IsTargetValid(currentTarget)) return;

        damageApplied = true;

        float damage = stats?.Attack ?? 10f;
        currentTarget.GetModule<StatsModule>()?.TakeDamage(damage);

        DebugManager.Log($"{controller.GetCharacterData()?.displayName} atacou {currentTarget.GetCharacterData()?.displayName} por {damage}", DebugCategory.Combat);
    }

    private void FinishAttack()
    {
        isAttacking = false;
        visualModule?.ResetAnimatorSpeed();

        // Fallback: se Animation Event não disparou, aplicar dano agora
        if (!damageApplied)
            ApplyDamage();
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
