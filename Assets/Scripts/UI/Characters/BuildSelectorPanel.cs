using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DropInHeroes.Data;

namespace DropInHeroes.UI
{

    /// <summary>
    /// Seletor de build dinâmico: mostra a build Default + as builds custom JÁ CRIADAS (0..3) + um botão
    /// "+" enquanto houver slot livre (não aparece slot vazio). Builds incompletas (nem todos os pontos
    /// gastos) recebem um aviso. Dispara <see cref="OnBuildSelected"/>(build) e <see cref="OnCreateBuild"/>.
    /// </summary>
    public class BuildSelectorPanel : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private BuildSlotButton slotPrefab;
        [SerializeField] private Button addButton;

        public event Action<CharacterBuild> OnBuildSelected;
        public event Action OnCreateBuild;

        private readonly List<BuildSlotButton> spawned = new List<BuildSlotButton>();
        private StatTreeData tree;

        private void Awake()
        {
            if (addButton != null) addButton.onClick.AddListener(() => OnCreateBuild?.Invoke());
        }

        private void OnDestroy()
        {
            if (addButton != null) addButton.onClick.RemoveAllListeners();
        }

        public void SetTree(StatTreeData t) => tree = t;

        public void Refresh(CharacterData character, CharacterBuild selected)
        {
            for (int i = 0; i < spawned.Count; i++) spawned[i].gameObject.SetActive(false);

            if (character == null)
            {
                if (addButton != null) addButton.gameObject.SetActive(false);
                return;
            }

            int idx = 0;
            CharacterBuild def = character.defaultBuild;
            AddSlot(idx++, def, def != null && !string.IsNullOrEmpty(def.label) ? def.label : "Padrão", false, ReferenceEquals(selected, def));

            var customs = BuildStore.GetCustomBuilds(character.ID);
            for (int i = 0; i < customs.Count; i++)
            {
                CharacterBuild b = customs[i];
                string text = !string.IsNullOrEmpty(b.label) ? b.label : "Build " + (i + 1);
                AddSlot(idx++, b, text, !IsComplete(b), ReferenceEquals(selected, b));
            }

            if (addButton != null)
            {
                addButton.gameObject.SetActive(BuildStore.CanCreate(character.ID));
                addButton.transform.SetAsLastSibling();
            }
        }

        private void AddSlot(int idx, CharacterBuild build, string text, bool incomplete, bool isSelected)
        {
            BuildSlotButton b;
            if (idx < spawned.Count) { b = spawned[idx]; b.gameObject.SetActive(true); }
            else { b = Instantiate(slotPrefab, container); spawned.Add(b); }
            b.transform.SetSiblingIndex(idx);
            CharacterBuild captured = build;
            b.Setup(text, incomplete, isSelected, () => OnBuildSelected?.Invoke(captured));
        }

        // Completa = todos os pontos de cada rank foram gastos (default nunca marca aviso).
        private bool IsComplete(CharacterBuild build)
        {
            if (tree == null || tree.pointsPerTier == null || build == null) return true;
            for (int t = 0; t < tree.pointsPerTier.Length; t++)
                if (TierSpent(build, t) < tree.pointsPerTier[t]) return false;
            return true;
        }

        private int TierSpent(CharacterBuild build, int tier)
        {
            if (build.allocations == null) return 0;
            int sum = 0;
            for (int i = 0; i < build.allocations.Count; i++)
            {
                TreeAllocation a = build.allocations[i];
                if (a.points <= 0) continue;
                if (tree.TryGetNode(a.nodeId, out TreeNode nd) && nd.tier == tier) sum += a.points;
            }
            return sum;
        }
    }
}
