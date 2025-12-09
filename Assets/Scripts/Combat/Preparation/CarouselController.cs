using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Controlador do carousel infinito de personagens
/// Usa padrão circular buffer para reciclar cards
/// </summary>
public class CarouselController : MonoBehaviour
{
    [Header("Card Settings")]
    [SerializeField] private CharacterCard[] visibleCards = new CharacterCard[5];
    [SerializeField] private float cardSpacing = 120f;

    [Header("Scroll Settings")]
    [SerializeField] private Button scrollLeftButton;
    [SerializeField] private Button scrollRightButton;
    [SerializeField] private float scrollAnimationSpeed = 5f;

    private List<CharacterData> allCharacters = new List<CharacterData>();
    private int currentCenterIndex = 0;
    private bool isScrolling = false;

    // === INITIALIZATION ===

    private void Start()
    {
        // Conectar botões
        if (scrollLeftButton != null)
        {
            scrollLeftButton.onClick.AddListener(ScrollLeft);
        }

        if (scrollRightButton != null)
        {
            scrollRightButton.onClick.AddListener(ScrollRight);
        }
    }

    public void Initialize(List<CharacterData> characters)
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("[CarouselController] Nenhum personagem fornecido!");
            return;
        }

        allCharacters = characters;
        currentCenterIndex = 0;

        // Popular cards visíveis iniciais
        UpdateVisibleCards();

        Debug.Log($"[CarouselController] Inicializado com {allCharacters.Count} personagens");
    }

    // === SCROLL CONTROL ===

    public void ScrollLeft()
    {
        if (isScrolling || allCharacters.Count == 0) return;

        // Mover para o personagem anterior (circular)
        currentCenterIndex--;
        if (currentCenterIndex < 0)
        {
            currentCenterIndex = allCharacters.Count - 1;
        }

        UpdateVisibleCards();

        Debug.Log($"[CarouselController] Scroll Left → Index {currentCenterIndex}");
    }

    public void ScrollRight()
    {
        if (isScrolling || allCharacters.Count == 0) return;

        // Mover para o próximo personagem (circular)
        currentCenterIndex++;
        if (currentCenterIndex >= allCharacters.Count)
        {
            currentCenterIndex = 0;
        }

        UpdateVisibleCards();

        Debug.Log($"[CarouselController] Scroll Right → Index {currentCenterIndex}");
    }

    // === CARD MANAGEMENT ===

    private void UpdateVisibleCards()
    {
        if (allCharacters.Count == 0) return;

        // Atualizar os 5 cards visíveis
        // Card 0: -2 do centro
        // Card 1: -1 do centro
        // Card 2: centro (currentCenterIndex)
        // Card 3: +1 do centro
        // Card 4: +2 do centro

        for (int i = 0; i < visibleCards.Length; i++)
        {
            if (visibleCards[i] == null) continue;

            // Calcular offset do centro (-2, -1, 0, +1, +2)
            int offset = i - 2;

            // Calcular índice circular
            int characterIndex = GetCircularIndex(currentCenterIndex + offset);

            // Atualizar card com personagem
            CharacterData character = allCharacters[characterIndex];
            visibleCards[i].SetData(character);
        }
    }

    private int GetCircularIndex(int index)
    {
        int count = allCharacters.Count;

        // Garantir que o índice está no range [0, count-1]
        while (index < 0)
        {
            index += count;
        }

        while (index >= count)
        {
            index -= count;
        }

        return index;
    }

    // === PUBLIC GETTERS ===

    public CharacterData GetCenterCharacter()
    {
        if (allCharacters.Count == 0) return null;
        return allCharacters[currentCenterIndex];
    }

    public int GetTotalCharacterCount()
    {
        return allCharacters.Count;
    }

    // === DEBUG ===

    private void OnValidate()
    {
        // Garantir que temos exatamente 5 cards
        if (visibleCards.Length != 5)
        {
            System.Array.Resize(ref visibleCards, 5);
        }
    }
}
