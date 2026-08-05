using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class HealthBarModule : IUnitModule
    {
        private UnitController controller;
        private StatsModule stats;
        private ShieldModule shield;
        private HealthBar healthBar;

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            stats = controller.GetModule<StatsModule>();
            shield = controller.GetModule<ShieldModule>();

            healthBar = controller.GetComponentInChildren<HealthBar>(true);

            if (healthBar == null)
            {
                DebugManager.LogWarning("HealthBar não encontrado no prefab!", DebugCategory.Combat);
                return;
            }

            bool isPlayer = controller.IsPlayerUnit();
            healthBar.Initialize(isPlayer);

            var healthResource = stats?.GetResourceObject(ResourceType.Health);
            if (healthResource != null)
                healthResource.OnValueChanged += OnHealthChanged;

            // Escudo absorve ANTES da vida: quando ele muda sem a vida mudar (dano só no escudo,
            // ganho/quebra de escudo), a barra precisa refletir a fatia azul mesmo assim.
            if (shield != null)
                shield.OnChanged += RefreshBar;

            healthBar.Hide();
        }

        private void OnHealthChanged(float _) => RefreshBar();

        private void RefreshBar()
        {
            if (healthBar == null || stats == null) return;
            ComputeNorms(out float healthNorm, out float shieldTopNorm);
            healthBar.SetHealth(healthNorm, shieldTopNorm);
        }

        // Escala TOTAL = maxHP + escudo (reescala): a vida ocupa HP/total, o escudo a fatia até
        // (HP+escudo)/total. Sem escudo, total = maxHP e o comportamento é o de sempre.
        private void ComputeNorms(out float healthNorm, out float shieldTopNorm)
        {
            float hp = stats.CurrentHealth;
            float shieldAmount = shield != null ? shield.TotalShield : 0f;
            float total = stats.MaxHealth + shieldAmount;
            if (total <= 0f) { healthNorm = 0f; shieldTopNorm = 0f; return; }
            healthNorm = hp / total;
            shieldTopNorm = (hp + shieldAmount) / total;
        }

        public void OnEnabled()
        {
            if (healthBar != null && stats != null)
            {
                ComputeNorms(out float healthNorm, out float shieldTopNorm);
                healthBar.SnapToHealth(healthNorm, shieldTopNorm);
            }
            healthBar?.Show();
        }

        public void OnDisabled()
        {
            healthBar?.Hide();
        }

        public void Cleanup()
        {
            var healthResource = stats?.GetResourceObject(ResourceType.Health);
            if (healthResource != null)
                healthResource.OnValueChanged -= OnHealthChanged;

            if (shield != null)
                shield.OnChanged -= RefreshBar;

            healthBar = null;
            stats = null;
            shield = null;
            controller = null;
        }

        public void ResetForPool()
        {
            healthBar?.ResetToFull();
            healthBar?.Hide();
        }
    }
}
