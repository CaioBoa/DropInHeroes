using UnityEngine;

/// <summary>
/// Estados do indicador de posicionamento
/// </summary>
public enum PositionIndicatorState
{
    Valid,      // Verde - posição livre
    Swap,       // Amarelo - swap com unidade próxima
    Invalid     // Vermelho - fora do campo ou inválido
}

/// <summary>
/// Indicador visual que mostra onde a unidade será posicionada durante drag
/// </summary>
public class PositionIndicator : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer indicatorSprite;

    [Header("Colors")]
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.7f);      // Verde semi-transparente
    [SerializeField] private Color swapColor = new Color(1f, 1f, 0f, 0.7f);       // Amarelo semi-transparente
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.7f);    // Vermelho semi-transparente

    private PositionIndicatorState currentState = PositionIndicatorState.Valid;
    private bool isVisible = false;

    // === INITIALIZATION ===

    private void Awake()
    {
        if (indicatorSprite == null)
        {
            indicatorSprite = GetComponent<SpriteRenderer>();
        }

        // Garantir que está desativado no início
        Hide();
    }

    // === VISIBILITY ===

    public void Show()
    {
        isVisible = true;

        if (indicatorSprite != null)
        {
            indicatorSprite.enabled = true;
        }

        // Aplicar cor do estado atual
        ApplyStateColor();
    }

    public void Hide()
    {
        isVisible = false;

        if (indicatorSprite != null)
        {
            indicatorSprite.enabled = false;
        }
    }

    // === POSITION & STATE ===

    public void UpdatePosition(Vector2 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
    }

    public void SetState(PositionIndicatorState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            ApplyStateColor();
        }
    }

    public void SetColor(Color color)
    {
        if (indicatorSprite != null)
        {
            indicatorSprite.color = color;
        }
    }

    private void ApplyStateColor()
    {
        if (!isVisible) return;

        Color targetColor = validColor;

        switch (currentState)
        {
            case PositionIndicatorState.Valid:
                targetColor = validColor;
                break;
            case PositionIndicatorState.Swap:
                targetColor = swapColor;
                break;
            case PositionIndicatorState.Invalid:
                targetColor = invalidColor;
                break;
        }

        SetColor(targetColor);
    }

    // === DEBUG ===

    private void OnValidate()
    {
        // Aplicar cor no editor para visualização
        if (indicatorSprite != null && isVisible)
        {
            ApplyStateColor();
        }
    }
}
