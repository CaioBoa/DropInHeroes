using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Parâmetros de um efeito visual. Tudo ajustável pela skill no Inspector.
    /// </summary>
    public struct VfxRequest
    {
        public AnimationClip clip;
        public Transform parent;       // o efeito segue este transform (ex.: o inimigo)
        public Vector3 localPosition;  // posição relativa ao parent; z só importa se a câmera ordena por Z
        public float alpha;            // translucidez base (reaplicada por frame)
        public int sortingLayerId;     // sorting layer (no 2D é o que de fato decide a ordem de render)
        public int sortingOrder;
        public float scale;            // escala absoluta; quando fitDiameter > 0, vira multiplicador de ajuste fino
        public float fitDiameter;      // se > 0, escala o host para que o sprite cubra este diâmetro (mundo)
        public float duration;         // tempo total visível; <= 0 usa o comprimento do clip
        public float fadeOut;          // segundos finais de fade (0 = corta seco)
        public bool flipX;
        public bool flipY;
    }

    /// <summary>
    /// Toca AnimationClips de efeito em hosts genéricos (SpriteRenderer + Animator) gerados por
    /// código e poolados. Sem prefab por habilidade. Auto-instancia.
    /// </summary>
    public class VfxPlayer : MonoBehaviour
    {
        private static VfxPlayer instance;
        public static VfxPlayer Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("VfxPlayer");
                    instance = go.AddComponent<VfxPlayer>();
                }
                return instance;
            }
        }

        private class ActiveVfx
        {
            public GameObject host;
            public SpriteRenderer renderer;
            public PlayableGraph graph;
            public float remaining;
            public float baseAlpha;
            public float fadeOut;
            public float fitDiameter;
            public float scaleMultiplier;
            public bool fitted;
        }

        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly List<ActiveVfx> active = new List<ActiveVfx>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            // Evita o campo estático apontar para um objeto destruído após troca de cena.
            if (instance == this) instance = null;
        }

        // Reset para sessões de Play com domain reload desabilitado.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public void Play(VfxRequest req)
        {
            if (req.clip == null) return;

            GameObject host = Rent();
            Transform t = host.transform;
            t.SetParent(req.parent != null ? req.parent : transform, false);
            t.localPosition = req.localPosition;
            t.localRotation = Quaternion.identity;

            float scale = req.scale <= 0f ? 1f : req.scale;
            // Com fitDiameter, a escala final é calculada quando o sprite existir (ver LateUpdate).
            t.localScale = req.fitDiameter > 0f ? Vector3.zero : Vector3.one * scale;

            var sr = host.GetComponent<SpriteRenderer>();
            sr.flipX = req.flipX;
            sr.flipY = req.flipY;
            sr.sortingLayerID = req.sortingLayerId;
            sr.sortingOrder = req.sortingOrder;
            Color c = sr.color;
            c.a = req.alpha;
            sr.color = c;

            host.SetActive(true);

            var animator = host.GetComponent<Animator>();
            PlayableGraph graph = ClipPlayer.Play(animator, req.clip);

            active.Add(new ActiveVfx
            {
                host = host,
                renderer = sr,
                graph = graph,
                remaining = req.duration > 0f ? req.duration : req.clip.length,
                baseAlpha = req.alpha,
                fadeOut = req.fadeOut,
                fitDiameter = req.fitDiameter,
                scaleMultiplier = scale,
                fitted = false
            });
        }

        // LateUpdate: roda DEPOIS do Animator avaliar o clip, então a translucidez que aplicamos
        // não é sobrescrita pelas cores do próprio clip.
        private void LateUpdate()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ActiveVfx a = active[i];

                // Host pode ter sido destruído junto com o inimigo-pai.
                if (a.host == null || a.renderer == null)
                {
                    ClipPlayer.Stop(ref a.graph);
                    active.RemoveAt(i);
                    continue;
                }

                // Ajusta a escala para casar com um diâmetro-alvo (ex.: hitbox da AoE) assim que o sprite existir.
                if (a.fitDiameter > 0f && !a.fitted && a.renderer.sprite != null)
                {
                    float spriteWidth = a.renderer.sprite.bounds.size.x;
                    if (spriteWidth > 0f)
                    {
                        a.host.transform.localScale = Vector3.one * (a.fitDiameter / spriteWidth * a.scaleMultiplier);
                        a.fitted = true;
                    }
                }

                a.remaining -= Time.deltaTime;

                float alpha = a.baseAlpha;
                if (a.fadeOut > 0f && a.remaining < a.fadeOut)
                    alpha = a.baseAlpha * Mathf.Clamp01(a.remaining / a.fadeOut);

                Color c = a.renderer.color;
                c.a = alpha;
                a.renderer.color = c;

                if (a.remaining > 0f) continue;

                ClipPlayer.Stop(ref a.graph);
                a.host.transform.SetParent(transform);
                a.host.SetActive(false);
                pool.Enqueue(a.host);
                active.RemoveAt(i);
            }
        }

        private GameObject Rent()
        {
            while (pool.Count > 0)
            {
                GameObject pooled = pool.Dequeue();
                if (pooled != null) return pooled;
            }
            return new GameObject("Vfx", typeof(SpriteRenderer), typeof(Animator));
        }
    }
}
