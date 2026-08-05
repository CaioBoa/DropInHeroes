using UnityEngine;

namespace DropInHeroes.Combat
{

    /// <summary>Configuração do projétil de um ataque à distância (sem prefab — visual é um AnimationClip).</summary>
    [System.Serializable]
    public struct ProjectileSpec
    {
        [Tooltip("AnimationClip do projétil. Tocado num host genérico — sem prefab.")]
        public AnimationClip clip;
        [Tooltip("Velocidade do projétil = base + coeficiente × Velocidade da unidade.")]
        public float speedBase;
        public float speedPerSpeed;
        [Tooltip("Escala (tamanho) do projétil na cena.")]
        public float scale;
        public int sortingOrder;
        [Tooltip("Gira o projétil na direção do voo (compensa a inversão ao ir para a esquerda).")]
        public bool faceTravelDirection;

        public static ProjectileSpec Default => new ProjectileSpec
        {
            speedBase = 0f,
            speedPerSpeed = 4f,
            scale = 1f,
            sortingOrder = 50,
            faceTravelDirection = true
        };
    }
}
