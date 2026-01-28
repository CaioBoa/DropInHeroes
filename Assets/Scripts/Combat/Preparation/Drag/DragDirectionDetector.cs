using UnityEngine;

/// <summary>
/// Utilitário para detectar direção de drag (horizontal vs vertical)
/// Extrai lógica de detecção para reutilização e testabilidade
/// </summary>
public class DragDirectionDetector
{
    public enum DragDirection
    {
        Undecided,   // Ainda não decidiu
        Horizontal,  // Scroll horizontal (carousel)
        Vertical     // Drag vertical (para battlefield)
    }

    private readonly float threshold;
    private Vector2 startPosition;
    private DragDirection currentDirection;

    // === CONSTRUCTOR ===

    public DragDirectionDetector(float detectionThreshold)
    {
        threshold = detectionThreshold;
        currentDirection = DragDirection.Undecided;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Inicia detecção de drag
    /// </summary>
    public void BeginDrag(Vector2 position)
    {
        startPosition = position;
        currentDirection = DragDirection.Undecided;
    }

    /// <summary>
    /// Atualiza e retorna a direção detectada
    /// </summary>
    public DragDirection UpdateDirection(Vector2 currentPosition)
    {
        // Se já decidiu, manter decisão
        if (currentDirection != DragDirection.Undecided)
        {
            return currentDirection;
        }

        Vector2 dragDelta = currentPosition - startPosition;
        float distance = dragDelta.magnitude;

        // Ainda não atingiu threshold
        if (distance < threshold)
        {
            return DragDirection.Undecided;
        }

        // Decidir baseado em componente dominante
        float horizontalMovement = Mathf.Abs(dragDelta.x);
        float verticalMovement = Mathf.Abs(dragDelta.y);

        if (horizontalMovement > verticalMovement)
        {
            currentDirection = DragDirection.Horizontal;
        }
        else
        {
            currentDirection = DragDirection.Vertical;
        }

        return currentDirection;
    }

    /// <summary>
    /// Reseta o detector
    /// </summary>
    public void Reset()
    {
        currentDirection = DragDirection.Undecided;
        startPosition = Vector2.zero;
    }

    // === PROPERTIES ===

    public DragDirection CurrentDirection => currentDirection;
    public bool IsDecided => currentDirection != DragDirection.Undecided;
    public Vector2 StartPosition => startPosition;
    public float Threshold => threshold;
}
