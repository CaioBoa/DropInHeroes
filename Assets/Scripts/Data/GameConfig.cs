using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.UI;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Agregador único dos configs globais lidos por código estático/singleton: definições de stat,
    /// piso geral de stats, keywords e tema de UI. Substitui os Resources.Load espalhados — cada
    /// consumidor lê GameConfig.Active.X. É o ÚNICO asset carregado por Resources
    /// (Assets/Data/Resources/GameConfig.asset); os assets referenciados vivem em Assets/Data/Config
    /// e são resolvidos por referência serializada (GUID). Funciona em qualquer cena e em edit-time —
    /// o preview de tema (ThemedGraphic/ThemedButton em ExecuteAlways) depende dessa resolução.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/System/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private StatDefinitionCatalog statDefinitions;
        [SerializeField] private GeneralBaseStats baseStats;
        [SerializeField] private KeywordCatalog keywords;
        [SerializeField] private UITheme theme;

        public StatDefinitionCatalog StatDefinitions => statDefinitions;
        public GeneralBaseStats BaseStats => baseStats;
        public KeywordCatalog Keywords => keywords;
        public UITheme Theme => theme;

        private static GameConfig active;

        /// <summary>Config ativo, carregado sob demanda de Resources/GameConfig.asset (edit-time e runtime).</summary>
        public static GameConfig Active
        {
            get
            {
                if (active == null) active = Resources.Load<GameConfig>("GameConfig");
                return active;
            }
        }

        // Estado estático persiste entre sessões de Play com domain reload desabilitado; reseta para
        // recarregar o asset na próxima sessão em vez de manter cache obsoleto.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active = null;
    }
}
