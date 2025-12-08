using UnityEngine;
using System.Collections.Generic;

public class CharacterManager : MonoBehaviour
{
    [Header("Available Characters")]
    [SerializeField] private List<string> availableCharacterIDs = new List<string> {"gulin", "marda", "rikurby"};
    [SerializeField] private int currentCharacterIndex = 0;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CharacterData currentCharacter;

    private void Awake()
    {
        // Buscar componentes se não foram atribuídos
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Carregar personagem inicial
        LoadCharacter(currentCharacterIndex);
    }

    public void LoadCharacter(int index)
    {
        if (index < 0)
        {
            Debug.LogWarning($"Índice {index} inválido!");
            return;
        }

        // Loop automático se ultrapassar o limite
        if (index >= availableCharacterIDs.Count)
        {
            index = 0;
        }

        currentCharacterIndex = index;

        string characterID = availableCharacterIDs[index];

        currentCharacter = DataManager.GetCharacter(characterID);

        if (currentCharacter != null)
        {
            ApplyCharacterVisuals();
        }
        else
        {
            Debug.LogError($"✗ Personagem '{characterID}' não encontrado no Catalog!");
        }
    }

    private void ApplyCharacterVisuals()
    {
        AnimatorOverrideController aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
        // Sobrescrever animações
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        var clips = aoc.animationClips;

        // Override Idle
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(
            clips[0], currentCharacter.idleAnimation));

       // Override Run
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(
            clips[1], currentCharacter.runAnimation));

        aoc.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = aoc;
        
        // Aplicar sprite padrão se definido
        if (spriteRenderer != null && currentCharacter.defaultSprite != null)
        {
            spriteRenderer.sprite = currentCharacter.defaultSprite;
        }
    }

    public void SwitchToNextCharacter()
    {
        currentCharacterIndex += 1;
        LoadCharacter(currentCharacterIndex);
    }
}