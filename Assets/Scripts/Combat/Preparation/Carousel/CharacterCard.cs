using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Card de personagem no carousel.
/// Decide direção do drag (horizontal = scroll do carousel; vertical = arrastar card para o campo).
/// Compatível com instanciação via script pelo CarouselController.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CharacterCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Drag Settings")]
    [SerializeField] private float dragAlpha = 0.6f;

    [Header("Carousel Integration")]
    [Tooltip("Threshold (pixels) para começar a decidir direção do drag")]
    [SerializeField] private float verticalDragThreshold = 20f;

    private CharacterData characterData;
    private bool isDragging = false;

    // Referência ao carousel (assume que o CarouselController está em um pai)
    private CarouselController carousel;

    // Detecção de direção
    private Vector2 dragStartPosition;
    private bool dragDirectionDecided = false;

    private void Awake()
    {
        carousel = GetComponentInParent<CarouselController>();
    }

    public void SetData(CharacterData data)
    {
        characterData = data;
        if (characterData != null && cardImage != null)
        {
            cardImage.sprite = characterData.cardPortrait;
        }
    }

    public CharacterData GetData() => characterData;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (characterData == null) return;
        dragStartPosition = eventData.position;
        dragDirectionDecided = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (characterData == null) return;

        if (!dragDirectionDecided)
        {
            Vector2 dragDelta = eventData.position - dragStartPosition;
            float distance = dragDelta.magnitude;

            if (distance > verticalDragThreshold)
            {
                float horizontalMovement = Mathf.Abs(dragDelta.x);
                float verticalMovement = Mathf.Abs(dragDelta.y);

                dragDirectionDecided = true;

                if (horizontalMovement > verticalMovement)
                {
                    // HORIZONTAL -> passar controle ao carousel
                    if (carousel != null)
                    {
                        carousel.BeginHorizontalScroll(dragStartPosition);
                    }
                }
                else
                {
                    // VERTICAL -> este card assume controle (drag para battlefield)
                    isDragging = true;
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = dragAlpha;
                        canvasGroup.blocksRaycasts = false;
                    }

                    // NOVO SISTEMA: Spawnar unidade imediatamente
                    if (PreparationManager.Instance != null)
                    {
                        PreparationManager.Instance.StartDraggingFromRoulette(characterData, eventData.position);
                    }
                }
            }
            return;
        }

        if (isDragging)
        {
            // NOVO SISTEMA: Atualizar posição da unidade real
            if (PreparationManager.Instance != null)
            {
                PreparationManager.Instance.UpdateDragging(eventData.position);
            }
        }
        else
        {
            if (carousel != null) carousel.UpdateHorizontalScroll(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            dragDirectionDecided = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // NOVO SISTEMA: Finalizar drag da unidade real
            if (PreparationManager.Instance != null && characterData != null)
            {
                PreparationManager.Instance.FinishDragging(eventData.position);
            }
        }
        else if (dragDirectionDecided)
        {
            if (carousel != null) carousel.EndHorizontalScroll(eventData.position);
            dragDirectionDecided = false;
        }
        else
        {
            dragDirectionDecided = false;
        }
    }
}
