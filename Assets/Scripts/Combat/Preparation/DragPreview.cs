using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clone visual que segue o cursor durante drag
/// </summary>
public class DragPreview : MonoBehaviour
{
    private Image previewImage;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private RectTransform rectTransform;

    [Header("Settings")]
    [SerializeField] private float previewAlpha = 0.7f;
    [SerializeField] private float previewScale = 1.2f;

    // === INITIALIZATION ===

    public void Initialize(Sprite sprite)
    {
        // Criar Canvas para preview (overlay mode)
        GameObject canvasObj = new GameObject("PreviewCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Sempre no topo

        canvasObj.AddComponent<GraphicRaycaster>();

        // Criar Image para preview
        GameObject imageObj = new GameObject("PreviewImage");
        imageObj.transform.SetParent(canvasObj.transform);

        rectTransform = imageObj.AddComponent<RectTransform>();
        previewImage = imageObj.AddComponent<Image>();

        // Configurar preview
        if (sprite != null)
        {
            previewImage.sprite = sprite;
            previewImage.preserveAspect = true;

            // Configurar tamanho
            rectTransform.sizeDelta = new Vector2(100f, 100f);
        }

        // Adicionar CanvasGroup para transparência
        canvasGroup = imageObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = previewAlpha;
        canvasGroup.blocksRaycasts = false; // Não bloquear raycasts

        // Aplicar escala
        rectTransform.localScale = Vector3.one * previewScale;

        Debug.Log("[DragPreview] Inicializado");
    }

    // === POSITION ===

    public void FollowCursor(Vector2 screenPosition)
    {
        if (rectTransform != null)
        {
            rectTransform.position = screenPosition;
        }
    }

    // === CLEANUP ===

    private void OnDestroy()
    {
        // Canvas será destruído junto com este GameObject
        Debug.Log("[DragPreview] Destruído");
    }
}
