using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Resolve acerto/esquiva de um ATAQUE (efeitos on-hit de uma skill) contra cada alvo inimigo.
    /// A esquiva é rolada UMA vez por (ataque, alvo) e cacheada — assim todas as fontes de dano/efeito
    /// daquele ataque compartilham o mesmo resultado: esquivar anula o ataque inteiro para o alvo, não
    /// cada fonte separadamente. chanceDeAcerto = clamp01((Accuracy − Dodge) / 100).
    /// Aliados/self nunca esquivam. DoT e efeitos fora do on-hit (onCast, always-hit) não passam por aqui.
    /// </summary>
    public static class HitChanceResolver
    {
        private const float AccuracyScale = 100f; // Accuracy/Dodge em pontos (100 = 100%).

        /// <summary>Remove do buffer os alvos inimigos que esquivaram deste ataque (usa/preenche o cache).</summary>
        public static void FilterDodged(in SkillContext context, List<UnitController> buffer, Dictionary<UnitController, bool> rolls)
        {
            if (rolls == null) return;
            for (int i = buffer.Count - 1; i >= 0; i--)
                if (Dodges(context, buffer[i], rolls)) buffer.RemoveAt(i);
        }

        private static bool Dodges(in SkillContext context, UnitController target, Dictionary<UnitController, bool> rolls)
        {
            if (target == null || context.owner == null) return false;
            if (target.GetTeam() == context.owner.GetTeam()) return false; // só inimigos esquivam

            if (rolls.TryGetValue(target, out bool hit)) return !hit; // já decidido para este ataque

            float accuracy = context.ownerStats?.Accuracy ?? AccuracyScale;
            float dodge = target.Stats?.Dodge ?? 0f;
            float hitChance = Mathf.Clamp01((accuracy - dodge) / AccuracyScale);
            hit = Random.value < hitChance;
            rolls[target] = hit;
            return !hit;
        }
    }
}
