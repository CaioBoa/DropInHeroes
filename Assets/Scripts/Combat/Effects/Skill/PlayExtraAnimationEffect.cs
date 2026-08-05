using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Toca uma animação EXTRA (one-shot) por chave nos alvos resolvidos — via
    /// <see cref="VisualModule.PlayExtraAnimation"/> e <see cref="Data.CharacterData.extraAnimations"/>.
    /// Para poses próprias de skills/passivas (ex.: uma passiva ativável que dispara uma pose de
    /// conjuração). Use target = Self para o próprio conjurador. A chave precisa existir na unidade
    /// que vai animar; senão é no-op (com aviso).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Play Extra Animation")]
    public class PlayExtraAnimationEffect : SkillEffect
    {
        [Tooltip("Chave da animação extra no CharacterData da unidade que vai animar (ex.: \"cast_especial\").")]
        [SerializeField] private string animationKey;
        [Tooltip("Multiplicador de velocidade da animação (1 = normal).")]
        [SerializeField] private float speedMultiplier = 1f;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (string.IsNullOrEmpty(animationKey)) return;
            for (int i = 0; i < targets.Count; i++)
                targets[i].Visual?.PlayExtraAnimation(animationKey, speedMultiplier);
        }
    }
}
