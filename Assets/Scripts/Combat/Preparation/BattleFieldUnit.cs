using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Wrapper para unidades posicionadas no campo de batalha
/// Permite arrastar unidades já posicionadas
/// </summary>
public class BattleFieldUnit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CharacterData CharacterData { get; private set; }
    public GameObject UnitInstance { get; private set; }
    public Vector2 CurrentPosition { get; private set; }

    private Animator animator;
    private bool isDragging = false;
    private Vector2 dragStartPosition;

    // === INITIALIZATION ===

    public void Initialize(CharacterData characterData, GameObject unitInstance, Vector2 position)
    {
        CharacterData = characterData;
        UnitInstance = unitInstance;
        CurrentPosition = position;

        // Obter Animator
        if (unitInstance != null)
        {
            animator = unitInstance.GetComponent<Animator>();
        }

        // Configurar estado inicial
        SetDraggingState(false);

        Debug.Log($"[BattleFieldUnit] Inicializado: {characterData.displayName}");
    }

    // === DRAG HANDLERS ===

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null) return;

        isDragging = true;
        dragStartPosition = CurrentPosition;

        // Notificar PreparationManager
        PreparationManager.Instance.OnDragStart(CharacterData);

        // Atualizar estado de animação
        SetDraggingState(true);

        Debug.Log($"[BattleFieldUnit] Drag iniciado: {CharacterData.displayName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null) return;

        // Atualizar posição visual da unidade enquanto arrasta
        Vector2 worldPos = PreparationManager.Instance.ScreenToWorldPosition(eventData.position);
        SetPosition(worldPos);

        // Notificar PreparationManager para atualizar indicador
        PreparationManager.Instance.OnDragUpdate(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (PreparationManager.Instance == null) return;

        isDragging = false;

        Vector2 worldPos = PreparationManager.Instance.ScreenToWorldPosition(eventData.position);

        // Verificar se posição é válida
        if (!PreparationManager.Instance.IsPositionValid(worldPos))
        {
            // Posição inválida - voltar para posição original
            SetPosition(dragStartPosition);
            SetDraggingState(false);
            Debug.Log($"[BattleFieldUnit] Drag cancelado (posição inválida)");
            return;
        }

        // Detectar se está próximo de outra unidade para swap
        BattleFieldUnit nearUnit;
        if (PreparationManager.Instance.GetUnitNearPosition(worldPos, out nearUnit))
        {
            if (nearUnit != this)
            {
                // Swap posições
                Vector2 nearUnitPosition = nearUnit.CurrentPosition;
                nearUnit.SetPosition(dragStartPosition);
                SetPosition(nearUnitPosition);

                Debug.Log($"[BattleFieldUnit] Swap entre {CharacterData.displayName} e {nearUnit.CharacterData.displayName}");
            }
            else
            {
                // Mesma unidade, apenas mover
                SetPosition(worldPos);
            }
        }
        else
        {
            // Sem swap, apenas atualizar posição
            SetPosition(worldPos);
        }

        // Atualizar estado de animação
        SetDraggingState(false);

        Debug.Log($"[BattleFieldUnit] Drag concluído: {CharacterData.displayName}");
    }

    // === POSITION & STATE ===

    public void SetPosition(Vector2 newPosition)
    {
        CurrentPosition = newPosition;

        if (UnitInstance != null)
        {
            UnitInstance.transform.position = new Vector3(newPosition.x, newPosition.y, 0f);
        }
    }

    public void SetDraggingState(bool dragging)
    {
        isDragging = dragging;

        if (animator != null)
        {
            animator.SetBool("IsDragging", dragging);
        }
    }

    // === DEBUG ===

    private void OnDrawGizmosSelected()
    {
        // Desenhar círculo de detecção de swap
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(CurrentPosition.x, CurrentPosition.y, 0f), 1.5f);
    }
}
