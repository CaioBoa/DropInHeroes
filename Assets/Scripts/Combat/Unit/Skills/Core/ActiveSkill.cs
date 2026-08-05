using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public abstract class ActiveSkill : ScriptableObject
    {
        // Fallbacks compartilhados pelas skills (antes repetidos como magic numbers em cada uma).
        public const float DefaultSpeed = 3f;            // Velocidade assumida quando ownerStats é nulo.
        public const float DefaultAnimReference = 12f;   // Velocidade em que a animação toca em 1×.
        public const float DefaultSkillDuration = 0.5f;  // Duração-fallback quando não há visual/skill.

        [Header("Skill Info")]
        public string displayName;
        [TextArea(2, 5)] public string description;
        [Tooltip("Ícone exibido no painel CharacterInfo. Null = placeholder (quadrado branco).")]
        public Sprite icon;

        [Header("Modificadores da skill (stats extras SÓ durante esta habilidade)")]
        [Tooltip("Stats concedidos ao DONO apenas enquanto esta skill aplica seus efeitos, revertidos logo " +
                 "depois. Genérico: +50 Penetração Mágica numa suprema (ignora metade da def. mágica), " +
                 "+Efetividade antes de um debuff, bônus de cura num heal, Lifesteal num ataque específico. " +
                 "flat na convenção do stat (percentuais em base 100: 50 = 50%); percent = fração (0.30 = +30%).")]
        [SerializeField] private SkillStatModifier[] skillModifiers;

        // Id único por instância (a skill é clonada por unidade) — evita colisão entre skills da mesma unidade.
        private string skillModId;

        public virtual void Initialize(SkillContext context) { }

        public virtual void Clear() { }

        /// <summary>
        /// Multiplicador de velocidade da animação: toca em 1× quando a Velocidade da unidade é
        /// igual a 'reference'. Centraliza o cálculo antes duplicado em todas as skills.
        /// </summary>
        protected static float SpeedMultiplier(SkillContext context, float reference)
        {
            float r = reference > 0f ? reference : DefaultAnimReference;
            return (context.ownerStats?.Speed ?? DefaultSpeed) / r;
        }

        /// <summary>
        /// Concede os <see cref="skillModifiers"/> ao dono ANTES de aplicar os efeitos da skill (par com
        /// <see cref="PopSkillModifiers"/>). Os efeitos e o cálculo de dano leem os stats já modificados —
        /// é assim que "esta habilidade tem +50 de Penetração Mágica" vira efeito real, sem tocar no
        /// DamageCalculator. Sem alocação e no-op quando a lista está vazia.
        /// </summary>
        protected void PushSkillModifiers(in SkillContext context)
        {
            if (skillModifiers == null || skillModifiers.Length == 0) return;
            StatsModule stats = context.ownerStats;
            if (stats == null) return;
            if (string.IsNullOrEmpty(skillModId)) skillModId = "skillmod_" + System.Guid.NewGuid().ToString("N");
            for (int i = 0; i < skillModifiers.Length; i++)
            {
                SkillStatModifier m = skillModifiers[i];
                stats.GetStatObject(m.stat)?.AddModifier(skillModId + "_" + i, m.flat, m.percent);
            }
        }

        /// <summary>Remove os modificadores aplicados por <see cref="PushSkillModifiers"/> (chamar em finally).</summary>
        protected void PopSkillModifiers(in SkillContext context)
        {
            if (skillModifiers == null || skillModifiers.Length == 0 || string.IsNullOrEmpty(skillModId)) return;
            StatsModule stats = context.ownerStats;
            if (stats == null) return;
            for (int i = 0; i < skillModifiers.Length; i++)
                stats.GetStatObject(skillModifiers[i].stat)?.RemoveModifier(skillModId + "_" + i);
        }

        /// <summary>
        /// Executa a skill. Retorna a duração da animação.
        /// </summary>
        public abstract float Execute(SkillContext context);

        /// <summary>
        /// Chamado pelo animation event no frame de impacto.
        /// </summary>
        public abstract void OnHit(SkillContext context);
    }

    /// <summary>
    /// Stat concedido ao dono APENAS durante a aplicação de uma skill (ver <see cref="ActiveSkill"/>).
    /// Reaproveita o sistema de modifiers do <see cref="Stat"/>: flat na convenção do próprio stat
    /// (percentuais em base 100 — Penetração Mágica 50 = 50%), percent = fração multiplicativa.
    /// </summary>
    [System.Serializable]
    public struct SkillStatModifier
    {
        public StatType stat;
        [Tooltip("Valor bruto somado ao stat (base 100 quando percentual: 50 = 50% p/ penetração/efetividade/lifesteal…).")]
        public float flat;
        [Tooltip("Fração multiplicativa aplicada ao stat: 0.30 = +30%.")]
        public float percent;
    }
}
