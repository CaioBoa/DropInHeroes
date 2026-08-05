using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{
    /// <summary>
    /// Resolve a apresentação visual de um <see cref="StatusEffect"/>: o ícone (próprio ou derivado
    /// do stat modificado, via <see cref="StatDefinitionCatalog"/>) e o texto do indicador (no máximo
    /// um número: duração restante OU stacks). Fonte única usada pelo painel CharacterInfo e pela
    /// pilha de status acima da barra de vida. A cor vem da <see cref="StatusEffect.Kind"/> e é
    /// aplicada por cada consumidor (cores configuráveis no Inspector).
    /// </summary>
    public static class StatusVisuals
    {
        /// <summary>Ícone do status: o próprio (da definição), ou (se for buff/debuff de stat) o ícone do stat.</summary>
        public static Sprite ResolveIcon(StatusEffect fx, StatDefinitionCatalog catalog)
        {
            if (fx == null) return null;
            if (fx.Icon != null) return fx.Icon;
            if (catalog != null && fx.Modifications != null && fx.Modifications.Length > 0)
            {
                var def = catalog.GetStatDefinition(fx.Modifications[0].stat);
                if (def != null) return def.icon;
            }
            return null;
        }

        /// <summary>Nome de exibição do status (da definição-tipo); fallback para o id.</summary>
        public static string DisplayName(StatusEffect fx)
        {
            if (fx == null) return string.Empty;
            if (fx.TypeDef != null && !string.IsNullOrEmpty(fx.TypeDef.displayName)) return fx.TypeDef.displayName;
            return fx.Id ?? string.Empty;
        }

        /// <summary>Descrição do status (da definição-tipo); vazia se não houver.</summary>
        public static string Description(StatusEffect fx)
            => fx != null && fx.TypeDef != null ? fx.TypeDef.description : string.Empty;

        /// <summary>Texto do indicador (no máximo um número): duração ou stacks; vazio se nenhum.</summary>
        public static string IndicatorText(StatusEffect fx)
        {
            if (fx == null) return string.Empty;
            switch (fx.Indicator)
            {
                case StatusIndicator.Duration:
                    return fx.Duration > 0f ? Mathf.CeilToInt(fx.Remaining).ToString() : string.Empty;
                case StatusIndicator.Stacks:
                    return fx.Stacks > 1 ? fx.Stacks.ToString() : string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
