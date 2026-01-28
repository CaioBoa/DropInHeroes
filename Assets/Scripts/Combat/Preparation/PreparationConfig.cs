using UnityEngine;

/// <summary>
/// Configurações centralizadas para o sistema de Preparation
/// Substitui magic numbers espalhados pelo código
/// </summary>
[CreateAssetMenu(menuName = "Combat/Preparation Config", fileName = "PreparationConfig")]
public class PreparationConfig : ScriptableObject
{
    [Header("Board Settings")]
    [Tooltip("Número máximo de unidades que podem ser posicionadas no board")]
    public int maxUnitsOnField = 4;

    [Header("Drag Settings")]
    [Tooltip("Raio de detecção para swap entre unidades (em world units)")]
    public float swapDetectionRadius = 1.5f;

    [Tooltip("Alpha da unidade durante drag (0-1)")]
    [Range(0f, 1f)]
    public float dragAlpha = 0.6f;

    [Tooltip("Offset Y do footprint durante drag (negativo = abaixo da unidade)")]
    public float footprintDragOffset = -1.0f;

    [Tooltip("Offset Y do footprint quando unidade está posicionada")]
    public float footprintPlacedOffset = 0f;

    [Tooltip("Escala visual do círculo de footprint (1.0 = tamanho normal)")]
    public float footprintScale = 1.5f;

    [Header("Animation Settings")]
    [Tooltip("Duração do lerp de queda da unidade (em segundos)")]
    public float dropLerpDuration = 0.5f;

    [Tooltip("Duração do lerp rápido para unidade sendo swapada (em segundos)")]
    public float swapLerpDuration = 0.15f;

    [Header("Carousel Settings")]
    [Tooltip("Padding horizontal entre cards no carousel")]
    public float carouselCardPadding = 20f;

    [Tooltip("Multiplicador de largura para cards nas laterais")]
    [Range(0.1f, 1f)]
    public float sideCardWidthMultiplier = 0.6f;

    [Tooltip("Multiplicador de altura para cards nas laterais")]
    [Range(0.1f, 1f)]
    public float sideCardHeightMultiplier = 0.8f;

    [Tooltip("Fricção do carousel (maior = para mais rápido)")]
    [Range(1f, 20f)]
    public float carouselFriction = 5f;

    [Tooltip("Threshold para detectar direção de drag (em pixels)")]
    public float dragDirectionThreshold = 10f;
}
