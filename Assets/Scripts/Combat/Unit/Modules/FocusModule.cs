using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Decide qual inimigo a unidade foca. Pipeline plugável: taunt (foco forçado) > estratégia da
    /// unidade > default (mais próximo). Respeita furtividade com fallback de último recurso (uma
    /// unidade não-focável volta a ser alvo se for a única viva). Reutiliza <see cref="TargetSelection"/>
    /// — mesmas primitivas das skills. O <see cref="CombatModule"/> delega a seleção de alvo a este módulo.
    /// </summary>
    public class FocusModule : IUnitModule
    {
        private UnitController controller;
        private StatusModule status;
        private Transform transform;

        // Buffer reutilizado para evitar alocação por frame.
        private readonly List<UnitController> buffer = new List<UnitController>();
        // Provedores de estratégia (id -> critério + prioridade). Maior prioridade vence; default Closest.
        private readonly List<StrategyEntry> strategies = new List<StrategyEntry>();

        private struct StrategyEntry
        {
            public string id;
            public TargetCriterion criterion;
            public int priority;
        }

        // === IUnitModule ===

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            transform = controller.transform;
            status = controller.GetModule<StatusModule>();
        }

        public void OnEnabled() { }

        public void OnDisabled()
        {
            strategies.Clear();
            buffer.Clear();
        }

        public void Cleanup()
        {
            strategies.Clear();
            buffer.Clear();
            controller = null;
            status = null;
            transform = null;
        }

        // === ESTRATÉGIA (passivas/itens plugam aqui) ===

        public void SetFocusStrategy(string id, TargetCriterion criterion, int priority = 0)
        {
            for (int i = 0; i < strategies.Count; i++)
            {
                if (strategies[i].id != id) continue;
                strategies[i] = new StrategyEntry { id = id, criterion = criterion, priority = priority };
                return;
            }
            strategies.Add(new StrategyEntry { id = id, criterion = criterion, priority = priority });
        }

        public void RemoveFocusStrategy(string id)
        {
            for (int i = strategies.Count - 1; i >= 0; i--)
                if (strategies[i].id == id) { strategies.RemoveAt(i); return; }
        }

        private TargetCriterion ActiveStrategy()
        {
            TargetCriterion criterion = TargetCriterion.Closest;
            int bestPriority = int.MinValue;
            for (int i = 0; i < strategies.Count; i++)
            {
                if (strategies[i].priority <= bestPriority) continue;
                bestPriority = strategies[i].priority;
                criterion = strategies[i].criterion;
            }
            return criterion;
        }

        // === SELEÇÃO DE ALVO (chamado pelo CombatModule) ===

        /// <summary>
        /// Resolve o alvo a focar entre 'enemies'. Ordem: taunt forçado → candidatos focáveis pela
        /// estratégia → fallback de furtividade (reinclui não-focáveis se nenhum restar).
        /// </summary>
        public UnitController ResolveTarget(IReadOnlyList<UnitController> enemies, IReadOnlyList<UnitController> allies, bool inRangeOnly, float range)
        {
            if (enemies == null || enemies.Count == 0) return null;

            UnitController taunt = ResolveTaunt(enemies, allies);
            if (taunt != null) return taunt;

            UnitController picked = SelectFrom(enemies, inRangeOnly, range, requireTargetable: true);
            if (picked == null)
                picked = SelectFrom(enemies, inRangeOnly, range, requireTargetable: false); // último focável
            return picked;
        }

        /// <summary>
        /// O alvo atual ainda é válido para manter sem re-focar? Considera morte, taunt e furtividade
        /// (uma unidade que ficou não-focável continua válida só se não houver outra focável viva).
        /// </summary>
        public bool IsValidFocus(UnitController target, IReadOnlyList<UnitController> enemies, IReadOnlyList<UnitController> allies)
        {
            if (!TargetSelection.IsAlive(target)) return false;

            if (status != null && status.TryGetForcedFocus(out _, out _))
            {
                UnitController taunt = ResolveTaunt(enemies, allies);
                return taunt == null || taunt == target; // sob taunt, só o alvo provocado é válido
            }

            if (!TargetSelection.IsTargetableUnit(target) && HasOtherTargetable(enemies, target))
                return false; // ficou furtivo e há outro focável vivo → deve trocar

            return true;
        }

        // === PRIVADO ===

        private UnitController ResolveTaunt(IReadOnlyList<UnitController> enemies, IReadOnlyList<UnitController> allies)
        {
            if (status == null || !status.TryGetForcedFocus(out TargetQuery query, out UnitController applier))
                return null;

            buffer.Clear();
            TargetContext ctx = TargetContext.FromUnit(controller, enemies, allies, null, applier);
            query.Resolve(ctx, buffer);
            for (int i = 0; i < buffer.Count; i++)
                if (TargetSelection.IsAlive(buffer[i])) return buffer[i];
            return null; // taunt sem alvo válido → cai para o foco normal
        }

        private UnitController SelectFrom(IReadOnlyList<UnitController> enemies, bool inRangeOnly, float range, bool requireTargetable)
        {
            buffer.Clear();
            Vector2 self = transform.position;
            float sqrRange = range * range;

            for (int i = 0; i < enemies.Count; i++)
            {
                UnitController e = enemies[i];
                if (!TargetSelection.IsAlive(e)) continue;
                if (requireTargetable && !e.IsTargetable) continue;
                if (inRangeOnly && ((Vector2)e.transform.position - self).sqrMagnitude > sqrRange) continue;
                buffer.Add(e);
            }

            if (buffer.Count == 0) return null;
            TargetSelection.Reduce(buffer, ActiveStrategy(), self, 1);
            return buffer.Count > 0 ? buffer[0] : null;
        }

        private static bool HasOtherTargetable(IReadOnlyList<UnitController> enemies, UnitController except)
        {
            if (enemies == null) return false;
            for (int i = 0; i < enemies.Count; i++)
            {
                UnitController e = enemies[i];
                if (e != except && TargetSelection.IsTargetableUnit(e)) return true;
            }
            return false;
        }
    }
}
