using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Interface para objetos que podem ser arrastados
    /// Fornece API consistente para operações de drag
    /// </summary>
    public interface IDraggable
    {
        /// <summary>
        /// Inicia uma operação de drag na posição especificada
        /// </summary>
        void StartDrag(Vector2 position);

        /// <summary>
        /// Atualiza a posição durante o drag
        /// </summary>
        void UpdateDrag(Vector2 position);

        /// <summary>
        /// Finaliza o drag (personagem cai até posição final)
        /// </summary>
        void EndDrag();

        /// <summary>
        /// Cancela o drag e retorna à posição inicial
        /// </summary>
        void CancelDrag();

        /// <summary>
        /// Verifica se o objeto está sendo arrastado atualmente
        /// </summary>
        bool IsDragging { get; }

        /// <summary>
        /// Posição atual do objeto
        /// </summary>
        Vector2 CurrentPosition { get; }
    }
}
