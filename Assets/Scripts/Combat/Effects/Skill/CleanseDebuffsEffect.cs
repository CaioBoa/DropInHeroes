using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// PURIFICA os alvos resolvidos: remove até N debuffs de cada um (mais antigos primeiro, via
    /// <see cref="StatusModule.RemoveDebuffs"/>). Reutilizável — ex.: Totem Boliviano remove 2 debuffs
    /// de todos os aliados. Remover um efeito de controle também libera a unidade imobilizada.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Cleanse Debuffs")]
    public class CleanseDebuffsEffect : SkillEffect
    {
        [Tooltip("Quantos debuffs remover de cada alvo (mais antigos primeiro).")]
        [SerializeField] private int count = 1;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var stats = targets[i].Stats;
                if (stats == null || stats.IsDead) continue;
                targets[i].GetModule<StatusModule>()?.RemoveDebuffs(count);
            }
        }
    }
}
