using UnityEngine;
using UnityEngine.AI;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Encapsula a navegação 2D da unidade via NavMeshAgent: adiciona o agente uma vez, liga/desliga
    /// entre combates, move até um destino, sincroniza a velocidade e expõe a velocidade atual.
    /// O CombatModule comanda o movimento; a máquina de estados e o timing de ataque ficam lá.
    /// </summary>
    public class MovementModule : IUnitModule
    {
        private const float DefaultSpeed = 3f;       // Velocidade assumida quando os stats não existem.
        private const float Acceleration = 8f;
        private const float StoppingDistance = 0.1f;
        private const float AgentRadius = 0.25f;

        private UnitController controller;
        private StatsModule stats;
        private NavMeshAgent agent;

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            stats = controller.GetModule<StatsModule>();

            // NavMeshAgent é caro de criar/destruir; adiciona uma vez por GameObject e só alterna
            // enabled. Começa desabilitado para não tentar se prender ao NavMesh fora de combate.
            agent = controller.GetComponent<NavMeshAgent>();
            if (agent == null) agent = controller.gameObject.AddComponent<NavMeshAgent>();
            agent.enabled = false;
            Configure();
        }

        public void OnEnabled() { }

        public void OnDisabled() => Deactivate();

        public void Cleanup()
        {
            // O agente vive com o GameObject; aqui (descarregamento de cena) só soltamos a referência.
            agent = null;
            controller = null;
            stats = null;
        }

        /// <summary>Liga o agente para combate e aplica a configuração (velocidade dos stats).</summary>
        public void Activate()
        {
            if (agent == null) return;
            agent.enabled = true;
            Configure();
        }

        /// <summary>Para e desliga o agente (fim de combate/morte/volta ao pool).</summary>
        public void Deactivate()
        {
            Stop();
            if (agent != null) agent.enabled = false;
        }

        private void Configure()
        {
            if (agent == null) return;
            agent.updateRotation = false;
            agent.updateUpAxis = false;          // jogo 2D: navegação no plano XY
            agent.speed = stats?.Speed ?? DefaultSpeed;
            agent.acceleration = Acceleration;
            agent.angularSpeed = 0f;
            agent.stoppingDistance = StoppingDistance;
            agent.radius = AgentRadius;
        }

        /// <summary>Reflete mudanças de velocidade (ex.: debuff de lentidão) no agente.</summary>
        public void SyncSpeed()
        {
            if (agent != null && agent.enabled && stats != null)
                agent.speed = stats.Speed;
        }

        public void MoveTo(Vector3 destination)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        /// <summary>Velocidade atual do agente (usada para flip do sprite).</summary>
        public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

        // === Movimento manual (salto de skill) ===
        // O CombatModule desliga o agente, faz o lerp do transform e o religa no fim.

        /// <summary>Desliga o agente para um movimento manual; retorna se ele estava ligado.</summary>
        public bool BeginManualMove()
        {
            bool wasEnabled = agent != null && agent.enabled;
            if (agent != null) agent.enabled = false;
            return wasEnabled;
        }

        /// <summary>Religa o agente após o movimento manual e o reposiciona no NavMesh.</summary>
        public void EndManualMove(Vector3 position)
        {
            if (agent == null) return;
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(position);
        }
    }
}
