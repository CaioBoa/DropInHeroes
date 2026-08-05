using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DropInHeroes.Combat;
using DropInHeroes.Data;

namespace DropInHeroes.Sandbox
{

    /// <summary>
    /// Item do painel esquerdo do Sandbox (roleta): arrasta um personagem direto para o campo (reusando o
    /// drag do PreparationManager) e configura build/rank ali mesmo por botões-ciclo. O item NÃO trava
    /// quando a unidade está no board — pode-se arrastar a mesma unidade várias vezes / para os dois times
    /// (teste espelho). Para o dummy, o grupo de build/rank fica oculto (dummy não tem build).
    /// </summary>
    public class SandboxRosterItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;
        [Tooltip("Container dos botões de build/rank — escondido para o dummy.")]
        [SerializeField] private GameObject buildRankGroup;
        [SerializeField] private Button buildButton;
        [SerializeField] private TMP_Text buildLabel;
        [SerializeField] private Button rankButton;
        [SerializeField] private TMP_Text rankLabel;

        private const int MaxRank = 3;

        private CharacterData character;
        private SandboxController.SandboxPick pick; // null → dummy (sem build/rank)
        private readonly List<CharacterBuild> buildOptions = new List<CharacterBuild>();
        private bool dragging;
        // Faixa rolável: arraste horizontal rola; arraste vertical pega a unidade pro campo.
        private ScrollRect parentScroll;
        private bool routingToScroll;
        // Hover: realça o fundo do card.
        private Color baseColor;
        private Color hoverColor;

        public CharacterData Character => character;

        public void Bind(CharacterData boundCharacter, SandboxController.SandboxPick boundPick)
        {
            character = boundCharacter;
            pick = boundPick;
            parentScroll = GetComponentInParent<ScrollRect>();
            if (background != null)
            {
                baseColor = background.color;
                hoverColor = Color.Lerp(baseColor, Color.white, 0.12f);
            }

            if (portrait != null)
            {
                portrait.sprite = character != null ? character.cardPortrait : null;
                portrait.enabled = portrait.sprite != null;
            }
            if (nameLabel != null) nameLabel.text = character != null ? character.displayName : "—";

            bool hasConfig = pick != null;
            if (buildRankGroup != null) buildRankGroup.SetActive(hasConfig);

            if (hasConfig)
            {
                RebuildBuildOptions();
                RefreshBuildLabel();
                RefreshRankLabel();
                if (buildButton != null) { buildButton.onClick.RemoveListener(CycleBuild); buildButton.onClick.AddListener(CycleBuild); }
                if (rankButton != null) { rankButton.onClick.RemoveListener(CycleRank); rankButton.onClick.AddListener(CycleRank); }
            }
        }

        private void OnDestroy()
        {
            if (buildButton != null) buildButton.onClick.RemoveListener(CycleBuild);
            if (rankButton != null) rankButton.onClick.RemoveListener(CycleRank);
        }

        // === DRAG (arrasta para o campo) ===

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Direção do gesto decide: horizontal → rolar a faixa; vertical (pra cima) → pegar a unidade.
            Vector2 delta = eventData.position - eventData.pressPosition;
            if (parentScroll != null && Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                routingToScroll = true;
                parentScroll.OnBeginDrag(eventData);
                return;
            }

            routingToScroll = false;
            if (character == null || PreparationManager.Instance == null) return;
            dragging = true;
            PreparationManager.Instance.StartDraggingFromRoulette(character, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (routingToScroll)
            {
                if (parentScroll != null) parentScroll.OnDrag(eventData);
                return;
            }
            if (!dragging) return;
            PreparationManager.Instance.UpdateDragging(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (routingToScroll)
            {
                routingToScroll = false;
                if (parentScroll != null) parentScroll.OnEndDrag(eventData);
                return;
            }
            if (!dragging) return;
            dragging = false;
            PreparationManager.Instance.FinishDragging(eventData.position);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null) background.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null) background.color = baseColor;
        }

        // === BUILD / RANK ===

        private void RebuildBuildOptions()
        {
            buildOptions.Clear();
            buildOptions.Add(null); // "Padrão" (defaultBuild via resolver)
            if (pick != null && pick.character != null)
            {
                var customs = BuildStore.GetCustomBuilds(pick.character.ID);
                for (int i = 0; i < customs.Count; i++)
                    buildOptions.Add(customs[i]);
            }
        }

        private void CycleBuild()
        {
            if (pick == null || buildOptions.Count == 0) return;
            int current = buildOptions.IndexOf(pick.build);
            if (current < 0) current = 0;
            pick.build = buildOptions[(current + 1) % buildOptions.Count];
            RefreshBuildLabel();
        }

        private void CycleRank()
        {
            if (pick == null) return;
            pick.rank = pick.rank >= MaxRank ? 1 : pick.rank + 1;
            RefreshRankLabel();
        }

        private void RefreshBuildLabel()
        {
            if (buildLabel == null) return;
            if (pick == null || pick.build == null) { buildLabel.text = "Padrão"; return; }
            buildLabel.text = string.IsNullOrEmpty(pick.build.label) ? "Custom" : pick.build.label;
        }

        private void RefreshRankLabel()
        {
            if (rankLabel != null) rankLabel.text = "R" + (pick != null ? pick.rank : 1);
        }
    }
}
