using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Gerencia a seleção de unidades do campo (em combate ou na preparação), para inspecionar
    /// atributos/status no painel CharacterInfo. Modelo:
    /// • Hover dá preview SÓ quando nada está fixado (navegar antes de escolher).
    /// • Click fixa a unidade (painel estável, hover não troca mais). Click na mesma unidade deseleciona.
    /// • Click em outra unidade troca a seleção. Click no vazio (mapa, fora de UI) deseleciona.
    /// Exibido = pinned ?? hovered (fixado tem prioridade → estabilidade). Dispara
    /// <see cref="OnSelectionChanged"/> com a unidade atual (ou null) e liga/desliga o anel de seleção.
    /// </summary>
    public class UnitSelectionManager : MonoBehaviour
    {
        public static UnitSelectionManager Instance { get; private set; }

        private UnitController pinned;   // clique = seleção fixa (persistente)
        private UnitController hovered;  // cursor sobre = preview (só vale se nada fixado)
        private UnitController current;  // último exibido, para detectar troca
        private Camera cam;

        /// <summary>Disparado quando a unidade exibida muda (null = nada selecionado).</summary>
        public event Action<UnitController> OnSelectionChanged;

        // Fixado tem prioridade: depois de clicar, o painel fica estável (hover não troca).
        public UnitController Current => pinned != null ? pinned : hovered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Clique no vazio (fora de unidade e de UI) deseleciona a unidade fixada.
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (pinned == null) return;
            // Sobre UI (HUD, painel, botões) não deseleciona — o clique pode ser uma interação de UI.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            // Sobre uma unidade: o clique é tratado por UnitSelectable (toggle/troca), não aqui.
            if (IsPointerOverUnit(mouse.position.ReadValue())) return;
            Deselect();
        }

        // Há uma unidade selecionável sob o ponteiro? Raycast 2D direto (independe do timing do EventSystem).
        private bool IsPointerOverUnit(Vector2 screenPos)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;
            Vector2 world = cam.ScreenToWorldPoint(screenPos);
            var hit = Physics2D.OverlapPoint(world);
            return hit != null && hit.GetComponentInParent<UnitSelectable>() != null;
        }

        /// <summary>Click numa unidade: alterna — fixa se nova; deseleciona se já era a fixada.</summary>
        public void Click(UnitController unit)
        {
            if (unit == null) return;
            if (pinned == unit)
            {
                // Toggle off: limpa também o hover para o painel sumir na hora, mesmo o cursor
                // ainda estando sobre a unidade (o preview volta se sair e entrar de novo).
                pinned = null;
                hovered = null;
            }
            else
            {
                pinned = unit;
            }
            Refresh();
        }

        public void SetHovered(UnitController unit)
        {
            hovered = unit;
            Refresh();
        }

        public void ClearHovered(UnitController unit)
        {
            if (hovered == unit) { hovered = null; Refresh(); }
        }

        /// <summary>Deseleciona a unidade fixada (ex.: clique no vazio).</summary>
        public void Deselect()
        {
            pinned = null;
            Refresh();
        }

        /// <summary>
        /// Notifica que uma unidade foi devolvida ao pool (desativada). Limpa qualquer referência
        /// a ela — sem isto o painel fica preso a um objeto reciclado que voltará como outro
        /// personagem (pool = SetActive(false), a referência continua viva).
        /// </summary>
        public void NotifyUnitDespawned(UnitController unit)
        {
            if (unit == null) return;
            if (pinned == unit) pinned = null;
            if (hovered == unit) hovered = null;
            // Se a unidade estava sendo exibida, Current agora difere de 'current' → Refresh
            // dispara OnSelectionChanged e o painel solta o objeto reciclado.
            Refresh();
        }

        private void Refresh()
        {
            UnitController next = Current;
            if (next == current) return;

            SetHighlight(current, false);
            SetHighlight(next, true);

            current = next;
            OnSelectionChanged?.Invoke(current);
        }

        private static void SetHighlight(UnitController u, bool on)
        {
            if (u == null) return;
            u.GetComponent<UnitSelectable>()?.SetHighlighted(on);
        }
    }
}
