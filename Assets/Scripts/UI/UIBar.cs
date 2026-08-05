using UnityEngine;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Barra de progresso simples (trilho + fill). Define o preenchimento via <see cref="SetValue(float)"/>
    /// (0..1), ajustando o <c>anchorMax.x</c> do Fill. Reutilizável para HP, energia, timer, loading.
    /// Opcionalmente aceita um <see cref="shieldFill"/> (fatia de escudo, desenhado ATRÁS do fill) — só o
    /// HP usa, via <see cref="SetValue(float,float)"/>.
    /// </summary>
    public class UIBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [Tooltip("Fatia de escudo (azul), DESENHADA ATRÁS do fill principal. Opcional — só HP usa.")]
        [SerializeField] private RectTransform shieldFill;

        public void SetValue(float t01)
        {
            SetEdge(fill, t01);
            SetEdge(shieldFill, 0f); // usos sem escudo (energia/timer) zeram a fatia
        }

        /// <param name="health01">HP / (maxHP + escudo).</param>
        /// <param name="shieldTop01">(HP + escudo) / (maxHP + escudo) — topo da fatia de escudo.</param>
        public void SetValue(float health01, float shieldTop01)
        {
            SetEdge(fill, health01);
            SetEdge(shieldFill, shieldTop01);
        }

        private static void SetEdge(RectTransform rt, float t01)
        {
            if (rt == null) return;
            Vector2 a = rt.anchorMax;
            a.x = Mathf.Clamp01(t01);
            rt.anchorMax = a;
        }
    }
}
