using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{

    public class LoadingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI percentageText;

        [Header("Animation (Optional)")]
        [SerializeField] private bool animateProgress = true;
        [SerializeField] private float animationSpeed = 2f;

        private float targetProgress = 0f;
        private float currentProgress = 0f;

        private void Update()
        {
            if (animateProgress && currentProgress < targetProgress)
            {
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, animationSpeed * Time.deltaTime);
                UpdateVisuals();
            }
        }

        public void UpdateProgress(float progress, string message)
        {
            targetProgress = Mathf.Clamp01(progress);

            if (!animateProgress)
            {
                currentProgress = targetProgress;
                UpdateVisuals();
            }

            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        private void UpdateVisuals()
        {
            if (progressBar != null)
            {
                progressBar.value = currentProgress;
            }

            if (percentageText != null)
            {
                percentageText.text = $"{currentProgress * 100:F0}%";
            }
        }
    }
}
