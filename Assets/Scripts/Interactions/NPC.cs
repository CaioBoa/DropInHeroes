using UnityEngine;

public class NPC : Interactable
{
    [Header("Visual (Character Database)")]
    [SerializeField] private string characterID;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CharacterData characterData;

    private void Awake()
    {
        // Buscar componentes visuais
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Carregar visual do personagem
        if (!string.IsNullOrEmpty(characterID))
        {
            LoadCharacterVisuals();
            setInteractPrompt($"Falar com {characterData.displayName}");
        }
    }

    private void LoadCharacterVisuals()
    {
        // Buscar dados do personagem no catalog
        characterData = DataManager.GetCharacter(characterID);

        if (characterData == null)
        {
            DebugManager.LogWarning($"CharacterData '{characterID}' não encontrado!", DebugCategory.Character);
            return;
        }

        // Aplicar animação Idle
        if (animator != null && characterData.idleAnimation != null)
        {
            // Criar um AnimatorOverrideController para aplicar apenas o Idle
            var aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
            var clips = aoc.animationClips;

            if (clips.Length > 0)
            {
                // Substituir o primeiro clip (Idle) pela animação do personagem
                aoc[clips[0]] = characterData.idleAnimation;
                animator.runtimeAnimatorController = aoc;
            }
        }

        // Aplicar sprite padrão
        if (spriteRenderer != null && characterData.defaultSprite != null)
        {
            spriteRenderer.sprite = characterData.defaultSprite;
        }

        DebugManager.Log($"Visual carregado: {characterData.displayName}", DebugCategory.Character);
    }
}
