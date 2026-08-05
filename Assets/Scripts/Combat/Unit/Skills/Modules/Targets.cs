using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Resolução de alvos em CÓDIGO — substitui o TargetQuery serializado no asset. As habilidades
    /// chamam estes helpers para dizer QUEM cada módulo afeta. Retornam listas novas (uso em passivas
    /// e conjuração, não por frame — alocação aceitável fora do hot path).
    /// </summary>
    public static class Targets
    {
        /// <summary>Aliados do dono (a própria equipe). includeSelf=false remove o próprio dono.</summary>
        public static List<UnitController> Allies(in SkillContext context, bool includeSelf)
        {
            var result = new List<UnitController>();
            var pool = context.allies;
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                {
                    UnitController u = pool[i];
                    if (u != null && (includeSelf || u != context.owner)) result.Add(u);
                }
            return result;
        }

        /// <summary>Inimigos do dono (a equipe oposta).</summary>
        public static List<UnitController> Enemies(in SkillContext context)
        {
            var result = new List<UnitController>();
            var pool = context.enemies;
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i] != null) result.Add(pool[i]);
            return result;
        }

        /// <summary>Só o próprio dono.</summary>
        public static List<UnitController> Self(in SkillContext context)
        {
            var result = new List<UnitController>();
            if (context.owner != null) result.Add(context.owner);
            return result;
        }

        /// <summary>O alvo atual da skill (o foco). Vazio se não houver.</summary>
        public static List<UnitController> Focus(in SkillContext context)
        {
            var result = new List<UnitController>();
            if (context.target != null) result.Add(context.target);
            return result;
        }

        /// <summary>Unidades do lado escolhido dentro do raio ao redor do DONO (círculo AoE). Só vivas.</summary>
        public static List<UnitController> AroundSelf(in SkillContext context, float radius, bool enemies)
        {
            var result = new List<UnitController>();
            var pool = enemies ? context.enemies : context.allies;
            if (pool == null || context.owner == null) return result;
            Vector2 center = context.owner.transform.position;
            float sqr = radius * radius;
            for (int i = 0; i < pool.Count; i++)
            {
                UnitController u = pool[i];
                if (u == null || u.Stats == null || u.Stats.IsDead) continue;
                if (((Vector2)u.transform.position - center).sqrMagnitude <= sqr) result.Add(u);
            }
            return result;
        }

        /// <summary>Unidades do lado escolhido dentro do raio ao redor do ALVO ATUAL. Só vivas.</summary>
        public static List<UnitController> AroundTarget(in SkillContext context, float radius, bool enemies)
        {
            var result = new List<UnitController>();
            var pool = enemies ? context.enemies : context.allies;
            if (pool == null || context.target == null) return result;
            Vector2 center = context.target.transform.position;
            float sqr = radius * radius;
            for (int i = 0; i < pool.Count; i++)
            {
                UnitController u = pool[i];
                if (u == null || u.Stats == null || u.Stats.IsDead) continue;
                if (((Vector2)u.transform.position - center).sqrMagnitude <= sqr) result.Add(u);
            }
            return result;
        }

        /// <summary>Unidades do lado escolhido na melhor FAIXA reta (onda) partindo do dono. Só vivas.</summary>
        public static List<UnitController> Line(in SkillContext context, float width, float length, bool enemies)
        {
            var result = new List<UnitController>();
            var pool = enemies ? context.enemies : context.allies;
            if (pool == null || context.owner == null) return result;
            for (int i = 0; i < pool.Count; i++)
            {
                UnitController u = pool[i];
                if (u != null && u.Stats != null && !u.Stats.IsDead) result.Add(u);
            }
            LineTargeting.KeepBestLine(context.owner.transform.position, result, width, length);
            return result;
        }

        /// <summary>Os 'count' inimigos (ou aliados) de MENOR vida atual.</summary>
        public static List<UnitController> LowestHealth(in SkillContext context, int count, bool enemies)
        {
            List<UnitController> result = enemies ? Enemies(context) : Allies(context, includeSelf: true);
            Vector2 origin = context.owner != null ? (Vector2)context.owner.transform.position : default;
            TargetSelection.Reduce(result, TargetCriterion.LowestCurrentHealth, origin, count);
            return result;
        }
    }
}
