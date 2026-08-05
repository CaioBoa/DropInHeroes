using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class EnergyBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private float lerpSpeed = 5f;

        [Header("Colors")]
        [SerializeField] private Color energyColor = new Color(1f, 0.85f, 0.15f);

        private float targetFillAmount = 0f;
        private Canvas canvas;

        public void Initialize()
        {
            canvas = GetComponent<Canvas>();

            if (fillImage != null)
            {
                fillImage.color = energyColor;
                fillImage.fillAmount = 0f;
            }

            targetFillAmount = 0f;
        }

        public void SetEnergyPercent(float percent)
        {
            targetFillAmount = Mathf.Clamp01(percent);
        }

        /// <summary>
        /// Define o preenchimento imediatamente, sem lerp. Usado ao exibir a barra
        /// para combate, evitando animar a partir de um valor obsoleto enquanto oculta.
        /// </summary>
        public void SnapToPercent(float percent)
        {
            targetFillAmount = Mathf.Clamp01(percent);
            if (fillImage != null)
                fillImage.fillAmount = targetFillAmount;
        }

        public void ResetToEmpty()
        {
            targetFillAmount = 0f;
            if (fillImage != null)
                fillImage.fillAmount = 0f;
        }

        private void Update()
        {
            if (fillImage == null) return;

            if (Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
            {
                fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, lerpSpeed * Time.deltaTime);
            }
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
