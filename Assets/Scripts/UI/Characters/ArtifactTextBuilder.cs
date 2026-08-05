using System.Text;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Fonte única do texto de explicação de um artefato: descrição + grants de stat (pontos/%) + as linhas
    /// de cada efeito (via o mesmo formatter das habilidades, números sempre visíveis). Usado no overview
    /// (ao lado do ícone) e em cada linha do modal de seleção, para não duplicar a montagem do texto.
    /// </summary>
    public static class ArtifactTextBuilder
    {
        public static string Summary(ArtifactData a, StatDefinitionCatalog cat, KeywordCatalog kw)
        {
            if (a == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(Header(a, cat));
            AppendEffects(sb, a.onHitEffects, cat, kw);
            AppendEffects(sb, a.summonOnHitEffects, cat, kw);
            AppendEffects(sb, a.onBattleStartEffects, cat, kw);

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Cabeçalho do artefato: descrição + grants de stat, SEM as linhas de efeito. Usado como texto-base
        /// no tooltip de hover — os efeitos vêm da pipeline de <see cref="ISkillEffectSource"/> (com keywords
        /// e SHIFT, igual às skills). Termina com quebra de linha (o Summary o concatena com os efeitos).
        /// </summary>
        public static string Header(ArtifactData a, StatDefinitionCatalog cat)
        {
            if (a == null) return string.Empty;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(a.description)) sb.AppendLine(a.description);

            if (a.statGrants != null)
            {
                for (int i = 0; i < a.statGrants.Length; i++)
                {
                    StatGrant g = a.statGrants[i];
                    StatDefinition def = cat != null ? cat.GetStatDefinition(g.stat) : null;
                    string statName = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : g.stat.ToString();
                    if (g.points != 0f) sb.AppendLine($"+{g.points:0.#} pt {statName}");
                    if (g.percent != 0f) sb.AppendLine($"{(g.percent > 0f ? "+" : string.Empty)}{Mathf.RoundToInt(g.percent * 100f)}% {statName}");
                }
            }

            return sb.ToString();
        }

        private static void AppendEffects(StringBuilder sb, SkillEffect[] effects, StatDefinitionCatalog cat, KeywordCatalog kw)
        {
            if (effects == null) return;
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffect fx = effects[i];
                if (fx == null || string.IsNullOrEmpty(fx.description)) continue;
                string line = AbilityTextFormatter.Format(fx.description, fx, kw, cat, true).richText;
                if (!string.IsNullOrEmpty(line)) sb.AppendLine(line);
            }
        }
    }
}
