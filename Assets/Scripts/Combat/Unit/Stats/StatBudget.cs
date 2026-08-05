using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Compõe o valor BRUTO de um stat base de uma unidade a partir do orçamento por pontos:
    /// bruto = GeneralBaseStats[stat] + pontos_da_unidade[stat] × StatDefinition.statPointValue.
    /// Fonte única usada por StatsModule (spawn) e pelos painéis/preview — garante que display e
    /// combate nunca divirjam. Carrega catálogo + piso geral de GameConfig.Active (cacheados).
    /// </summary>
    public static class StatBudget
    {
        private static StatDefinitionCatalog catalog;
        private static GeneralBaseStats general;

        private static void EnsureLoaded()
        {
            if (catalog == null) catalog = GameConfig.Active?.StatDefinitions;
            if (general == null) general = GameConfig.Active?.BaseStats;
        }

        /// <summary>Valor bruto do stat base: piso geral + pontos da unidade × valor-por-ponto do stat.
        /// Invocações (<see cref="SummonData"/>) NÃO usam piso nem pontos — só os stats fixos delas.</summary>
        public static float ComputeBaseStat(CharacterData character, StatType type)
        {
            EnsureLoaded();
            float generalBase = general != null ? general.Get(type) : 0f;
            if (character == null) return generalBase;

            // MaxEnergy não é um stat de orçamento: é o custo do supremo (SupremeSkill.energyCost).
            // Sem supremo: herói cai no piso; invocação fica 0 (não usa energia).
            if (type == StatType.MaxEnergy)
                return character.supremeSkill != null ? character.supremeSkill.energyCost
                     : (character is SummonData ? 0f : generalBase);

            if (character is SummonData summon)
                return summon.GetFixedStat(type);

            return generalBase + PointsToValue(type, character.GetStatPoints(type));
        }

        /// <summary>
        /// Converte pontos em valor bruto de um stat (pontos × statPointValue). Fonte ÚNICA da conversão —
        /// reusada por base, árvore de stats e artefato, para que nerf/buff de um stat (via statPointValue)
        /// se propague a tudo de uma vez.
        /// </summary>
        public static float PointsToValue(StatType type, float points)
        {
            EnsureLoaded();
            float pointValue = catalog != null ? (catalog.GetStatDefinition(type)?.statPointValue ?? 1f) : 1f;
            return points * pointValue;
        }
    }
}
