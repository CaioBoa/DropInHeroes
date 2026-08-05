using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Registro central de regras de combate. Regras miram grupos (por tag) e são aplicadas
    /// automaticamente às unidades em combate — inclusive invocações criadas DEPOIS de a regra existir.
    /// É o ponto onde itens/relíquias plugam (ver <see cref="CombatItem"/> e CombatController.Rules).
    /// </summary>
    public class CombatRulesRegistry
    {
        private readonly List<CombatRule> rules = new List<CombatRule>();
        private readonly List<UnitController> units = new List<UnitController>();

        /// <summary>Adiciona uma regra e a aplica retroativamente às unidades já em combate.</summary>
        public void AddRule(CombatRule rule)
        {
            if (rule == null || rules.Contains(rule)) return;
            rules.Add(rule);
            for (int i = 0; i < units.Count; i++)
                if (units[i] != null && rule.Matches(units[i])) rule.Apply(units[i]);
        }

        public void RemoveRule(CombatRule rule)
        {
            if (rule == null || !rules.Remove(rule)) return;
            for (int i = 0; i < units.Count; i++)
                if (units[i] != null && rule.Matches(units[i])) rule.Remove(units[i]);
        }

        /// <summary>Registra uma unidade que entrou em combate e aplica as regras que casam com ela.</summary>
        public void RegisterUnit(UnitController unit)
        {
            if (unit == null || units.Contains(unit)) return;
            units.Add(unit);
            for (int i = 0; i < rules.Count; i++)
                if (rules[i].Matches(unit)) rules[i].Apply(unit);
        }

        public void UnregisterUnit(UnitController unit)
        {
            if (unit == null || !units.Remove(unit)) return;
            for (int i = 0; i < rules.Count; i++)
                if (rules[i].Matches(unit)) rules[i].Remove(unit);
        }

        /// <summary>Limpa todas as regras e unidades (fim do combate).</summary>
        public void Clear()
        {
            for (int u = 0; u < units.Count; u++)
                if (units[u] != null)
                    for (int r = 0; r < rules.Count; r++)
                        if (rules[r].Matches(units[u])) rules[r].Remove(units[u]);
            rules.Clear();
            units.Clear();
        }
    }
}
