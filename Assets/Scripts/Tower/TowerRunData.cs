using System;
using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Envelope estático para passar a seleção de picks (personagem + build escolhida) da tela de picks
    /// para a cena de run da Torre. Mesmo padrão de LoadingRequest: nenhum GameObject persistente.
    /// A build por herói é resolvida no deploy via <see cref="UnitController.PickedBuildResolver"/>,
    /// registrado aqui em <see cref="Set"/> — se um personagem não tiver build de pick, cai no defaultBuild.
    /// </summary>
    public static class TowerRunData
    {
        private static CharacterData[] selectedCharacters = Array.Empty<CharacterData>();
        private static CharacterBuild[] selectedBuilds = Array.Empty<CharacterBuild>();

        public static IReadOnlyList<CharacterData> SelectedCharacters => selectedCharacters;
        public static int Count => selectedCharacters.Length;
        public static bool HasSelection => selectedCharacters.Length > 0;

        /// <summary>Build escolhida nos picks para este personagem (null → o deploy usa o defaultBuild).</summary>
        public static CharacterBuild BuildFor(CharacterData character)
        {
            if (character == null) return null;
            for (int i = 0; i < selectedCharacters.Length; i++)
                if (selectedCharacters[i] == character)
                    return i < selectedBuilds.Length ? selectedBuilds[i] : null;
            return null;
        }

        public static void Set(IList<CharacterData> characters, IList<CharacterBuild> builds = null)
        {
            if (characters == null || characters.Count == 0)
            {
                selectedCharacters = Array.Empty<CharacterData>();
                selectedBuilds = Array.Empty<CharacterBuild>();
                UnitController.PickedBuildResolver = BuildFor;
                return;
            }

            selectedCharacters = new CharacterData[characters.Count];
            selectedBuilds = new CharacterBuild[characters.Count];
            for (int i = 0; i < characters.Count; i++)
            {
                selectedCharacters[i] = characters[i];
                selectedBuilds[i] = builds != null && i < builds.Count ? builds[i] : null;
            }
            // O deploy no run pergunta a build escolhida por aqui (senão defaultBuild).
            UnitController.PickedBuildResolver = BuildFor;
            Debug.Log($"[TowerRunData] Picks definidos: {selectedCharacters.Length} unidades.");
        }

        public static void Reset()
        {
            selectedCharacters = Array.Empty<CharacterData>();
            selectedBuilds = Array.Empty<CharacterBuild>();
        }
    }
}
