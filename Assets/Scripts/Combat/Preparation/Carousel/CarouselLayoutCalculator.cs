using UnityEngine;

/// <summary>
/// Calcula dimensões e layout de cards no carousel
/// Responsabilidade: cálculos de layout (spacing, tamanhos, posições)
/// </summary>
public class CarouselLayoutCalculator
{
    // === LAYOUT RESULT ===

    public struct LayoutResult
    {
        public float CardSpacing;
        public float CenterCardWidth;
        public float CenterCardHeight;
        public float PanelWidth;
        public float PanelHeight;

        public LayoutResult(float spacing, float width, float height, float panelW, float panelH)
        {
            CardSpacing = spacing;
            CenterCardWidth = width;
            CenterCardHeight = height;
            PanelWidth = panelW;
            PanelHeight = panelH;
        }
    }

    // === CARD POSITION RESULT ===

    public struct CardPositionResult
    {
        public Vector2 AnchoredPosition;
        public float Scale;
        public float Alpha;
        public float VisibleFraction;

        public CardPositionResult(Vector2 pos, float scale, float alpha, float visible)
        {
            AnchoredPosition = pos;
            Scale = scale;
            Alpha = alpha;
            VisibleFraction = visible;
        }
    }

    // === PUBLIC API ===

    /// <summary>
    /// Calcula dimensões das cards baseado no tamanho do panel
    /// </summary>
    public LayoutResult CalculateCardDimensions(
        RectTransform panelRect,
        RectTransform cardPrefabRect,
        int numVisibleCards,
        float horizontalPadding,
        float minSpacingMultiplier = 0.6f)
    {
        if (panelRect == null || cardPrefabRect == null)
        {
            DebugManager.LogWarning("RectTransforms inválidos!", DebugCategory.UI);
            return default;
        }

        Vector2 panelSize = panelRect.rect.size;
        float centerCardHeight = panelSize.y;

        // Calcular largura baseado em aspect ratio do prefab
        Vector2 prefabSize = cardPrefabRect.rect.size;
        float aspect = prefabSize.y != 0f ? prefabSize.x / prefabSize.y : 1f;
        float centerCardWidth = centerCardHeight * aspect;

        // Calcular spacing
        float availableWidth = panelSize.x * (1f - 2f * horizontalPadding);
        int denom = Mathf.Max(1, numVisibleCards - 1);
        float spacingByLayout = (availableWidth - centerCardWidth) / denom;

        float minSpacing = centerCardWidth * minSpacingMultiplier;
        float cardSpacing = Mathf.Max(minSpacing, spacingByLayout);
        cardSpacing = Mathf.Min(cardSpacing, availableWidth / denom);

        return new LayoutResult(cardSpacing, centerCardWidth, centerCardHeight, panelSize.x, panelSize.y);
    }

    /// <summary>
    /// Calcula posição, escala e alpha de uma card específica
    /// </summary>
    public CardPositionResult CalculateCardPosition(
        int cardIndex,
        int numVisibleCards,
        float scrollOffset,
        float cardSpacing,
        float centerCardWidth,
        float sideCardScale,
        float globalAlphaMultiplier,
        RectTransform panelRect)
    {
        if (panelRect == null)
        {
            DebugManager.LogWarning("PanelRect é null!", DebugCategory.UI);
            return default;
        }

        // Calcular offset fracional para simetria
        float centerFloat = scrollOffset / cardSpacing;
        float offsetFraction = centerFloat - Mathf.Round(centerFloat);

        int centerRelative = (numVisibleCards - 1) / 2;
        int relativeOffset = cardIndex - centerRelative;

        // Posição X simétrica
        float posX = (relativeOffset - offsetFraction) * cardSpacing;

        // Escala baseada na distância do centro
        float distanceFromCenter = Mathf.Abs(relativeOffset - offsetFraction);
        float maxDistance = ((numVisibleCards - 1) / 2f);
        float t = Mathf.Clamp01(distanceFromCenter / maxDistance);
        float scale = Mathf.Lerp(1f, sideCardScale, t);

        // Calcular interseção com panel (visibilidade parcial)
        float panelHalfWidth = panelRect.rect.width * 0.5f;
        float panelLeft = -panelHalfWidth;
        float panelRight = panelHalfWidth;

        float actualCardWidth = centerCardWidth * scale;
        float halfCardW = actualCardWidth * 0.5f;
        float cardLeft = posX - halfCardW;
        float cardRight = posX + halfCardW;

        float overlapLeft = Mathf.Max(cardLeft, panelLeft);
        float overlapRight = Mathf.Min(cardRight, panelRight);
        float overlapWidth = Mathf.Max(0f, overlapRight - overlapLeft);
        float visibleFraction = Mathf.Clamp01(overlapWidth / actualCardWidth);

        // Alpha final
        float baseAlpha = Mathf.Lerp(1f, 0.4f, t);
        float finalAlpha = baseAlpha * visibleFraction * globalAlphaMultiplier;

        return new CardPositionResult(
            new Vector2(posX, 0f),
            scale,
            finalAlpha,
            visibleFraction
        );
    }

    /// <summary>
    /// Calcula o índice circular (wrap around)
    /// </summary>
    public int GetCircularIndex(int index, int totalCount)
    {
        if (totalCount <= 0) return 0;

        int mod = index % totalCount;
        if (mod < 0) mod += totalCount;
        return mod;
    }

    /// <summary>
    /// Calcula o target de snap mais próximo
    /// </summary>
    public float CalculateSnapTarget(float currentOffset, float cardSpacing)
    {
        if (cardSpacing <= 0f) return currentOffset;

        float centerFloat = currentOffset / cardSpacing;
        float snappedIndex = Mathf.Round(centerFloat);
        return snappedIndex * cardSpacing;
    }
}
