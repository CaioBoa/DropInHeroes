using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Interface para objetos que fornecem footprint (círculo de posicionamento)
    /// Permite acesso consistente à posição e estado do footprint
    /// </summary>
    public interface IFootprintProvider
    {
        /// <summary>
        /// Retorna a posição world atual do footprint
        /// Esta é a posição de REFERÊNCIA para validação e placement
        /// </summary>
        Vector2 GetFootprintPosition();

        /// <summary>
        /// Define o offset Y do footprint relativo à unidade
        /// </summary>
        /// <param name="yOffset">Offset em world units. Negativo = abaixo da unidade</param>
        void SetFootprintOffset(float yOffset);

        /// <summary>
        /// Retorna o offset Y atual do footprint
        /// </summary>
        float GetFootprintOffset();

        /// <summary>
        /// Mostra o footprint com o estado visual especificado
        /// </summary>
        void ShowState(UnitFootprint.FootprintState state);

        /// <summary>
        /// Esconde o footprint
        /// </summary>
        void Hide();

        /// <summary>
        /// Retorna o raio do footprint
        /// </summary>
        float GetRadius();
    }
}
