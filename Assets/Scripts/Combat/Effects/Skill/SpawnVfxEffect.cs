using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Toca um efeito visual (<see cref="VfxPlayer"/>) sobre cada alvo resolvido. Herda a sorting
    /// layer do alvo e renderiza acima dele. Cobre tanto o impacto único no centro de uma AoE
    /// (alvo = CurrentTarget, <see cref="fitDiameter"/> = diâmetro do raio) quanto um efeito por
    /// inimigo (alvo = Enemies, <see cref="followTarget"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skill Effects/Spawn VFX")]
    public class SpawnVfxEffect : SkillEffect
    {
        [Header("Clipe")]
        [SerializeField] private AnimationClip clip;
        [Range(0f, 1f)] [SerializeField] private float alpha = 1f;
        [Tooltip("Escala absoluta; quando fitDiameter > 0, vira multiplicador de ajuste fino.")]
        [SerializeField] private float scale = 1f;
        [Tooltip("Se > 0, escala o efeito para cobrir este diâmetro (mundo). Use o diâmetro da AoE.")]
        [SerializeField] private float fitDiameter = 0f;
        [Tooltip("Ordem de render acima do alvo (sortingOrder dele + este valor).")]
        [SerializeField] private int sortingOffset = 1;

        [Header("Posicionamento")]
        [Tooltip("Marque para o efeito seguir o alvo (parent). Desmarque para fixá-lo na posição do alvo (snapshot).")]
        [SerializeField] private bool followTarget = false;
        [Tooltip("Offset no eixo Z (só afeta ordem se a câmera ordena por Z).")]
        [SerializeField] private float zOffset = 0f;

        [Header("Tempo")]
        [Tooltip("Tempo total visível, em segundos. 0 = comprimento do clipe.")]
        [SerializeField] private float duration = 0f;
        [Tooltip("Segundos finais de fade-out (0 = corta seco).")]
        [SerializeField] private float fadeOut = 0f;
        [SerializeField] private bool flipX = false;
        [SerializeField] private bool flipY = false;

        public override void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state)
        {
            if (clip == null || targets.Count == 0) return;

            for (int i = 0; i < targets.Count; i++)
            {
                UnitController target = targets[i];

                // O SpriteRenderer do alvo vive no filho "Visual" — usar o módulo (GetComponent no
                // root retornaria null e o efeito cairia na sorting layer 0).
                int layerId = 0;
                int order = sortingOffset;
                var renderer = target.Visual?.SpriteRenderer;
                if (renderer != null)
                {
                    layerId = renderer.sortingLayerID;
                    order = renderer.sortingOrder + sortingOffset;
                }

                VfxPlayer.Instance.Play(new VfxRequest
                {
                    clip = clip,
                    parent = followTarget ? target.transform : null,
                    localPosition = followTarget ? new Vector3(0f, 0f, zOffset) : target.transform.position,
                    alpha = alpha,
                    sortingLayerId = layerId,
                    sortingOrder = order,
                    scale = scale,
                    fitDiameter = fitDiameter,
                    duration = duration,
                    fadeOut = fadeOut,
                    flipX = flipX,
                    flipY = flipY
                });
            }
        }
    }
}
