using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Módulo responsável por visuais da unidade (sprite, animações)
/// Gerencia Animator e SpriteRenderer de forma isolada
/// </summary>
public class VisualModule : IUnitModule
{
    private UnitController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AnimatorOverrideController animatorOverride;
    private AnimationClip attackClip;

    // Animator parameters (cached hash para performance)
    private static readonly int IsDraggingHash = Animator.StringToHash("isDragging");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");
    private static readonly int IsWinnerHash = Animator.StringToHash("isWinner");

    // Evento disparado pelo Animation Event no frame de impacto
    public event Action OnAttackHit;

    // Damage flash settings
    private static readonly Color damageFlashColor = Color.red;
    private const float damageFlashDuration = 0.1f;
    private Coroutine damageFlashCoroutine;

    // === INTERFACE IMPLEMENTATION ===

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        animator = controller.GetComponent<Animator>();
        spriteRenderer = controller.GetComponent<SpriteRenderer>();

        if (animator == null)
        {
            DebugManager.LogError("Animator não encontrado!", DebugCategory.Combat);
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            DebugManager.LogError("Animator precisa de RuntimeAnimatorController!", DebugCategory.Combat);
            return;
        }

        // Criar override controller a partir do base
        animatorOverride = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = animatorOverride;
    }

    public void OnEnabled()
    {
        // Futuro: ativar renderização, etc
    }

    public void OnDisabled()
    {
        // Futuro: desativar renderização, etc
    }

    public void Cleanup()
    {
        animatorOverride = null;
        animator = null;
        spriteRenderer = null;
    }

    // === PUBLIC API ===

    public void ApplyCharacterVisuals(CharacterData characterData)
    {
        if (characterData == null || spriteRenderer == null) return;

        if (characterData.defaultSprite != null)
        {
            spriteRenderer.sprite = characterData.defaultSprite;
        }
    }

    public void ApplyCharacterAnimations(CharacterData characterData)
    {
        if (characterData == null || animatorOverride == null) return;

        // Obter overrides do animator
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        animatorOverride.GetOverrides(overrides);

        // Sobrescrever clips baseado no CharacterData
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip originalClip = overrides[i].Key;
            AnimationClip newClip = null;

            // Match por nome do clip do UnitAnimator.controller
            switch (originalClip.name)
            {
                case "Placeholder_Idle":
                    newClip = characterData.idleAnimation;
                    break;
                case "Placeholder_Run":
                    newClip = characterData.runAnimation;
                    break;
                case "Placeholder_Drag":
                    newClip = characterData.dragAnimation;
                    break;
                case "Placeholder_Attack":
                    newClip = characterData.attackAnimation;
                    attackClip = newClip;
                    break;
                case "Placeholder_Death":
                    newClip = characterData.deathAnimation;
                    break;
                case "Placeholder_DeathIdle":
                    newClip = characterData.deathIdleAnimation;
                    break;
                case "Placeholder_Victory":
                    newClip = characterData.victoryAnimation;
                    break;
            }

            if (newClip != null)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
            }
        }

        animatorOverride.ApplyOverrides(overrides);

        DebugManager.Log($"Applied animations for {characterData.displayName}", DebugCategory.Character);
    }

    public void SetDraggingAnimation(bool isDragging)
    {
        if (animator == null)
        {
            DebugManager.LogWarning("Animator is null!", DebugCategory.Combat);
            return;
        }

        animator.SetBool(IsDraggingHash, isDragging);
    }

    public void SetMovingAnimation(bool isMoving)
    {
        if (animator == null)
        {
            DebugManager.LogWarning("Animator is null!", DebugCategory.Combat);
            return;
        }

        animator.SetBool(IsMovingHash, isMoving);
    }

    // Método simples para flip baseado em direção X
    public void SetFacingDirection(float directionX)
    {
        if (spriteRenderer == null) return;
        
        if (directionX > 0.01f)
            spriteRenderer.flipX = true;
        else if (directionX < -0.01f)
            spriteRenderer.flipX = false;
    }

    // Ou alternativa: flip baseado em posição de um alvo
    public void FaceTowards(Vector3 targetPosition)
    {
        if (spriteRenderer == null || controller == null) return;

        float dirX = targetPosition.x - controller.transform.position.x;
        SetFacingDirection(dirX);
    }

    // === ATTACK ANIMATION ===

    /// <summary>
    /// Inicia animação de ataque com velocidade ajustada.
    /// Retorna duração real da animação (considerando speedMultiplier).
    /// </summary>
    public float PlayAttackAnimation(float speedMultiplier)
    {
        if (animator == null) return 0.5f;

        animator.speed = speedMultiplier;
        animator.SetTrigger(AttackTriggerHash);

        float duration = attackClip != null ? attackClip.length / speedMultiplier : 0.5f;
        return duration;
    }

    /// <summary>
    /// Chamado via Animation Event no frame de impacto do ataque.
    /// </summary>
    public void OnAttackHitFrame()
    {
        OnAttackHit?.Invoke();
    }

    /// <summary>
    /// Reseta velocidade do animator após ataque.
    /// </summary>
    public void ResetAnimatorSpeed()
    {
        if (animator != null)
            animator.speed = 1f;
    }

    // === DEATH ANIMATION ===

    public void PlayDeathAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger(DeathTriggerHash);
        animator.SetBool(IsDeadHash, true);
    }

    // === VICTORY ANIMATION ===

    public void PlayVictoryAnimation()
    {
        if (animator == null) return;
        animator.SetBool(IsWinnerHash, true);
    }

    // === DAMAGE FEEDBACK ===

    /// <summary>
    /// Executa flash visual de dano no sprite.
    /// </summary>
    public void PlayDamageFlash()
    {
        if (controller == null || spriteRenderer == null) return;

        if (damageFlashCoroutine != null)
            controller.StopCoroutine(damageFlashCoroutine);

        damageFlashCoroutine = controller.StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = Color.white;
        damageFlashCoroutine = null;
    }

    // === PROPERTIES ===

    public Animator Animator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
}
