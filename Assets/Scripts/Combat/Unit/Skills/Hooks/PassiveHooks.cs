using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class PassiveHooks
    {
        // Combat lifecycle
        public event Action onBattleStart;
        public event Action onBattleEnd;

        // Unit events
        public event Action onUnitDeath;
        public event Action<float> onDamageTaken;
        public event Action<float> onDamageDealt;
        public event Action<float> onHealReceived;
        // Um golpe ofensivo acertou ESTA unidade (não conta DoT nem dano compartilhado). Arg = atacante.
        public event Action<UnitController> onAttackReceived;
        // Um golpe DESTA unidade causou dano CRÍTICO (só golpes que podem critar — não DoT nem riders on-hit).
        // Arg = alvo atingido. Base para passivas de crítico (ex.: acumular condição ao critar).
        public event Action<UnitController> onCriticalHit;
        public event Action onBeforeAttack;
        public event Action onAfterAttack;

        // Energy events
        public event Action onEnergyFull;
        public event Action onSupremeUsed;

        // === Invoke Methods ===

        public void InvokeBattleStart() => onBattleStart?.Invoke();
        public void InvokeBattleEnd() => onBattleEnd?.Invoke();
        public void InvokeUnitDeath() => onUnitDeath?.Invoke();
        public void InvokeDamageTaken(float amount) => onDamageTaken?.Invoke(amount);
        public void InvokeDamageDealt(float amount) => onDamageDealt?.Invoke(amount);
        public void InvokeHealReceived(float amount) => onHealReceived?.Invoke(amount);
        public void InvokeAttackReceived(UnitController attacker) => onAttackReceived?.Invoke(attacker);
        public void InvokeCriticalHit(UnitController target) => onCriticalHit?.Invoke(target);
        public void InvokeBeforeAttack() => onBeforeAttack?.Invoke();
        public void InvokeAfterAttack() => onAfterAttack?.Invoke();
        public void InvokeEnergyFull() => onEnergyFull?.Invoke();
        public void InvokeSupremeUsed() => onSupremeUsed?.Invoke();

        public void ClearAll()
        {
            onBattleStart = null;
            onBattleEnd = null;
            onUnitDeath = null;
            onDamageTaken = null;
            onDamageDealt = null;
            onHealReceived = null;
            onAttackReceived = null;
            onCriticalHit = null;
            onBeforeAttack = null;
            onAfterAttack = null;
            onEnergyFull = null;
            onSupremeUsed = null;
        }
    }
}
