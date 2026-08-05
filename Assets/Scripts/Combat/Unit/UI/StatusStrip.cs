using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{
    /// <summary>
    /// Pilha world-space de ícones de status acima da barra de vida — versão simples: só ícones
    /// coloridos pela categoria do status, SEM números. Alimentada pelo <see cref="StatusStripModule"/>.
    /// O GameObject se auto-oculta quando não há status.
    /// </summary>
    public class StatusStrip : MonoBehaviour
    {
        [Tooltip("Prefab do pip: raiz = caixa (Image), filho \"Icon\" (Image) = sprite do status.")]
        [SerializeField] private GameObject pipPrefab;

        [Header("Cores por categoria")]
        [SerializeField] private Color positive = new Color(0.42f, 0.78f, 0.5f, 1f);
        [SerializeField] private Color negative = new Color(0.86f, 0.4f, 0.46f, 1f);
        [SerializeField] private Color neutral = new Color(0.45f, 0.6f, 0.85f, 1f);

        private readonly List<Pip> pips = new List<Pip>();
        private class Pip { public GameObject go; public Image box; public Image icon; }

        public void SetEffects(IReadOnlyList<StatusEffect> effects, StatDefinitionCatalog catalog)
        {
            int n = effects != null ? effects.Count : 0;

            for (int i = 0; i < pips.Count; i++)
                pips[i].go.SetActive(i < n);

            for (int i = 0; i < n; i++)
            {
                if (i >= pips.Count) pips.Add(CreatePip());
                var pip = pips[i];
                var fx = effects[i];
                pip.go.SetActive(true);
                pip.box.color = CategoryColor(fx.Kind);
                var sprite = StatusVisuals.ResolveIcon(fx, catalog);
                pip.icon.sprite = sprite;
                pip.icon.enabled = sprite != null; // sem sprite => mostra só a caixa colorida
            }

            gameObject.SetActive(n > 0); // some quando não há status
        }

        private Color CategoryColor(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Negative: return negative;
                case StatusKind.Neutral: return neutral;
                default: return positive;
            }
        }

        private Pip CreatePip()
        {
            var go = Instantiate(pipPrefab, transform);
            return new Pip
            {
                go = go,
                box = go.GetComponent<Image>(),
                icon = go.transform.Find("Icon").GetComponent<Image>()
            };
        }
    }
}
