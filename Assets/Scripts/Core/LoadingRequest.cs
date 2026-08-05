using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Core
{

    /// <summary>
    /// Parâmetro consumido pelo LoadingManager para decidir qual cena carregar
    /// após inicializar o DataManager. Configurar ANTES de carregar a cena Loading.
    /// Default = TowerRun (compatibilidade quando dev abre Loading direto no Editor).
    /// </summary>
    public static class LoadingRequest
    {
        public static string TargetScene { get; private set; } = SceneNames.TowerRun;

        public static void Configure(string targetScene)
        {
            TargetScene = targetScene;
        }

        // Estado estático persiste entre sessões de Play com domain reload desabilitado;
        // reseta para o default para o dev abrir a cena Loading direto sem herdar o último alvo.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TargetScene = SceneNames.TowerRun;
        }
    }
}
