using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Contexto de uma instância de dano, passado aos modificadores de dano de saída/entrada.
    /// Permite regras condicionais à FONTE/ALVO (ex.: "sofre menos dano DE invocações",
    /// "invocações causam crítico", "mais dano CONTRA heróis").
    /// </summary>
    public readonly struct DamageContext
    {
        public readonly StatsModule source;
        public readonly StatsModule target;
        public readonly UnitTag sourceTags;
        public readonly UnitTag targetTags;

        public DamageContext(StatsModule source, StatsModule target, UnitTag sourceTags, UnitTag targetTags)
        {
            this.source = source;
            this.target = target;
            this.sourceTags = sourceTags;
            this.targetTags = targetTags;
        }

        public bool SourceHasTag(UnitTag tag) => (sourceTags & tag) != 0;
        public bool TargetHasTag(UnitTag tag) => (targetTags & tag) != 0;
    }
}
