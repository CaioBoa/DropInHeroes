using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using DropInHeroes.Combat;

namespace DropInHeroes.UI
{
    /// <summary>Resultado da formatação: rich-text (TMP) + keywords COM explicação usadas (pilha lateral).</summary>
    public struct AbilityText
    {
        public string richText;
        public List<KeywordEntry> keywords;
    }

    /// <summary>
    /// Converte o template de descrição de uma habilidade em rich-text TMP. Tags:
    ///   {kw:chave}          — palavra-chave (cor do catálogo; coleta a explicação se houver).
    ///   {v:campo:Stat:fmt}  — valor lido por reflexão do campo da skill, colorido pela cor do Stat.
    ///   {shift: ... }       — segmento exibido só com SHIFT (pode conter outras tags).
    /// fmt: pct (30→"30%"), pctFrac (0.3→"30%"), flat (30→"30"), sec (5→"5s"). Default = pct.
    /// </summary>
    public static class AbilityTextFormatter
    {
        public static AbilityText Format(string template, object skill, KeywordCatalog keywords,
                                         StatDefinitionCatalog stats, bool shift)
        {
            var sb = new StringBuilder();
            var used = new List<KeywordEntry>();
            var seen = new HashSet<string>();
            Parse(template ?? string.Empty, sb, skill, keywords, stats, shift, used, seen);
            return new AbilityText { richText = sb.ToString(), keywords = used };
        }

        private static void Parse(string s, StringBuilder sb, object skill, KeywordCatalog kw,
                                  StatDefinitionCatalog stats, bool shift, List<KeywordEntry> used, HashSet<string> seen)
        {
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '{')
                {
                    int end = MatchBrace(s, i);
                    if (end < 0) { sb.Append(s[i]); i++; continue; }
                    Tag(s.Substring(i + 1, end - i - 1), sb, skill, kw, stats, shift, used, seen);
                    i = end + 1;
                }
                else { sb.Append(s[i]); i++; }
            }
        }

        private static int MatchBrace(string s, int open)
        {
            int depth = 0;
            for (int j = open; j < s.Length; j++)
            {
                if (s[j] == '{') depth++;
                else if (s[j] == '}' && --depth == 0) return j;
            }
            return -1;
        }

        private static void Tag(string inner, StringBuilder sb, object skill, KeywordCatalog kw,
                                StatDefinitionCatalog stats, bool shift, List<KeywordEntry> used, HashSet<string> seen)
        {
            int colon = inner.IndexOf(':');
            string tag = colon >= 0 ? inner.Substring(0, colon) : inner;
            string rest = colon >= 0 ? inner.Substring(colon + 1) : string.Empty;
            switch (tag)
            {
                case "shift":
                    if (shift) Parse(rest, sb, skill, kw, stats, shift, used, seen);
                    break;
                case "kw":
                    Keyword(rest.Trim(), sb, kw, stats, used, seen);
                    break;
                case "v":
                    Value(rest, sb, skill, stats);
                    break;
                default:
                    sb.Append('{').Append(inner).Append('}'); // tag desconhecida: literal
                    break;
            }
        }

        private static void Keyword(string key, StringBuilder sb, KeywordCatalog kw,
                                    StatDefinitionCatalog stats, List<KeywordEntry> used, HashSet<string> seen)
        {
            var e = kw != null ? kw.Get(key) : null;
            Wrap(sb, KeywordColor(e, stats), e != null ? e.Display : key);
            if (e != null && e.HasExplanation && seen.Add(e.key)) used.Add(e);
        }

        private static void Value(string rest, StringBuilder sb, object skill, StatDefinitionCatalog stats)
        {
            var parts = rest.Split(':');
            string text = FormatValue(ReflectFloat(skill, parts[0]), parts.Length > 2 ? parts[2] : "pct");
            Color? col = parts.Length > 1 ? StatColor(parts[1], stats) : null;
            if (col.HasValue) Wrap(sb, col.Value, text); else sb.Append(text);
        }

        private static void Wrap(StringBuilder sb, Color c, string text)
            => sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>').Append(text).Append("</color>");

        private static Color KeywordColor(KeywordEntry e, StatDefinitionCatalog stats)
        {
            if (e == null) return Color.white;
            if (e.useStatColor) { var c = StatColor(e.statColor, stats); if (c.HasValue) return c.Value; }
            return e.hasColor ? e.color : Color.white;
        }

        private static Color? StatColor(string statName, StatDefinitionCatalog stats)
            => System.Enum.TryParse(statName, out StatType t) ? StatColor(t, stats) : null;

        private static Color? StatColor(StatType t, StatDefinitionCatalog stats)
        {
            var def = stats != null ? stats.GetStatDefinition(t) : null;
            return def != null ? def.TintColor : null;
        }

        // Reflete um caminho de campo: "chance" (campo direto) ou "scalings.0.percent" (índice de
        // array/lista + campo aninhado). Permite ler valores dentro de structs como Scaling[].
        private static float ReflectFloat(object skill, string path)
        {
            if (skill == null || string.IsNullOrEmpty(path)) return 0f;
            object cur = skill;
            var segments = path.Split('.');
            for (int i = 0; i < segments.Length && cur != null; i++)
            {
                string seg = segments[i];
                if (int.TryParse(seg, out int idx))
                {
                    cur = cur is System.Collections.IList list && idx >= 0 && idx < list.Count ? list[idx] : null;
                }
                else
                {
                    cur = FindField(cur.GetType(), seg)?.GetValue(cur);
                }
            }
            return cur is float fl ? fl : (cur is int it ? it : 0f);
        }

        // GetField(NonPublic) não enxerga privados HERDADOS via o tipo derivado (ex.: campo declarado
        // na classe-base da skill) — sobe a hierarquia manualmente.
        private static FieldInfo FindField(System.Type type, string name)
        {
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f;
                type = type.BaseType;
            }
            return null;
        }

        private static string FormatValue(float v, string fmt)
        {
            switch (fmt)
            {
                case "pctFrac": return Mathf.RoundToInt(v * 100f) + "%";
                case "invPctFrac": return Mathf.RoundToInt((1f - v) * 100f) + "%"; // multiplicador→redução (0.6→"40%")
                case "absPctFrac": return Mathf.RoundToInt(Mathf.Abs(v) * 100f) + "%"; // fração com sinal→magnitude (-0.3→"30%")
                case "flat": return v.ToString("0.#");
                case "sec": return v.ToString("0.#") + "s";
                default: return Mathf.RoundToInt(v) + "%"; // pct
            }
        }
    }
}
