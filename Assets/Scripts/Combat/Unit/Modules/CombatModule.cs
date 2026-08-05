using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Máquina de estados de combate: mira, timing de ataque, upkeep de energia/status e execução de
    /// skills. O movimento em si (NavMeshAgent) é delegado ao MovementModule.
    /// </summary>
    public class CombatModule : IUnitModule
    {
        private const float DefaultRange = 1.5f; // Alcance assumido quando os stats não existem.

        private UnitController controller;
        private VisualModule visualModule;
        private StatsModule stats;
        private SkillsModule skills;
        private StatusModule status;
        private FocusModule focus;
        private MovementModule movement;
        private Transform transform;
        private List<UnitController> allTargets;
        private List<UnitController> allies;

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
        public IReadOnlyList<UnitController> Targets => allTargets;
        public IReadOnlyList<UnitController> Allies => allies;

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            transform = controller.transform;
            stats = controller.GetModule<StatsModule>();
            visualModule = controller.GetModule<VisualModule>();
            skills = controller.GetModule<SkillsModule>();
            status = controller.GetModule<StatusModule>();
            focus = controller.GetModule<FocusModule>();
            movement = controller.GetModule<MovementModule>();
        }

        public void OnEnabled() { }

        public void OnDisabled()
        {
            // O agente é desligado pelo MovementModule (seu próprio OnDisabled, no ResetToPool);
            // aqui só resetamos o estado de combate e paramos o trajeto em curso.
            SetState(CombatState.Waiting);
            movement?.Stop();
            currentTarget = null;
            allTargets = null;
            allies = null;
            isAttacking = false;

            skills?.ClearSkills();
        }

        public void Cleanup()
        {
            controller = null;
            stats = null;
            skills = null;
            visualModule = null;
            status = null;
            focus = null;
            movement = null;
            transform = null;
            allTargets = null;
            allies = null;
            currentTarget = null;
        }

        /// <summary>
        /// Início de combate em DUAS FASES: <see cref="ResetForCombat"/> (reset puro — zera energia e
        /// limpa escudos) precisa rodar em TODAS as unidades ANTES de qualquer <see cref="BeginCombat"/>
        /// (grants de início + ativação), senão o Clear de uma unidade apagaria um escudo que outra
        /// acabou de conceder a ela (passiva/artefato que dá escudo a aliados). O CombatController faz os
        /// dois passes no batch inicial; invocações spawnadas no meio do combate chamam este wrapper
        /// (as duas fases juntas, unidade única — sem risco de wipe).
        /// </summary>
        public void StartCombat(List<UnitController> allies, List<UnitController> enemies)
        {
            ResetForCombat(allies, enemies);
            BeginCombat();
        }

        /// <summary>Fase 1: seta aliados/inimigos e zera o estado transiente (energia + escudos). NÃO
        /// concede nada — para que o Clear de escudos NUNCA rode depois de um grant de início de combate.</summary>
        public void ResetForCombat(List<UnitController> allies, List<UnitController> enemies)
        {
            this.allies = allies;
            allTargets = enemies;

            stats?.PrepareForCombat();
            // Escudos não persistem entre combates — cada luta começa sem absorção residual.
            controller?.GetModule<ShieldModule>()?.Clear();
        }

        /// <summary>Fase 2: aplica os efeitos de início de combate (build/artefato + passiva) e ativa a
        /// unidade. Só depois de TODAS terem passado pelo <see cref="ResetForCombat"/> — assim os escudos
        /// concedidos aqui a aliados não são mais limpos por ninguém.</summary>
        public void BeginCombat()
        {
            // Aplica a build (pontos da árvore + artefato) — com allies/enemies já setados
            // (efeitos de início de combate miram por eles).
            controller?.GetModule<LoadoutModule>()?.ApplyToCombat();

            // Sem NENHUMA capacidade de agir (mover/atacar/conjurar) = invocação passiva pura:
            // existe e pode ser atacada/morrer, mas não age. Conjurador sem ataque (ex.: totem com
            // supremo) AGE: regenera energia e autocasta o supremo (ver TickCombatUpkeep).
            bool canAct = controller != null &&
                (controller.HasCapability(UnitCapability.Move) || controller.HasCapability(UnitCapability.Attack)
                 || controller.HasCapability(UnitCapability.UseSupreme));
            if (!canAct)
            {
                SetState(CombatState.Waiting);
                return;
            }

            movement?.Activate();

            nextAttackTime = Time.time + Random.Range(0f, 0.5f);

            skills?.InitializeSkills();
            // Assina os gatilhos de expiração de status (supremo/ataque/dano) DEPOIS dos hooks existirem.
            status?.SubscribeExpiryTriggers();

            SetState(CombatState.LookingForTarget);
        }

        public void Tick()
        {
            switch (currentState)
            {
                case CombatState.LookingForTarget:
                    TickCombatUpkeep();
                    TickLookingForTarget();
                    break;

                case CombatState.Attacking:
                    TickCombatUpkeep();
                    TickAttacking();
                    break;

                case CombatState.Immobilized:
                    // Controle (stun/root): não age nem regenera energia, mas o status corre
                    // para o próprio efeito de controle expirar.
                    status?.Tick(Time.deltaTime);
                    break;

                case CombatState.Waiting:
                case CombatState.Dead:
                case CombatState.Victory:
                    // Combate suspenso (Waiting) ou encerrado (Dead/Victory): nenhuma mecânica de
                    // combate roda — sem regen de energia, sem ataques, sem supremo.
                    break;
            }
        }

        /// <summary>Mecânicas contínuas que só valem enquanto a unidade está em combate ativo.</summary>
        private void TickCombatUpkeep()
        {
            if (controller != null && controller.HasCapability(UnitCapability.UseSupreme))
                stats?.RegenerateEnergy(Time.deltaTime);
            status?.Tick(Time.deltaTime);

            // Refletir mudanças de velocidade (ex.: debuff de lentidão) no agente.
            movement?.SyncSpeed();

            // Conjurador SEM capacidade de ataque (ex.: totem): o fluxo normal de ataque nunca roda,
            // então o ciclo do supremo é gerido aqui — dispara assim que a energia enche (sem depender
            // de alvo em alcance) e encerra a conjuração quando a animação termina.
            if (controller != null && !controller.HasCapability(UnitCapability.Attack))
            {
                if (isAttacking)
                {
                    if (Time.time >= attackEndTime) FinishAttack();
                }
                else if (skills != null && skills.IsSupremeReady)
                {
                    StartAttack();
                }
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
            switch (state)
            {
                case CombatState.LookingForTarget:
                    visualModule?.SetMovingAnimation(false);
                    movement?.Stop();
                    break;
                case CombatState.Immobilized:
                    visualModule?.SetStunnedAnimation(false);
                    break;
            }
        }

        private void OnEnterState(CombatState state)
        {
            switch (state)
            {
                case CombatState.LookingForTarget:
                    // Só entra em animação de movimento quem PODE se mover — um conjurador estacionário
                    // (totem, sem capacidade Move) fica no Idle em vez de "correr parado" (que, sem clipe de
                    // Run, prende o sprite no defaultSprite estático).
                    visualModule?.SetMovingAnimation(controller != null && controller.HasCapability(UnitCapability.Move));
                    TryFindAndMoveToTarget();
                    break;
                case CombatState.Attacking:
                    break;
                case CombatState.Immobilized:
                    visualModule?.SetMovingAnimation(false);
                    visualModule?.SetStunnedAnimation(true);
                    break;
                case CombatState.Dead:
                    StopCombatActivity();
                    visualModule?.PlayDeathAnimation();
                    controller.GetModule<HealthBarModule>()?.OnDisabled();
                    controller.GetModule<EnergyBarModule>()?.OnDisabled();
                    break;
                case CombatState.Victory:
                    StopCombatActivity();
                    visualModule?.PlayVictoryAnimation();
                    break;
            }
        }

        // === STATE TICK METHODS ===

        private void TickLookingForTarget()
        {
            if (currentTarget == null || !focus.IsValidFocus(currentTarget, allTargets, allies))
            {
                TryFindAndMoveToTarget();
                if (currentTarget == null)
                {
                    movement?.Stop();
                    return;
                }
            }

            // Flip baseado na velocidade do movimento
            Vector3 velocity = movement?.Velocity ?? Vector3.zero;
            if (velocity.sqrMagnitude > 0.01f)
                visualModule?.SetFacingDirection(velocity.x);

            float range = stats?.Range ?? DefaultRange;
            float sqrToTarget = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (sqrToTarget <= range * range)
            {
                SetState(CombatState.Attacking);
                return;
            }

            // Movimento via NavMesh (apenas se a unidade pode se mover)
            if (controller.HasCapability(UnitCapability.Move))
                movement?.MoveTo(currentTarget.transform.position);
        }

        private void TickAttacking()
        {
            float range = stats?.Range ?? DefaultRange;

            if (currentTarget != null)
                visualModule?.FaceTowards(currentTarget.transform.position);

            // Aguardar animação em progresso (interrupção por prioridade é feita via InterruptCurrentSkill)
            if (isAttacking)
            {
                if (Time.time >= attackEndTime)
                    FinishAttack();
                return;
            }

            // Revalidar o alvo atual (morte/taunt/furtividade) via FocusModule
            if (currentTarget == null || !focus.IsValidFocus(currentTarget, allTargets, allies))
            {
                if (!ReacquireTargetOrLook(range)) return;
            }

            // Verificar se o alvo ainda está em range (distância ao quadrado — evita sqrt por frame).
            // Se o bloco acima re-adquiriu um alvo em range, esta checagem passa sem re-resolver.
            float sqrToTarget = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrToTarget > range * range)
            {
                if (!ReacquireTargetOrLook(range)) return;
            }

            // Unidade sem capacidade de ataque (ex.: invocação só-de-suporte): rastreia o alvo mas
            // não executa skill.
            if (controller == null || !controller.HasCapability(UnitCapability.Attack))
                return;

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
            currentTarget = focus.ResolveTarget(allTargets, allies, inRangeOnly: false, range: 0f);
            if (currentTarget != null && controller.HasCapability(UnitCapability.Move))
                movement?.MoveTo(currentTarget.transform.position);
        }

        // Re-adquire um alvo em range; se não houver, foca o mais próximo fora de range e volta a
        // LookingForTarget. Retorna false quando não há alvo em range (o caller deve sair do tick).
        private bool ReacquireTargetOrLook(float range)
        {
            currentTarget = focus.ResolveTarget(allTargets, allies, inRangeOnly: true, range);
            if (currentTarget != null) return true;

            currentTarget = focus.ResolveTarget(allTargets, allies, inRangeOnly: false, range);
            SetState(CombatState.LookingForTarget);
            return false;
        }

        // === ATTACK METHODS ===

        private void StartAttack()
        {
            float duration = skills?.ExecuteSkill(currentTarget) ?? ActiveSkill.DefaultSkillDuration;

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
        /// Entra em CONTROLE (stun/root): interrompe ataque/movimento e imobiliza. Chamado pelo
        /// StatusModule quando o primeiro efeito de controle fica ativo. Não age em morte/vitória.
        /// </summary>
        public void EnterControl()
        {
            if (currentState == CombatState.Dead || currentState == CombatState.Victory) return;
            if (currentState == CombatState.Immobilized) return;

            isAttacking = false;
            visualModule?.ResetAnimatorSpeed();
            skills?.OnSkillInterrupted();
            movement?.Stop();
            SetState(CombatState.Immobilized);
        }

        /// <summary>Sai do controle quando o último efeito de controle expira: volta a procurar alvo.</summary>
        public void ExitControl()
        {
            if (currentState != CombatState.Immobilized) return;
            SetState(CombatState.LookingForTarget);
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

        /// <summary>
        /// Encerra toda a atividade de combate da unidade ao entrar em estado terminal
        /// (morte/vitória): interrompe ataque e desliga o movimento.
        /// </summary>
        private void StopCombatActivity()
        {
            isAttacking = false;
            currentTarget = null;
            visualModule?.ResetAnimatorSpeed();
            movement?.Deactivate();
        }

        /// <summary>
        /// Move a unidade até 'destination' em 'duration' segundos (ex.: salto de skill). Desabilita
        /// o NavMeshAgent durante o lerp e o re-sincroniza no fim, se ainda em combate ativo.
        /// </summary>
        public void LeapTo(Vector3 destination, float duration)
        {
            if (controller == null) return;
            controller.StartCoroutine(LeapRoutine(destination, duration));
        }

        private IEnumerator LeapRoutine(Vector3 destination, float duration)
        {
            bool reEnable = movement != null && movement.BeginManualMove(); // libera o transform para o lerp

            Vector3 start = transform.position;
            destination.z = start.z; // mantém o plano 2D

            float t = 0f;
            while (duration > 0f && t < duration)
            {
                if (currentState == CombatState.Dead || currentState == CombatState.Victory)
                    yield break;

                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(start, destination, k);
                visualModule?.SetFacingDirection(destination.x - start.x);
                yield return null;
            }

            transform.position = destination;

            // Só reabilita o agente se a unidade ainda estiver lutando (evita reativar em morte/vitória).
            if (reEnable && (currentState == CombatState.LookingForTarget || currentState == CombatState.Attacking))
                movement?.EndManualMove(destination);
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
}
