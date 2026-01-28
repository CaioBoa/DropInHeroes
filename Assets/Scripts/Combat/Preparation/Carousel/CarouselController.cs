using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// CarouselController com suporte a cards parciais (aparecer/desaparecer gradualmente nas bordas)
/// - Instancia N cards (3..7) a partir de um prefab
/// - Cálculo simétrico de spacing
/// - Posicionamento com round-based fractional offset (corrige assimetria esquerda/direita)
/// - Partial alpha baseado em interseção (leva em conta localScale)
/// - Cards parciais são interativos (não bloqueamos blocksRaycasts)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CarouselController : MonoBehaviour
{
    [Header("Prefab / Instanciação")]
    [Tooltip("Prefab da CharacterCard")]
    [SerializeField] private CharacterCard cardPrefab;
    [Tooltip("Número de cards visíveis (ímpar). Máximo 7, mínimo 3.")]
    [SerializeField, Range(3, 9)] private int numVisibleCards = 5;

    [Header("Configuração Visual")]
    [Tooltip("Escala das cards laterais em relação à central (0.7 = 70%)")]
    [SerializeField, Range(0.5f, 1f)] private float sideCardScale = 0.8f;

    [Tooltip("Padding lateral em % da largura do panel")]
    [SerializeField, Range(0f, 0.2f)] private float horizontalPadding = 0.05f;

    [Header("Física do Drag")]
    [SerializeField] private float dragSensitivity = 1f;
    [Tooltip("Multiplicador aplicado ao momentum no fim do drag")]
    [SerializeField] private float momentumMultiplier = 0.9f;
    [Tooltip("Valor entre 0 (parar rápido) e 1 (sem perda). Use ~0.90-0.98 para bons resultados.")]
    [SerializeField, Range(0.85f, 0.999f)] private float friction = 0.92f;
    [Tooltip("Velocidade mínima (pixels/s) considerada movimento. Abaixo disso será considerado parado.")]
    [SerializeField] private float minVelocityThreshold = 30f;
    [Tooltip("Velocidade máxima absoluta em pixels/s para evitar picos")]
    [SerializeField] private float maxVelocity = 2000f;

    [Header("Snap")]
    [Tooltip("Velocidade do snap (quanto maior, mais rápido)")]
    [SerializeField] private float snapSpeed = 12f;
    [Tooltip("Tolerância em pixels para considerar posição já no alvo")]
    [SerializeField] private float snapTolerance = 0.5f;

    [Header("Visibilidade Parcial")]
    [Tooltip("Multiplicador geral de alpha (útil para ajustar intensidade)")]
    [SerializeField, Range(0.1f, 1f)] private float globalAlphaMultiplier = 1f;

    // Dados
    private List<CharacterData> allCharacters = new List<CharacterData>();

    // Estado do scroll
    private float scrollOffset = 0f; // pixels
    private float scrollVelocity = 0f; // pixels/s

    // Drag
    private bool isDragging = false;
    private Vector2 lastDragPosition;
    private float lastDragTime;

    // Dimensões e layout
    private RectTransform panelRect;
    private float cardSpacing; // pixels
    private float centerCardHeight;
    private float centerCardWidth;

    private bool isInitialized = false;

    // Instanciadas dinamicamente
    private CharacterCard[] visibleCards;
    private int[] visibleAssignedIndices;

    // Snap
    private bool isSnapping = false;
    private float snapTargetOffset = 0f;

    // Eventos
    public event Action<CharacterData> OnCenterCharacterChanged;
    private int lastCenterIndex = int.MinValue;

    private void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        if (panelRect == null)
        {
            DebugManager.LogError("RectTransform não encontrado!", DebugCategory.UI);
            return;
        }

        numVisibleCards = Mathf.Clamp(numVisibleCards, 3, 9);
        if (numVisibleCards % 2 == 0) numVisibleCards = Mathf.Max(3, numVisibleCards - 1);

        EnsureInstantiateVisibleCards();
        CalculateCardDimensions();
    }

    private void Start()
    {
        scrollOffset = 0f;
        scrollVelocity = 0f;
    }

    private void EnsureInstantiateVisibleCards()
    {
        if (cardPrefab == null)
        {
            DebugManager.LogError("cardPrefab não atribuído!", DebugCategory.UI);
            return;
        }

        if (visibleCards != null && visibleCards.Length == numVisibleCards) return;

        if (visibleCards != null)
        {
            foreach (var c in visibleCards)
            {
                if (c != null) Destroy(c.gameObject);
            }
        }

        visibleCards = new CharacterCard[numVisibleCards];
        visibleAssignedIndices = new int[numVisibleCards];
        for (int i = 0; i < visibleAssignedIndices.Length; i++) visibleAssignedIndices[i] = int.MinValue;

        for (int i = 0; i < numVisibleCards; i++)
        {
            CharacterCard inst = Instantiate(cardPrefab, transform);
            inst.name = $"CharacterCard_{i}";
            RectTransform rt = inst.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            visibleCards[i] = inst;

            if (inst.GetComponent<CanvasGroup>() == null) inst.gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Inicializa o carousel com uma lista de personagens.
    /// </summary>
    public void Initialize(List<CharacterData> characters)
    {
        if (characters == null || characters.Count == 0)
        {
            DebugManager.LogWarning("Nenhum personagem fornecido!", DebugCategory.UI);
            return;
        }

        allCharacters = characters;
        scrollOffset = 0f;
        scrollVelocity = 0f;
        isSnapping = false;
        lastCenterIndex = int.MinValue;

        CalculateCardDimensions();
        UpdateCardPositions();
        isInitialized = true;
    }

    private void CalculateCardDimensions()
    {
        if (panelRect == null) return;
        if (visibleCards == null || visibleCards.Length == 0) return;

        Vector2 panelSize = panelRect.rect.size;
        centerCardHeight = panelSize.y;

        RectTransform firstRect = visibleCards[0].GetComponent<RectTransform>();
        Vector2 prefabSize = firstRect != null ? firstRect.rect.size : new Vector2(100f, 100f);
        float aspect = prefabSize.y != 0f ? prefabSize.x / prefabSize.y : 1f;
        centerCardWidth = centerCardHeight * aspect;

        float availableWidth = panelSize.x * (1f - 2f * horizontalPadding);

        int denom = Mathf.Max(1, numVisibleCards - 1);
        float spacingByLayout = (availableWidth - centerCardWidth) / denom;

        float minSpacing = centerCardWidth * 0.6f;
        cardSpacing = Mathf.Max(minSpacing, spacingByLayout);
        cardSpacing = Mathf.Min(cardSpacing, availableWidth / denom);

        ConfigureAllCards();
    }

    private void ConfigureAllCards()
    {
        for (int i = 0; i < visibleCards.Length; i++)
        {
            if (visibleCards[i] == null) continue;
            RectTransform rt = visibleCards[i].GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(centerCardWidth, centerCardHeight);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            CanvasGroup cg = visibleCards[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = visibleCards[i].gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (visibleCards == null || visibleCards.Length == 0) return;
        if (cardSpacing <= 0f) return;

        if (!isDragging)
        {
            if (Mathf.Abs(scrollVelocity) > 0f)
            {
                float decay = Mathf.Clamp01(friction);
                scrollVelocity *= Mathf.Pow(decay, Time.deltaTime * 60f);

                scrollOffset += scrollVelocity * Time.deltaTime;

                if (Mathf.Abs(scrollVelocity) < minVelocityThreshold)
                {
                    scrollVelocity = 0f;
                    StartSnapToNearest();
                }
            }
            else
            {
                if (isSnapping)
                {
                    scrollOffset = Mathf.Lerp(scrollOffset, snapTargetOffset, 1f - Mathf.Exp(-snapSpeed * Time.deltaTime));
                    if (Mathf.Abs(scrollOffset - snapTargetOffset) <= snapTolerance)
                    {
                        scrollOffset = snapTargetOffset;
                        isSnapping = false;
                    }
                }
            }
        }

        UpdateCardPositions();
    }

    private void UpdateCardPositions()
    {
        if (allCharacters == null || allCharacters.Count == 0) return;

        // Usar centerFloat e offsetFraction baseado em Round para simetria.
        float centerFloat = scrollOffset / cardSpacing; // ex: 2.3
        float offsetFraction = centerFloat - Mathf.Round(centerFloat); // ex: 0.3 (positivo ou negativo)

        int centerRelative = (numVisibleCards - 1) / 2;

        float panelHalfWidth = panelRect.rect.width * 0.5f;
        float panelLeft = -panelHalfWidth;
        float panelRight = panelHalfWidth;

        for (int i = 0; i < visibleCards.Length; i++)
        {
            if (visibleCards[i] == null) continue;

            int relativeOffset = i - centerRelative;
            int intendedIndex = Mathf.RoundToInt(centerFloat) + relativeOffset;
            int characterIndex = GetCircularIndex(intendedIndex);

            if (visibleAssignedIndices[i] != characterIndex)
            {
                visibleAssignedIndices[i] = characterIndex;
                visibleCards[i].SetData(allCharacters[characterIndex]);
            }

            RectTransform rt = visibleCards[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            // posição simétrica usando offsetFraction
            float posX = (relativeOffset - offsetFraction) * cardSpacing;
            rt.anchoredPosition = new Vector2(posX, 0f);

            // escala baseada na distância do centro (normalizada)
            float distanceFromCenter = Mathf.Abs(relativeOffset - offsetFraction);
            float maxDistance = ((numVisibleCards - 1) / 2f);
            float t = Mathf.Clamp01(distanceFromCenter / maxDistance);
            float scale = Mathf.Lerp(1f, sideCardScale, t);
            rt.localScale = Vector3.one * scale;

            // calcular interseção levando em conta escala atual (largura real)
            float actualCardWidth = centerCardWidth * rt.localScale.x;
            float halfCardW = actualCardWidth * 0.5f;
            float cardLeft = posX - halfCardW;
            float cardRight = posX + halfCardW;

            float overlapLeft = Mathf.Max(cardLeft, panelLeft);
            float overlapRight = Mathf.Min(cardRight, panelRight);
            float overlapWidth = Mathf.Max(0f, overlapRight - overlapLeft);
            float visibleFraction = Mathf.Clamp01(overlapWidth / actualCardWidth);

            // Alpha: combinação base (pela distância) * visibleFraction * global multiplier
            float baseAlpha = Mathf.Lerp(1f, 0.4f, t);
            float finalAlpha = baseAlpha * visibleFraction * globalAlphaMultiplier;

            CanvasGroup cg = visibleCards[i].GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = finalAlpha;
                // NÃO mudamos blocksRaycasts aqui — cards parciais continuam interativas.
                cg.blocksRaycasts = true;
            }
        }

        // Notificar mudança do personagem central
        int currentCenterIndex = GetCircularIndex(Mathf.RoundToInt(centerFloat));
        if (allCharacters.Count > 0 && currentCenterIndex != lastCenterIndex)
        {
            lastCenterIndex = currentCenterIndex;
            OnCenterCharacterChanged?.Invoke(allCharacters[currentCenterIndex]);
        }
    }

    private int GetCircularIndex(int index)
    {
        int count = Mathf.Max(1, allCharacters.Count);
        return ((index % count) + count) % count;
    }

    // === API usada pelas CharacterCard ===

    public void BeginHorizontalScroll(Vector2 startPosition)
    {
        isDragging = true;
        lastDragPosition = startPosition;
        lastDragTime = Time.time;
        scrollVelocity = 0f;
        isSnapping = false;
    }

    public void UpdateHorizontalScroll(Vector2 currentPosition)
    {
        if (!isDragging) return;
        float deltaX = currentPosition.x - lastDragPosition.x;
        // decrementa offset quando arrasta para a direita (comportamento natural)
        scrollOffset -= deltaX * dragSensitivity;
        lastDragPosition = currentPosition;
        lastDragTime = Time.time;
    }

    public void EndHorizontalScroll(Vector2 endPosition)
    {
        if (!isDragging) return;
        isDragging = false;

        float deltaTime = Time.time - lastDragTime;
        if (deltaTime > 0f && deltaTime < 0.5f)
        {
            float deltaX = endPosition.x - lastDragPosition.x;
            float velocity = -(deltaX / Mathf.Max(0.0001f, deltaTime)) * dragSensitivity * momentumMultiplier;
            velocity = Mathf.Clamp(velocity, -maxVelocity, maxVelocity);
            scrollVelocity = velocity;

            if (Mathf.Abs(scrollVelocity) < minVelocityThreshold)
            {
                scrollVelocity = 0f;
                StartSnapToNearest();
            }
        }
        else
        {
            StartSnapToNearest();
        }
    }

    public void CancelScroll()
    {
        isDragging = false;
        scrollVelocity = 0f;
        isSnapping = false;
    }

    public bool IsScrolling()
    {
        return isDragging || Mathf.Abs(scrollVelocity) > 0f || isSnapping;
    }

    // Utilitários públicos
    public CharacterData GetCenterCharacter()
    {
        if (allCharacters == null || allCharacters.Count == 0) return null;
        float centerFloat = scrollOffset / cardSpacing;
        int centerIndex = GetCircularIndex(Mathf.RoundToInt(centerFloat));
        return allCharacters[centerIndex];
    }

    public int GetTotalCharacterCount() => allCharacters?.Count ?? 0;

    public void StopMomentum()
    {
        scrollVelocity = 0f;
        isSnapping = false;
    }

    public void SetScrollPosition(int characterIndex)
    {
        if (allCharacters == null || allCharacters.Count == 0) return;
        scrollOffset = characterIndex * cardSpacing;
        scrollVelocity = 0f;
        isSnapping = false;
    }

    public void RefreshLayout()
    {
        CalculateCardDimensions();
    }

    private void StartSnapToNearest()
    {
        if (cardSpacing <= 0f) return;
        float normalized = scrollOffset / cardSpacing;
        float nearest = Mathf.Round(normalized);
        snapTargetOffset = nearest * cardSpacing;
        isSnapping = true;
    }

    private void OnRectTransformDimensionsChange()
    {
        CalculateCardDimensions();
    }
}
