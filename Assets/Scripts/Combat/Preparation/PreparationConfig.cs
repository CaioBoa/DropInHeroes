using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Configuração da fase de preparação (board, drag, footprint, lerps).
    /// Tunables que afetam o comportamento direto do PreparationManager e DragCoordinator.
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Preparation Config", fileName = "PreparationConfig")]
    public class PreparationConfig : ScriptableObject
    {
        [Header("Board Settings")]
        [Tooltip("Número máximo de unidades que podem ser posicionadas no board")]
        public int maxUnitsOnField = 3;

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
    }
}
