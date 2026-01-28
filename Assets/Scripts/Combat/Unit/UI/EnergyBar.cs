using UnityEngine;
using UnityEngine.UI;

public class EnergyBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Colors")]
    [SerializeField] private Color energyColor = new Color(0.2f, 0.6f, 0.9f);

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
