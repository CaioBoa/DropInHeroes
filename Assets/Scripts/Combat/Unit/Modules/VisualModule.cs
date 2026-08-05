using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Módulo responsável por visuais da unidade (sprite, animações)
    /// Gerencia Animator e SpriteRenderer de forma isolada
    /// </summary>
    public class VisualModule : IUnitModule
    {
        private UnitController controller;
        private Transform visualTransform;
        private Transform barsCanvas;      // "Canvas" (barras de vida/energia), acima do personagem
        private Transform statusCanvas;    // "StatusCanvas" (pips de status), acima das barras
        private Transform selectionRing;   // "SelectionRing" (anel no chão, atrás do personagem)
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private AnimatorOverrideController animatorOverride;
        private AnimationClip attackClip;
        private AnimationClip supremeClip;

        // Escala/ancoragem por animação: reaplicada só quando o clipe muda (constante) ou a cada
        // frame (curva no tempo). Buffer reutilizado para leitura non-alloc do clip-info por frame.
        private readonly List<AnimatorClipInfo> clipInfoBuffer = new List<AnimatorClipInfo>();
        private AnimationClip lastTunedClip;
        private AnimClipTuning currentTuning;
        private bool currentHasTuning;
        private bool currentTimeVarying;

        // Fonte dos clipes (base + perfis de transformação) e perfil de transformação ativo (null = base).
        private CharacterData characterData;
        private string activeProfileKey;

        // Nome do placeholder do estado genérico "Extra" (one-shot), sobrescrito por PlayExtraAnimation.
        private const string ExtraPlaceholder = "Placeholder_Extra";

        // Animator parameters (cached hash para performance)
        private static readonly int IsDraggingHash = Animator.StringToHash("isDragging");
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int SupremeTriggerHash = Animator.StringToHash("Supreme");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        private static readonly int ExtraTriggerHash = Animator.StringToHash("Extra");
        private static readonly int IsDeadHash = Animator.StringToHash("isDead");
        private static readonly int IsWinnerHash = Animator.StringToHash("isWinner");
        private static readonly int IsStunnedHash = Animator.StringToHash("isStunned");

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

            // UI em world-space da unidade — posicionada por personagem em ApplyWorldAnchors.
            barsCanvas = controller.transform.Find("Canvas");
            statusCanvas = controller.transform.Find("StatusCanvas");
            selectionRing = controller.transform.Find("SelectionRing");

            // SpriteRenderer e Animator vivem no filho "Visual", escalável de forma
            // independente do Footprint/colisão/barras (que ficam na raiz).
            visualTransform = controller.transform.Find("Visual");
            if (visualTransform == null)
            {
                DebugManager.LogError("Filho 'Visual' não encontrado no prefab da unidade!", DebugCategory.Combat);
                return;
            }
            animator = visualTransform.GetComponent<Animator>();
            spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();

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
            barsCanvas = null;
            statusCanvas = null;
            selectionRing = null;
        }

        // === PUBLIC API ===

        public void ApplyCharacterVisuals(CharacterData characterData)
        {
            if (characterData == null) return;

            if (spriteRenderer != null && characterData.defaultSprite != null)
                spriteRenderer.sprite = characterData.defaultSprite;

            ApplyVisualScale(characterData.visualScale);
            ApplyWorldAnchors(characterData);
        }

        // Distância raiz→pés em unidades de mundo (casa com a ancoragem do import: pés ~1.0 abaixo da raiz).
        private const float AnchorFeetDistance = 1f;
        // Status empilha logo acima das barras.
        private const float StatusStackMargin = 0.22f;

        // Escala apenas o nó visual, em torno dos pés, para manter os pés ancorados no Footprint.
        private void ApplyVisualScale(float scale)
        {
            if (visualTransform == null) return;

            float s = scale <= 0f ? 1f : scale;
            visualTransform.localScale = Vector3.one * s;
            visualTransform.localPosition = new Vector3(0f, (s - 1f) * AnchorFeetDistance, 0f);
        }

        /// <summary>
        /// Reaplica a escala/ancoragem POR ANIMAÇÃO do clipe em reprodução. Chamado por
        /// UnitController.Update só em combate. Non-alloc: reusa <see cref="clipInfoBuffer"/> e só
        /// reaplica quando o clipe muda — o custo por frame é uma leitura + comparação.
        /// </summary>
        public void Tick()
        {
            if (animator == null || characterData == null || visualTransform == null) return;
            animator.GetCurrentAnimatorClipInfo(0, clipInfoBuffer);
            if (clipInfoBuffer.Count == 0) return;
            AnimationClip current = clipInfoBuffer[0].clip;

            // Na troca de clipe: resolve o ajuste e — se constante — aplica uma vez.
            if (current != lastTunedClip)
            {
                lastTunedClip = current;
                currentHasTuning = characterData.TryGetClipTuning(current, out currentTuning);
                currentTimeVarying = currentHasTuning && currentTuning.IsTimeVarying;
                if (!currentTimeVarying) ApplyResolved(0f);
            }

            // Ajuste que varia no tempo: reavalia a cada frame pelo tempo normalizado do estado.
            if (currentTimeVarying) ApplyResolved(GetNormalizedTime());
        }

        // Resolve escala/offset por eixo (constante ou curva) do clipe atual e aplica.
        private void ApplyResolved(float nt)
        {
            float mx = currentHasTuning ? currentTuning.EvalScaleX(nt) : 1f;
            float my = currentHasTuning ? currentTuning.EvalScaleY(nt) : 1f;
            float ox = currentHasTuning ? currentTuning.EvalOffsetX(nt) : 0f;
            float oy = currentHasTuning ? currentTuning.EvalOffsetY(nt) : 0f;
            ApplyScaleAnchor(mx, my, ox, oy);
        }

        // Escala não-uniforme (X/Y) sobre o visualScale; ancora os pés pela escala Y e aplica o offset.
        private void ApplyScaleAnchor(float mulX, float mulY, float offsetX, float offsetY)
        {
            mulX = Mathf.Max(0.05f, mulX);
            mulY = Mathf.Max(0.05f, mulY);
            float baseScale = characterData.visualScale <= 0f ? 1f : characterData.visualScale;
            float sx = baseScale * mulX, sy = baseScale * mulY;
            visualTransform.localScale = new Vector3(sx, sy, 1f);
            visualTransform.localPosition = new Vector3(offsetX, (sy - 1f) * AnchorFeetDistance + offsetY, 0f);
        }

        // Tempo normalizado 0..1 dentro do estado atual (envolve em loops; one-shots transicionam antes de estourar).
        private float GetNormalizedTime()
        {
            float nt = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            return nt - Mathf.Floor(nt);
        }

        // Posiciona a UI world-space por parâmetros do personagem (independentes do visualScale):
        // barras/status ACIMA da cabeça (altura por personagem) e o anel de seleção nos pés (raio+altura).
        private void ApplyWorldAnchors(CharacterData data)
        {
            if (barsCanvas != null) SetLocalY(barsCanvas, data.overheadHeight);
            if (statusCanvas != null) SetLocalY(statusCanvas, data.overheadHeight + StatusStackMargin);
            if (selectionRing != null)
            {
                SetLocalY(selectionRing, data.selectionRingOffsetY);
                float r = data.selectionRingScale <= 0f ? 0.9f : data.selectionRingScale;
                selectionRing.localScale = new Vector3(r, r, 1f);
            }
        }

        private static void SetLocalY(Transform t, float y)
        {
            Vector3 p = t.localPosition;
            t.localPosition = new Vector3(p.x, y, p.z);
        }

        public void ApplyCharacterAnimations(CharacterData data)
        {
            characterData = data;
            activeProfileKey = null;
            lastTunedClip = null; // o Tick de combate reaplica a escala por-clipe do zero
            ApplyProfile(null);
            if (data != null)
                DebugManager.Log($"Applied animations for {data.displayName}", DebugCategory.Character);
        }

        /// <summary>
        /// Ativa uma TRANSFORMAÇÃO: substitui o CONJUNTO de clipes (idle/run/attack/…) pelos do perfil
        /// enquanto ativo; clipe vazio no perfil cai no base. Reaproveita o mesmo AnimatorOverrideController
        /// e a mesma FSM (não precisa de estados novos). No-op se a chave não existir.
        /// </summary>
        public void SetAnimationProfile(string key)
        {
            var profile = characterData?.GetAnimationProfile(key);
            if (profile == null)
            {
                DebugManager.LogWarning($"Perfil de animação '{key}' não existe em {characterData?.displayName}.", DebugCategory.Character);
                return;
            }
            activeProfileKey = key;
            ApplyProfile(profile);
        }

        /// <summary>Volta ao conjunto base de clipes (fim da transformação).</summary>
        public void ResetAnimationProfile()
        {
            activeProfileKey = null;
            ApplyProfile(null);
        }

        /// <summary>Chave do perfil de transformação ativo, ou null (base).</summary>
        public string ActiveAnimationProfile => activeProfileKey;

        // Reaplica os overrides dos placeholders geridos resolvendo cada um pelo perfil (ou base).
        private void ApplyProfile(AnimationProfile profile)
        {
            if (characterData == null || animatorOverride == null) return;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            animatorOverride.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                AnimationClip newClip = ResolveClip(original.name, profile);
                if (original.name == "Placeholder_Attack" && newClip != null) attackClip = newClip;
                else if (original.name == "Placeholder_Supreme" && newClip != null) supremeClip = newClip;
                if (newClip != null)
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, newClip);
            }

            animatorOverride.ApplyOverrides(overrides);
        }

        // Clip de um placeholder: do perfil (transformação) quando definido, senão o base do CharacterData.
        // Placeholder_Extra e quaisquer outros não geridos pelo perfil retornam null (mantêm o override atual).
        private AnimationClip ResolveClip(string placeholder, AnimationProfile p)
        {
            switch (placeholder)
            {
                case "Placeholder_Idle":      return p != null && p.idleAnimation != null ? p.idleAnimation : characterData.idleAnimation;
                case "Placeholder_Run":       return p != null && p.runAnimation != null ? p.runAnimation : characterData.runAnimation;
                case "Placeholder_Drag":      return p != null && p.dragAnimation != null ? p.dragAnimation : characterData.dragAnimation;
                case "Placeholder_Attack":    return p != null && p.attackAnimation != null ? p.attackAnimation : characterData.attackAnimation;
                case "Placeholder_Death":     return p != null && p.deathAnimation != null ? p.deathAnimation : characterData.deathAnimation;
                case "Placeholder_DeathIdle": return p != null && p.deathIdleAnimation != null ? p.deathIdleAnimation : characterData.deathIdleAnimation;
                case "Placeholder_Victory":   return p != null && p.victoryAnimation != null ? p.victoryAnimation : characterData.victoryAnimation;
                case "Placeholder_Supreme":   return p != null && p.supremeAnimation != null ? p.supremeAnimation : characterData.supremeAnimation;
                case "Placeholder_Stun":      return p != null && p.stunAnimation != null ? p.stunAnimation : characterData.stunAnimation;
                default: return null;
            }
        }

        /// <summary>
        /// Toca uma animação EXTRA avulsa (one-shot) pela chave: sobrescreve o placeholder do estado
        /// genérico "Extra" com o clipe e dispara o trigger; o estado volta ao idle/run ao terminar.
        /// Para skills/passivas com pose própria. Retorna a duração real (0 se a chave não existir).
        /// </summary>
        public float PlayExtraAnimation(string key, float speedMultiplier = 1f)
        {
            if (animator == null || animatorOverride == null || characterData == null) return 0f;
            AnimationClip clip = characterData.GetExtraAnimation(key);
            if (clip == null)
            {
                DebugManager.LogWarning($"Animação extra '{key}' não existe em {characterData.displayName}.", DebugCategory.Combat);
                return 0f;
            }

            float speed = speedMultiplier <= 0f ? 1f : speedMultiplier;
            animatorOverride[ExtraPlaceholder] = clip; // sobrescreve o slot Extra com o clipe pedido
            animator.speed = speed;
            animator.SetTrigger(ExtraTriggerHash);
            return clip.length / speed;
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

        /// <summary>Liga/desliga a animação de imobilização (stun/root) — estado 'Stunned' no Animator
        /// (parâmetro isStunned). O clipe por personagem vem de CharacterData.stunAnimation.</summary>
        public void SetStunnedAnimation(bool isStunned)
        {
            if (animator == null) return;
            animator.SetBool(IsStunnedHash, isStunned);
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

        // Orienta a unidade parada (idle/preparação) para o lado adversário do tabuleiro.
        // A arte-base olha para a esquerda; os inimigos nascem em +X (enemySpawnOrigin),
        // então o time do jogador encara a direita e o time inimigo encara a esquerda.
        public void ApplyIdleFacing()
        {
            if (controller == null) return;
            SetFacingDirection(controller.IsPlayerUnit() ? 1f : -1f);
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
        /// Toca a animação de ataque ESTICADA para durar exatamente 'targetDuration' segundos. A
        /// cadência do ataque passa a depender só desse alvo (derivado da Speed), não da duração/frames
        /// do clipe. O AnimationEvent de impacto dispara proporcionalmente dentro do intervalo.
        /// Retorna 'targetDuration'.
        /// </summary>
        public float PlayAttackAnimationForDuration(float targetDuration)
        {
            if (animator == null || targetDuration <= 0f) return targetDuration;

            float clipLength = attackClip != null ? attackClip.length : targetDuration;
            animator.speed = clipLength / targetDuration; // estica/comprime p/ preencher o intervalo
            animator.SetTrigger(AttackTriggerHash);
            return targetDuration;
        }

        /// <summary>
        /// Inicia animação supreme com velocidade ajustada.
        /// Retorna duração real da animação (considerando speedMultiplier).
        /// </summary>
        public float PlaySupremeAnimation(float speedMultiplier)
        {
            if (animator == null) return 0.5f;

            animator.speed = speedMultiplier;
            animator.SetTrigger(SupremeTriggerHash);

            float duration = supremeClip != null ? supremeClip.length / speedMultiplier : 0.5f;
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

        /// <summary>
        /// Limpa todos os parâmetros booleanos e triggers do animator.
        /// Usado ao devolver a unidade ao pool para garantir que isWinner/isDead
        /// não persistam entre combates.
        /// </summary>
        public void ResetAnimatorState()
        {
            if (animator == null) return;
            animator.SetBool(IsDraggingHash, false);
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsDeadHash, false);
            animator.SetBool(IsWinnerHash, false);
            animator.SetBool(IsStunnedHash, false);
            animator.ResetTrigger(AttackTriggerHash);
            animator.ResetTrigger(SupremeTriggerHash);
            animator.ResetTrigger(DeathTriggerHash);
            animator.ResetTrigger(ExtraTriggerHash);
            animator.speed = 1f;
            activeProfileKey = null; // o próximo spawn reaplica o conjunto base via ApplyCharacterAnimations
            lastTunedClip = null;    // força o Tick a reaplicar a escala por-clipe no próximo combate
        }

        /// <summary>
        /// Reset visual ao devolver a unidade ao pool: interrompe o flash de dano em andamento e
        /// restaura a cor base. Sem isto, uma unidade que morre durante o flash volta tingida de
        /// vermelho no próximo spawn — o SetActive(false) do pool mata a coroutine antes do reset
        /// para branco (e ResetAnimatorState/ApplyCharacterVisuals não tocam a cor).
        /// </summary>
        public void ResetForPool()
        {
            if (damageFlashCoroutine != null && controller != null)
                controller.StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = null;

            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            ResetAnimatorState();
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
}
