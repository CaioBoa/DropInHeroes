using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Card de personagem no carousel
/// Pode ser arrastado para o campo de batalha
/// </summary>
public class CharacterCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Drag Settings")]
    [SerializeField] private float dragAlpha = 0.6f;

    private CharacterData characterData;
    private DragPreview dragPreview;
    private bool isDragging = false;

    // === INITIALIZATION ===

    private void Awake()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    // === DATA ===

    public void SetData(CharacterData data)
    {
        characterData = data;
        Debug.Log($"[CharacterCard] Dados definidos para: {characterData.displayName}");
        if (characterData != null && cardImage != null)
        {
            // Aplicar sprite do personagem no card
            cardImage.sprite = characterData.defaultSprite;
        }
    }

    public CharacterData GetData()
    {
        return characterData;
    }

    // === DRAG HANDLERS ===

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (characterData == null || PreparationManager.Instance == null) return;

        isDragging = true;

        // Reduzir opacidade do card original
        if (canvasGroup != null)
        {
            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false; // Permitir raycast passar pelo card
        }

        // Criar preview visual
        CreateDragPreview(eventData);

        // Notificar PreparationManager
        PreparationManager.Instance.OnDragStart(characterData);

        Debug.Log($"[CharacterCard] Drag iniciado: {characterData.displayName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || PreparationManager.Instance == null) return;

        // Atualizar posição do preview
        if (dragPreview != null)
        {
            dragPreview.FollowCursor(eventData.position);
        }

        // Notificar PreparationManager para atualizar indicador
        PreparationManager.Instance.OnDragUpdate(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // Restaurar opacidade do card original
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Destruir preview
        DestroyDragPreview();

        // Notificar PreparationManager
        if (PreparationManager.Instance != null && characterData != null)
        {
            PreparationManager.Instance.OnDragEnd(characterData, eventData.position);
        }

        Debug.Log($"[CharacterCard] Drag finalizado: {characterData?.displayName ?? "null"}");
    }

    // === DRAG PREVIEW ===

    private void CreateDragPreview(PointerEventData eventData)
    {
        if (characterData == null) return;

        // Criar GameObject para preview
        GameObject previewObj = new GameObject("DragPreview");
        dragPreview = previewObj.AddComponent<DragPreview>();

        // Inicializar preview com sprite do personagem
        dragPreview.Initialize(characterData.defaultSprite);

        // Posicionar no cursor
        dragPreview.FollowCursor(eventData.position);
    }

    private void DestroyDragPreview()
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview.gameObject);
            dragPreview = null;
        }
    }
}
