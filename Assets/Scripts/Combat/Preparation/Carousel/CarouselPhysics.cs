using UnityEngine;

/// <summary>
/// Gerencia física do carousel (momentum, friction, snapping)
/// Responsabilidade: simulação física do scroll
/// </summary>
public class CarouselPhysics
{
    private float scrollOffset;
    private float scrollVelocity;

    private readonly float friction;
    private readonly float minVelocityThreshold;
    private readonly float maxVelocity;
    private readonly float snapSpeed;
    private readonly float snapTolerance;

    private bool isSnapping;
    private float snapTargetOffset;

    // === CONSTRUCTOR ===

    public CarouselPhysics(
        float frictionValue,
        float minVelocity,
        float maxVel,
        float snapSpd,
        float snapTol)
    {
        friction = frictionValue;
        minVelocityThreshold = minVelocity;
        maxVelocity = maxVel;
        snapSpeed = snapSpd;
        snapTolerance = snapTol;

        scrollOffset = 0f;
        scrollVelocity = 0f;
        isSnapping = false;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Aplica drag (usuário arrastando)
    /// </summary>
    public void ApplyDrag(float deltaX)
    {
        scrollOffset += deltaX;
        isSnapping = false;
    }

    /// <summary>
    /// Inicia momentum ao soltar drag
    /// </summary>
    public void ApplyMomentum(float velocity, float multiplier)
    {
        scrollVelocity = Mathf.Clamp(velocity * multiplier, -maxVelocity, maxVelocity);
    }

    /// <summary>
    /// Atualiza física (fricção e snapping)
    /// </summary>
    public void Update(float deltaTime)
    {
        // Aplicar fricção
        if (Mathf.Abs(scrollVelocity) > 0f)
        {
            float decay = Mathf.Clamp01(friction);
            scrollVelocity *= Mathf.Pow(decay, deltaTime * 60f);

            scrollOffset += scrollVelocity * deltaTime;

            // Parar se velocidade muito baixa
            if (Mathf.Abs(scrollVelocity) < minVelocityThreshold)
            {
                scrollVelocity = 0f;
            }
        }

        // Snapping
        if (isSnapping)
        {
            scrollOffset = Mathf.Lerp(
                scrollOffset,
                snapTargetOffset,
                1f - Mathf.Exp(-snapSpeed * deltaTime)
            );

            if (Mathf.Abs(scrollOffset - snapTargetOffset) <= snapTolerance)
            {
                scrollOffset = snapTargetOffset;
                isSnapping = false;
            }
        }
    }

    /// <summary>
    /// Inicia snap para a posição mais próxima
    /// </summary>
    public void StartSnapToNearest(float cardSpacing)
    {
        if (cardSpacing <= 0f) return;

        float centerFloat = scrollOffset / cardSpacing;
        float snappedIndex = Mathf.Round(centerFloat);
        snapTargetOffset = snappedIndex * cardSpacing;
        isSnapping = true;
        scrollVelocity = 0f;
    }

    /// <summary>
    /// Reseta o estado físico
    /// </summary>
    public void Reset()
    {
        scrollOffset = 0f;
        scrollVelocity = 0f;
        isSnapping = false;
    }

    // === PROPERTIES ===

    public float ScrollOffset => scrollOffset;
    public float ScrollVelocity => scrollVelocity;
    public bool IsSnapping => isSnapping;
    public bool IsMoving => Mathf.Abs(scrollVelocity) > minVelocityThreshold || isSnapping;

    /// <summary>
    /// Verifica se deve iniciar snap (parado mas não snappado)
    /// </summary>
    public bool ShouldStartSnap => Mathf.Abs(scrollVelocity) < minVelocityThreshold && !isSnapping;

    public void SetOffset(float offset)
    {
        scrollOffset = offset;
    }
}
