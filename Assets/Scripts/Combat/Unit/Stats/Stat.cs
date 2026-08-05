using System;
using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class Stat
    {
        public StatType Type { get; }
        public string DisplayName { get; }

        private float baseValue;
        private float manualValue;            // base + ajustes manuais (Modify/SetValue)
        private float? minValue;
        private float? maxValue;

        // Modificadores nomeados de buffs/debuffs. Cada um soma um flat e/ou um percentual.
        // Mantidos separados do manualValue para poderem ser adicionados/removidos individualmente.
        private readonly Dictionary<string, StatModifier> modifiers = new Dictionary<string, StatModifier>();

        // Modificadores dinâmicos: um provedor de valor FLAT avaliado a cada leitura. Para stats que
        // variam continuamente (ex.: defesa proporcional à vida perdida) sem recalcular por frame.
        private readonly Dictionary<string, Func<float>> dynamicFlat = new Dictionary<string, Func<float>>();

        public float CurrentValue => ClampValue(ComputeValue());
        public float HypotheticalValue => manualValue;
        public float BaseValue => baseValue;

        public Stat(StatType type, float baseValue, StatDefinition definition)
        {
            Type = type;
            this.baseValue = baseValue;
            manualValue = baseValue;
            minValue = definition?.MinValue;
            maxValue = definition?.MaxValue;
            DisplayName = definition?.displayName ?? type.ToString();
        }

        public void Modify(float amount) => manualValue += amount;

        public void SetValue(float value) => manualValue = value;

        public void SetBaseValue(float value)
        {
            baseValue = value;
            manualValue = value;
        }

        /// <summary>Reverte ao valor base e remove todos os modificadores (buffs/debuffs).</summary>
        public void Reset()
        {
            manualValue = baseValue;
            modifiers.Clear();
            dynamicFlat.Clear();
        }

        // === Modificadores (buffs/debuffs) ===

        /// <summary>Adiciona ou substitui um modificador nomeado. percent: -0.30 = -30%.</summary>
        public void AddModifier(string id, float flat, float percent)
        {
            modifiers[id] = new StatModifier(flat, percent);
        }

        public void RemoveModifier(string id) => modifiers.Remove(id);

        public bool HasModifier(string id) => modifiers.ContainsKey(id);

        /// <summary>Adiciona um modificador dinâmico (flat) avaliado a cada leitura do valor.</summary>
        public void AddDynamicModifier(string id, Func<float> flatProvider) => dynamicFlat[id] = flatProvider;

        public void RemoveDynamicModifier(string id) => dynamicFlat.Remove(id);

        private float ComputeValue()
        {
            if (modifiers.Count == 0 && dynamicFlat.Count == 0) return manualValue;

            float flatSum = 0f;
            float percentSum = 0f;
            foreach (var mod in modifiers.Values)
            {
                flatSum += mod.Flat;
                percentSum += mod.Percent;
            }
            foreach (var provider in dynamicFlat.Values)
                flatSum += provider();

            float multiplier = Mathf.Max(0f, 1f + percentSum);
            return (manualValue + flatSum) * multiplier;
        }

        private float ClampValue(float value)
        {
            if (minValue.HasValue) value = Mathf.Max(value, minValue.Value);
            if (maxValue.HasValue) value = Mathf.Min(value, maxValue.Value);
            return value;
        }

        private readonly struct StatModifier
        {
            public readonly float Flat;
            public readonly float Percent;

            public StatModifier(float flat, float percent)
            {
                Flat = flat;
                Percent = percent;
            }
        }
    }
}
