using UnityEngine;
using UnityEngine.Playables;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Toca um AnimationClip diretamente num Animator via Playables, sem precisar de
    /// AnimatorController. Usado por hosts genéricos de VFX/projétil (sem prefab por habilidade).
    /// O PlayableGraph retornado deve ser destruído (Stop) quando o efeito terminar.
    /// </summary>
    public static class ClipPlayer
    {
        public static PlayableGraph Play(Animator animator, AnimationClip clip)
        {
            AnimationPlayableUtilities.PlayClip(animator, clip, out PlayableGraph graph);
            return graph;
        }

        public static void Stop(ref PlayableGraph graph)
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
