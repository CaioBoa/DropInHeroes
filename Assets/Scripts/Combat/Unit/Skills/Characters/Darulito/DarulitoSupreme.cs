using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Supremo do Darulito — "Totem da Libertação": no impacto, concede um escudo a todos os aliados (escala
    /// com Defesa e Defesa Mágica) e invoca o Totem atrás de si (recast substitui o anterior). O escudo é um
    /// módulo; a invocação com posicionamento "atrás" e recast-replace é lógica própria (estado currentTotem).
    /// O totem age sozinho (conjurador sem ataque — autocasta o próprio supremo, ver CombatModule).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Darulito/Supreme")]
    public class DarulitoSupreme : ModularSupreme
    {
        [Header("Escudo aos aliados")]
        [Tooltip("Multiplicador da Defesa no escudo (2.0 = 200%).")]
        [SerializeField] private float shieldDefenseScaling = 2.0f;
        [Tooltip("Multiplicador da Defesa Mágica no escudo (2.0 = 200%).")]
        [SerializeField] private float shieldMagicDefenseScaling = 2.0f;
        [SerializeField] private string shieldId = "darulito_supreme_shield";

        [Header("Totem")]
        [Tooltip("SummonData do totem. Stats fixos + derivação do Darulito aplicados no spawn.")]
        [SerializeField] private SummonData totemData;
        [Tooltip("Distância atrás do Darulito onde o totem aparece.")]
        [SerializeField] private float spawnOffset = 1.5f;

        private StatScaling[] shieldScalings;
        private UnitController currentTotem;

        protected override void OnImpact(SkillContext context, ref EffectRunState state)
        {
            shieldScalings ??= new[]
            {
                new StatScaling(StatType.Defense, shieldDefenseScaling),
                new StatScaling(StatType.MagicalDefense, shieldMagicDefenseScaling)
            };

            SkillModules.Shield(context, Targets.Allies(context, includeSelf: true), shieldId, shieldScalings, ref state);
            SpawnTotem(context);
        }

        public override void Clear()
        {
            base.Clear();
            currentTotem = null;
        }

        private void SpawnTotem(SkillContext context)
        {
            if (totemData == null || context.owner == null || CombatController.Instance == null) return;

            DespawnCurrent(); // recast substitui o totem anterior

            Vector3 spawnPos = ComputeSpawnPosition(context);
            currentTotem = CombatController.Instance.SpawnSummon(totemData, context.owner.GetTeam(), spawnPos, context.owner);
        }

        private void DespawnCurrent()
        {
            if (currentTotem != null) CombatController.Instance.DespawnSummon(currentTotem);
            currentTotem = null;
        }

        // Atrás = lado oposto ao inimigo mais próximo (protege o totem do foco imediato).
        private Vector3 ComputeSpawnPosition(SkillContext context)
        {
            Vector3 ownerPos = context.owner.transform.position;
            UnitController nearest = FindNearestEnemy(context.enemies, ownerPos);

            Vector3 behind;
            if (nearest != null)
            {
                Vector3 toEnemy = nearest.transform.position - ownerPos;
                Vector3 dir = toEnemy.sqrMagnitude > 0.001f ? toEnemy.normalized : Vector3.right;
                behind = ownerPos - dir * spawnOffset;
            }
            else
            {
                behind = ownerPos + Vector3.left * spawnOffset;
            }

            behind.z = ownerPos.z;
            return behind;
        }

        private static UnitController FindNearestEnemy(IReadOnlyList<UnitController> enemies, Vector3 from)
        {
            if (enemies == null) return null;

            UnitController nearest = null;
            float minSqr = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                UnitController e = enemies[i];
                if (e == null) continue;
                StatsModule st = e.Stats;
                if (st == null || st.IsDead) continue;

                float d = (e.transform.position - from).sqrMagnitude;
                if (d < minSqr) { minSqr = d; nearest = e; }
            }
            return nearest;
        }
    }
}
