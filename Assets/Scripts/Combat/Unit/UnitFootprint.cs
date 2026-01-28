using UnityEngine;

/// <summary>
/// Círculo de footprint UNIFICADO que:
/// 1. Mostra área de ocupação de unidades no board
/// 2. Indica validação de posição (verde/amarelo/vermelho) durante drag
/// SUBSTITUI completamente o PositionIndicator
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class UnitFootprint : MonoBehaviour
{
    [Header("Visual Settings")]
    // NOTA: Valores configurados dinamicamente via FootprintModule.ApplyConfig()
    // usando parâmetros globais de PreparationConfig
    private float circleScale;
    private float yOffset;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.3f);    // Branco transparente
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.7f);     // Verde (posição válida)
    [SerializeField] private Color swapColor = new Color(1f, 1f, 0f, 0.6f);      // Amarelo (swap)
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.7f);   // Vermelho (inválido)
    [SerializeField] private Color fullBoardColor = new Color(1f, 0.5f, 0f, 0.7f); // Laranja (board cheio)

    private SpriteRenderer spriteRenderer;
    private FootprintState currentState = FootprintState.Hidden;

    public enum FootprintState
    {
        Hidden,          // Escondido
        Normal,          // Branco (unidades no board sem drag ativo)
        Valid,           // Verde (posição válida durante drag)
        Swap,            // Amarelo (swap detection)
        Invalid,         // Vermelho (posição inválida)
        FullBoardNoSwap  // Laranja (board cheio, sem swap possível)
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            DebugManager.LogError("SpriteRenderer não encontrado!", DebugCategory.Drag);
            return;
        }

        // Configurar sorting order para ficar abaixo da unidade
        spriteRenderer.sortingOrder = -1;

        // Iniciar escondido
        Hide();

        // NOTA: Scale e YOffset serão aplicados via FootprintModule.ApplyConfig()
    }

    public void Show(FootprintState state = FootprintState.Normal)
    {
        currentState = state;
        spriteRenderer.enabled = true;
        ApplyStateColor();
    }

    public void Hide()
    {
        currentState = FootprintState.Hidden;
        spriteRenderer.enabled = false;
    }

    public void SetState(FootprintState state)
    {
        if (currentState == state) return;

        currentState = state;

        if (currentState != FootprintState.Hidden)
        {
            ApplyStateColor();
        }
    }

    private void ApplyStateColor()
    {
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case FootprintState.Normal:
                spriteRenderer.color = normalColor;
                break;
            case FootprintState.Valid:
                spriteRenderer.color = validColor;
                break;
            case FootprintState.Swap:
                spriteRenderer.color = swapColor;
                break;
            case FootprintState.Invalid:
                spriteRenderer.color = invalidColor;
                break;
            case FootprintState.FullBoardNoSwap:
                spriteRenderer.color = fullBoardColor;
                break;
        }
    }

    public void SetScale(float scale)
    {
        circleScale = scale;
        transform.localScale = Vector3.one * circleScale;
    }

    /// <summary>
    /// Define dinamicamente a distância Y onde a unidade será posicionada
    /// Útil para efeitos de "arremesso" ou personagens de tamanhos diferentes
    /// </summary>
    public void SetYOffset(float offset)
    {
        yOffset = offset;
        transform.localPosition = new Vector3(0f, yOffset, 0f);
    }

    public float GetYOffset()
    {
        return yOffset;
    }

    public float GetRadius()
    {
        // Assumindo sprite de 1 unidade de diâmetro
        return circleScale / 2f;
    }
}
