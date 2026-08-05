using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>Forma de área de uma <see cref="TargetQuery"/>: nenhuma, círculo (raio) ou faixa reta (onda).</summary>
    public enum RadiusMode { None, AroundSelf, AroundTarget, Line }

    /// <summary>
    /// Seletor de alvos componível e data-driven: lado + filtro + critério/quantidade + forma de área.
    /// Substitui o antigo TargetMode/TargetResolver. Preenche um buffer fornecido pelo chamador
    /// (sem alocar). Exemplos: alvo único = {CurrentTarget}; todos os aliados = {Allies, count:0};
    /// só aliados-herói = {Allies, requireTags:Hero}; inimigo de menor vida =
    /// {Enemies, criterion:LowestCurrentHealth, count:1}; onda = {Enemies, Line, lineWidth, lineLength}.
    /// </summary>
    [System.Serializable]
    public struct TargetQuery
    {
        public TargetSide side;
        public TargetFilter filter;
        public TargetCriterion criterion;
        [Tooltip("0 = todos; 1 = único; N = top-N.")]
        public int count;
        public RadiusMode radiusMode;
        [Tooltip("Raio do círculo (AroundSelf/AroundTarget).")]
        public float radius;
        [Tooltip("Largura da faixa (Line).")]
        public float lineWidth;
        [Tooltip("Comprimento/alcance da faixa (Line).")]
        public float lineLength;

        public void Resolve(in TargetContext ctx, List<UnitController> results)
        {
            results.Clear();

            // 1. Junta os candidatos do lado escolhido (filtrados + vivos).
            switch (side)
            {
                case TargetSide.CurrentTarget: AddSingle(ctx.currentTarget, results); break;
                case TargetSide.Self: AddSingle(ctx.owner, results); break;
                case TargetSide.Applier: AddSingle(ctx.applier, results); break;
                case TargetSide.Allies: AddGroup(ctx.allies, results); break;
                case TargetSide.Enemies: AddGroup(ctx.enemies, results); break;
                case TargetSide.AllUnits:
                    AddGroup(ctx.allies, results);
                    AddGroup(ctx.enemies, results);
                    break;
            }

            // 2. Aplica a forma de área (círculo ou faixa reta).
            ApplyShape(ctx, results);

            // 3. Ordena/seleciona por critério e quantidade.
            Vector2 origin = ctx.owner != null ? (Vector2)ctx.owner.transform.position : default;
            TargetSelection.Reduce(results, criterion, origin, count);
        }

        private void AddSingle(UnitController unit, List<UnitController> results)
        {
            if (filter.Passes(unit)) results.Add(unit);
        }

        private void AddGroup(IReadOnlyList<UnitController> source, List<UnitController> results)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
                if (filter.Passes(source[i])) results.Add(source[i]);
        }

        private void ApplyShape(in TargetContext ctx, List<UnitController> results)
        {
            if (radiusMode == RadiusMode.None || results.Count == 0) return;

            if (radiusMode == RadiusMode.Line)
            {
                // Faixa reta partindo do dono; direção que cobre mais candidatos.
                if (ctx.owner == null) { results.Clear(); return; }
                LineTargeting.KeepBestLine(ctx.owner.transform.position, results, lineWidth, lineLength);
                return;
            }

            // Círculo (AroundSelf/AroundTarget). radius <= 0 = sem filtro de área.
            if (radius <= 0f) return;
            UnitController center = radiusMode == RadiusMode.AroundTarget ? ctx.currentTarget : ctx.owner;
            if (center == null) { results.Clear(); return; }

            Vector2 c = center.transform.position;
            float sqr = radius * radius;
            for (int i = results.Count - 1; i >= 0; i--)
                if (((Vector2)results[i].transform.position - c).sqrMagnitude > sqr)
                    results.RemoveAt(i);
        }
    }
}
