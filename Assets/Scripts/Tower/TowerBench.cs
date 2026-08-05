using System;
using System.Collections.Generic;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Bench da run de Torre. Slots começam vazios. Comprar um personagem novo
    /// preenche o primeiro slot vazio (ordem de compra); comprar duplicado
    /// incrementa o rank do slot existente até o teto.
    /// </summary>
    public class TowerBench
    {
        private readonly List<BenchEntry> entries = new List<BenchEntry>();
        private readonly int maxRank;

        public IReadOnlyList<BenchEntry> Entries => entries;
        public int Capacity => entries.Count;

        public event Action OnChanged;
        public event Action<BenchEntry> OnEntryChanged;

        public TowerBench(int capacity, int maxRank)
        {
            this.maxRank = maxRank;
            for (int i = 0; i < capacity; i++)
            {
                entries.Add(new BenchEntry());
            }
        }

        public BenchEntry GetEntry(int index)
        {
            return (index >= 0 && index < entries.Count) ? entries[index] : null;
        }

        public BenchEntry FindEntry(CharacterData character)
        {
            if (character == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Character == character) return entries[i];
            }
            return null;
        }

        public BenchEntry FindEntryByController(UnitController controller)
        {
            if (controller == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].DeployedController == controller) return entries[i];
            }
            return null;
        }

        /// <summary>
        /// Compra ou evolui. Retorna true se houve mudança no bench.
        /// </summary>
        public bool BuyOrUpgrade(CharacterData character)
        {
            if (character == null) return false;

            BenchEntry existing = FindEntry(character);
            if (existing != null)
            {
                if (existing.Rank >= maxRank) return false;
                existing.Rank++;
                NotifyChanged(existing);
                return true;
            }

            // Preencher primeiro slot vazio (ordem de compra)
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsEmpty)
                {
                    entries[i].Character = character;
                    entries[i].Rank = 1;
                    NotifyChanged(entries[i]);
                    return true;
                }
            }

            return false;
        }

        private void NotifyChanged(BenchEntry entry)
        {
            OnEntryChanged?.Invoke(entry);
            OnChanged?.Invoke();
        }
    }
}
