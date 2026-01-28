using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Colors")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f);

    private float targetFillAmount = 1f;
    private Canvas canvas;

    public void Initialize(bool isPlayer)
    {
        canvas = GetComponent<Canvas>();

        if (fillImage != null)
        {
            fillImage.color = isPlayer ? playerColor : enemyColor;
            fillImage.fillAmount = 1f;
        }

        targetFillAmount = 1f;
    }

    public void SetHealthPercent(float percent)
    {
        targetFillAmount = Mathf.Clamp01(percent);
    }

    public void ResetToFull()
    {
        targetFillAmount = 1f;
        if (fillImage != null)
            fillImage.fillAmount = 1f;
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
