using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva da Vela — "Jaguareté Aba": TRANSFORMAÇÃO por acúmulo de críticos. Cada acerto crítico soma 1
    /// stack da condição de acúmulo; ao atingir o limiar (por rank), consome os stacks e desperta a forma
    /// Jaguareté (permanente no combate) via <see cref="SkillsModule.SetForm"/>, trocando o bônus persistente
    /// da forma base pelo da forma alvo. Composição de módulos: os bônus de forma são condições
    /// (<see cref="SkillModules.Status"/>) escaladas por rank em código; a troca de conjunto/animação é do
    /// SkillsModule; o crítico chega pelo hook <c>onCriticalHit</c> e a troca é efetivada no <c>onAfterAttack</c>
    /// (fora da pilha de dano). A única lógica própria é o contador e o momento da troca (estado). O SO só
    /// expõe números e chaves.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Vela/Passive")]
    public class VelaPassive : ModularPassive
    {
        [Header("Acúmulo por crítico")]
        [Tooltip("Id da condição de acúmulo (StackMode = Accumulate) na tabela de status. Cada crítico soma 1 stack.")]
        [SerializeField] private string stackConditionId = "jaguarete_stacks";
        [Tooltip("Críticos necessários para transformar, por rank (índice 0 = rank 1). Ex.: 20/16/12.")]
        [SerializeField] private int[] critsToTransformByRank = { 20, 16, 12 };

        [Header("Forma alvo")]
        [Tooltip("Chave da forma (em CharacterData.forms) para a qual transformar ao atingir o limiar.")]
        [SerializeField] private string targetFormKey = "jaguarete";
        [Tooltip("Chave da animação EXTRA (one-shot) tocada no momento da transformação. Vazio = sem clipe.")]
        [SerializeField] private string transformAnimationKey = "transform";

        [Header("Bônus da forma BASE (escala por rank)")]
        [Tooltip("Id da condição persistente enquanto na forma base. Vazio = nenhuma.")]
        [SerializeField] private string baseFormConditionId = "aba_bonus";
        [Tooltip("Modificadores de stat da forma base, por rank (ex.: Regeneração de Energia +30/40/50%).")]
        [SerializeField] private RankScaledStatMod[] baseFormBonus;
        [Tooltip("Multiplicador de cura CAUSADA da forma base, por rank (1.3 = +30%). Vazio = sem bônus.")]
        [SerializeField] private float[] baseFormHealDoneByRank;

        [Header("Bônus da forma ALVO (escala por rank)")]
        [Tooltip("Id da condição persistente enquanto na forma alvo. Vazio = nenhuma.")]
        [SerializeField] private string targetFormConditionId = "jaguarete_bonus";
        [Tooltip("Modificadores de stat da forma alvo, por rank (ex.: Lifesteal +15/20/25, Ataque +20/35/50%).")]
        [SerializeField] private RankScaledStatMod[] targetFormBonus;
        [Tooltip("Multiplicador de cura CAUSADA da forma alvo, por rank. Vazio = sem bônus.")]
        [SerializeField] private float[] targetFormHealDoneByRank;

        // Estado de runtime (clone por unidade).
        private UnitController owner;
        private SkillsModule skills;
        private StatusModule status;
        private VisualModule visual;
        private SkillContext ctx;
        private bool transformed;
        private bool pendingTransform;

        protected override void OnReset()
        {
            transformed = false;
            pendingTransform = false;
        }

        protected override void Compose(SkillContext context)
        {
            ctx = context;
            owner = context.owner;
            skills = context.skills;
            visual = context.ownerVisual;
            status = owner.GetModule<StatusModule>();
            PassiveHooks hooks = context.skills?.Hooks;
            if (status == null || hooks == null) return;

            // Bônus da forma ATUAL. No nascer, transformed=false → forma base; num revive já transformado,
            // reafirma o bônus da forma alvo (as skills clonadas continuam as da forma alvo).
            if (transformed)
            {
                ApplyBonus(targetFormConditionId, targetFormBonus, targetFormHealDoneByRank);
            }
            else
            {
                // Início limpo na forma base (defensivo entre rodadas): remove restos da forma alvo.
                RemoveCondition(targetFormConditionId);
                RemoveCondition(stackConditionId);
                visual?.ResetAnimationProfile();
                ApplyBonus(baseFormConditionId, baseFormBonus, baseFormHealDoneByRank);
            }

            hooks.onCriticalHit += OnCriticalHit;
            hooks.onAfterAttack += OnAfterAttack;
            Persistent.Add(() =>
            {
                hooks.onCriticalHit -= OnCriticalHit;
                hooks.onAfterAttack -= OnAfterAttack;
            });
        }

        private void OnCriticalHit(UnitController target)
        {
            if (transformed || status == null || string.IsNullOrEmpty(stackConditionId)) return;

            status.ApplyStatus(stackConditionId, 0f, null, -1f, 1f, owner); // Accumulate → +1 stack

            int threshold = RankValue(critsToTransformByRank);
            if (threshold > 0 && status.StacksOf(stackConditionId) >= threshold)
                pendingTransform = true; // efetiva no fim do ataque (fora da pilha de dano/OnHit)
        }

        // A troca reinstancia clones de skill — executa DEPOIS do golpe (onAfterAttack), nunca dentro do OnHit.
        private void OnAfterAttack()
        {
            if (pendingTransform && !transformed) Transform();
        }

        private void Transform()
        {
            transformed = true;
            pendingTransform = false;

            RemoveCondition(stackConditionId);
            RemoveCondition(baseFormConditionId);
            ApplyBonus(targetFormConditionId, targetFormBonus, targetFormHealDoneByRank);

            // Toca o clipe de transformação (one-shot) ANTES de trocar o conjunto — o estado Extra roda por
            // cima e, ao terminar, cai no idle já da nova forma (SetForm troca o perfil por baixo).
            if (!string.IsNullOrEmpty(transformAnimationKey)) visual?.PlayExtraAnimation(transformAnimationKey);
            skills?.SetForm(targetFormKey); // troca ataque + supremo + perfil de animação
        }

        // Aplica a condição-bônus da forma (via módulo de status) com mods/cura escalados para o rank atual.
        private void ApplyBonus(string conditionId, RankScaledStatMod[] bonus, float[] healDoneByRank)
        {
            if (string.IsNullOrEmpty(conditionId) || status == null) return;
            int rank = owner != null ? owner.Rank : 1;
            StatModification[] mods = BuildMods(bonus, rank);

            // Bônus de cura CAUSADA da forma é o stat HealBonus (base 100): converte o multiplicador por rank
            // (1.3 = +30%) em +30 e acopla aos modificadores da condição.
            if (healDoneByRank != null && healDoneByRank.Length > 0)
            {
                float healBonus = (RankTiers.ValueFor(healDoneByRank, rank, 1f) - 1f) * 100f;
                if (healBonus != 0f) mods = WithExtra(mods, StatModification.Flat(StatType.HealBonus, healBonus));
            }

            var state = new EffectRunState();
            SkillModules.Status(ctx, Targets.Self(ctx), conditionId, 0f, mods, ref state); // permanente (Overwrite substitui)
        }

        // Devolve os modificadores com um extra anexado (sem mutar o original).
        private static StatModification[] WithExtra(StatModification[] mods, StatModification extra)
        {
            int n = mods != null ? mods.Length : 0;
            var result = new StatModification[n + 1];
            for (int i = 0; i < n; i++) result[i] = mods[i];
            result[n] = extra;
            return result;
        }

        // Constrói os modificadores de stat para o rank atual a partir da tabela por rank.
        private static StatModification[] BuildMods(RankScaledStatMod[] bonus, int rank)
        {
            if (bonus == null || bonus.Length == 0) return null;
            var mods = new StatModification[bonus.Length];
            for (int i = 0; i < bonus.Length; i++)
            {
                float v = RankTiers.ValueFor(bonus[i].valuesByRank, rank, 0f);
                mods[i] = bonus[i].asPercent ? StatModification.Percent(bonus[i].stat, v)
                                             : StatModification.Flat(bonus[i].stat, v);
            }
            return mods;
        }

        private void RemoveCondition(string conditionId)
        {
            if (!string.IsNullOrEmpty(conditionId) && status != null) status.RemoveEffect(conditionId);
        }

        private int RankValue(int[] tiersPerRank)
        {
            int idx = RankTiers.IndexFor(owner != null ? owner.Rank : 1, tiersPerRank != null ? tiersPerRank.Length : 0);
            return idx >= 0 ? tiersPerRank[idx] : 0;
        }
    }
}
