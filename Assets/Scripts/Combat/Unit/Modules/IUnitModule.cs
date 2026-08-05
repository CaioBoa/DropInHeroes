using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Interface base para todos os módulos de unidade
    /// Permite arquitetura modular com UnitController como hub central
    /// </summary>
    public interface IUnitModule
    {
        /// <summary>
        /// Inicializa o módulo com referência ao controller
        /// </summary>
        void Initialize(UnitController controller);

        /// <summary>
        /// Chamado quando o módulo é ativado
        /// </summary>
        void OnEnabled();

        /// <summary>
        /// Chamado quando o módulo é desativado
        /// </summary>
        void OnDisabled();

        /// <summary>
        /// Limpa recursos do módulo
        /// </summary>
        void Cleanup();
    }
}
