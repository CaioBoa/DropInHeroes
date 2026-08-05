using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Tabela única dos TIPOS de status do jogo. Substitui os N assets de <c>StatusDefinition</c>:
    /// cada tipo é uma linha (<see cref="StatusTypeDef"/>), editável numa lista só. Lookup por id.
    /// Consultada por <see cref="StatusModule.ApplyStatus"/> e pelos widgets de status.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusCatalog", menuName = "Game/System/Status Catalog")]
    public class StatusCatalog : ScriptableObject
    {
        [SerializeField] private List<StatusTypeDef> types = new List<StatusTypeDef>();

        private Dictionary<string, StatusTypeDef> lookup;

        public void Initialize()
        {
            // Guard pelo lookup (não por flag): após reimport o dicionário pode voltar a null — reconstruir.
            if (lookup != null) return;
            lookup = new Dictionary<string, StatusTypeDef>(types.Count);
            for (int i = 0; i < types.Count; i++)
            {
                StatusTypeDef t = types[i];
                if (t != null && !string.IsNullOrEmpty(t.id)) lookup[t.id] = t;
            }
        }

        public StatusTypeDef Get(string id)
        {
            if (lookup == null) Initialize();
            return id != null && lookup.TryGetValue(id, out StatusTypeDef t) ? t : null;
        }

        public IReadOnlyList<StatusTypeDef> All => types;
    }
}
