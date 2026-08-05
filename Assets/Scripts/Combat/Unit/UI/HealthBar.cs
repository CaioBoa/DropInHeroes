using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [Tooltip("Segmento de escudo (Filled Horizontal), DESENHADO ATRÁS do fill de vida. Opcional.")]
        [SerializeField] private Image shieldImage;
        [SerializeField] private float lerpSpeed = 5f;

        [Header("Colors")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f);
        [Tooltip("Cor do segmento de escudo (marcação azul, estilo LoL/TFT).")]
        [SerializeField] private Color shieldColor = new Color(0.4f, 0.75f, 1f, 1f);

        // Preenchimentos normalizados na escala TOTAL (maxHP + escudo): a vida ocupa HP/total e o
        // escudo é o segmento entre a vida e (HP+escudo)/total. O fill de escudo fica ATRÁS do de vida,
        // então a vida cobre a parte de baixo e o azul só aparece no topo (a "fatia" de escudo).
        private float targetFillAmount = 1f;
        private float targetShieldAmount = 0f;
        private Canvas canvas;

        public void Initialize(bool isPlayer)
        {
            canvas = GetComponent<Canvas>();

            if (fillImage != null)
            {
                fillImage.color = isPlayer ? playerColor : enemyColor;
                fillImage.fillAmount = 1f;
            }
            if (shieldImage != null)
            {
                shieldImage.color = shieldColor;
                shieldImage.fillAmount = 0f;
            }

            targetFillAmount = 1f;
            targetShieldAmount = 0f;
        }

        /// <param name="healthNorm">HP / (maxHP + escudo).</param>
        /// <param name="shieldTopNorm">(HP + escudo) / (maxHP + escudo) — topo do segmento de escudo.</param>
        public void SetHealth(float healthNorm, float shieldTopNorm)
        {
            targetFillAmount = Mathf.Clamp01(healthNorm);
            targetShieldAmount = Mathf.Clamp01(shieldTopNorm);
        }

        /// <summary>
        /// Define os preenchimentos imediatamente, sem lerp. Usado ao exibir a barra para combate,
        /// evitando animar a partir de um valor obsoleto enquanto oculta.
        /// </summary>
        public void SnapToHealth(float healthNorm, float shieldTopNorm)
        {
            SetHealth(healthNorm, shieldTopNorm);
            if (fillImage != null) fillImage.fillAmount = targetFillAmount;
            if (shieldImage != null) shieldImage.fillAmount = targetShieldAmount;
        }

        public void ResetToFull()
        {
            targetFillAmount = 1f;
            targetShieldAmount = 0f;
            if (fillImage != null) fillImage.fillAmount = 1f;
            if (shieldImage != null) shieldImage.fillAmount = 0f;
        }

        private void Update()
        {
            if (fillImage != null && Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
                fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, lerpSpeed * Time.deltaTime);

            if (shieldImage != null && Mathf.Abs(shieldImage.fillAmount - targetShieldAmount) > 0.001f)
                shieldImage.fillAmount = Mathf.Lerp(shieldImage.fillAmount, targetShieldAmount, lerpSpeed * Time.deltaTime);
        }

        public void Show()
        {
            if (canvas != null)
                canvas.enabled = true;
        }

        public void Hide()
        {
            if (canvas != null)
                canvas.enabled = false;
        }
    }
}
