using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("Runtime Data")]
    private CharacterData characterData;
    private int currentHP;
    private bool isInitialized = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Initialize(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogError("[UnitController] CharacterData é null!");
            return;
        }

        CombatData combatData = data.combatData;

        // Aplicar visual
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.defaultSprite;
        }

        if (animator != null && combatData.battleAnimator != null)
        {
            animator.runtimeAnimatorController = combatData.battleAnimator;
        }

        // Resetar stats
        currentHP = combatData.maxHP;
        isInitialized = true;

        Debug.Log($"[UnitController] Initialized: {data.ID} (HP: {currentHP}/{combatData.maxHP})");
    }

    public void ResetUnit()
    {
        currentHP = characterData != null ? characterData.combatData.maxHP : 0;
        isInitialized = false;
    }

    // Getters
    public CombatData GetCombatData() => characterData.combatData;
    public int GetCurrentHP() => currentHP;
    public bool IsInitialized() => isInitialized;

    // Combat methods (placeholder)
    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        Debug.Log($"[UnitController] {characterData.ID} took {damage} damage. HP: {currentHP}/{characterData.combatData.maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[UnitController] {characterData.ID} died!");
        // Tocar animação de morte, etc.
    }
}