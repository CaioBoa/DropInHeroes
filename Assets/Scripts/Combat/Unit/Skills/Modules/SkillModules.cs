using System;
using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Vocabulário de módulos code-invokable que as habilidades compõem. Cada método FAZ uma coisa
    /// sobre alvos já resolvidos (ver <see cref="Targets"/>). Os módulos PERSISTENTES (modificadores
    /// reversíveis) registram a reversão em <see cref="PersistentEffects"/> — a passiva/skill não
    /// gerencia remoção. (Fatia inicial: o que a passiva do Guliver e da Marda precisam.)
    /// </summary>
    public static class SkillModules
    {
        // Buffer transitório do LeapToBestAoe (síncrono, single-thread — nada retido entre chamadas).
        private static readonly List<Vector2> leapPoints = new List<Vector2>();

        // === Stats (persistentes/reversíveis) ===

        /// <summary>Modificador FLAT estático num stat de cada alvo (base 100 → valor 30 = +30%). Reversível.</summary>
        public static void StatModifier(PersistentEffects persistent, IReadOnlyList<UnitController> targets, StatType stat, float flat)
        {
            if (targets == null || Mathf.Approximately(flat, 0f)) return;
            string id = "sm_" + Guid.NewGuid().ToString("N");
            for (int i = 0; i < targets.Count; i++)
                targets[i]?.Stats?.GetStatObject(stat)?.AddModifier(id, flat, 0f);
            persistent.Add(() =>
            {
                for (int i = 0; i < targets.Count; i++)
                    targets[i]?.Stats?.GetStatObject(stat)?.RemoveModifier(id);
            });
        }

        /// <summary>
        /// Modificador DINÂMICO num stat, valor = <paramref name="valuePerCount"/> × contagem(alvo),
        /// reavaliado a cada leitura. Ex.: DamageBonus do inimigo −X por debuff ativo nele. Reversível.
        /// </summary>
        public static void StatModifierPerCount(PersistentEffects persistent, IReadOnlyList<UnitController> targets,
            StatType stat, float valuePerCount, Func<UnitController, int> count)
        {
            if (targets == null || count == null) return;
            string id = "smc_" + Guid.NewGuid().ToString("N");
            for (int i = 0; i < targets.Count; i++)
            {
                UnitController target = targets[i]; // capturado por instância
                target?.Stats?.GetStatObject(stat)?.AddDynamicModifier(id, () => valuePerCount * count(target));
            }
            persistent.Add(() =>
            {
                for (int i = 0; i < targets.Count; i++)
                    targets[i]?.Stats?.GetStatObject(stat)?.RemoveDynamicModifier(id);
            });
        }

        // === Dano (one-shot) ===

        /// <summary>
        /// Causa dano aos <paramref name="targets"/> pelo caminho canônico (crit/lifesteal/notificações
        /// via <see cref="DamageApplier"/>). Rola esquiva contra inimigos (compartilhada pelo golpe via
        /// <paramref name="state"/>). Ataque "sem dano" = simplesmente não chamar este módulo.
        /// </summary>
        public static void Damage(in SkillContext context, List<UnitController> targets, StatScaling[] scalings,
            DamageType type, ref EffectRunState state, float baseDamage = 0f, bool cannotCrit = false, bool ignoreTypeAdvantage = false)
        {
            if (targets == null || targets.Count == 0) return;
            HitChanceResolver.FilterDodged(context, targets, state.dodgeRolls);
            if (targets.Count == 0) { state.previousTargetCount = 0; return; }
            var config = new DamageConfig
            {
                damageType = type,
                cannotCrit = cannotCrit || !context.allowCrit,
                ignoreTypeAdvantage = ignoreTypeAdvantage
            };
            DamageApplier.Apply(context, targets, baseDamage, scalings, config);
            state.previousTargetCount = targets.Count;
        }

        // === Status (one-shot) ===

        /// <summary>
        /// Aplica um status (tipo na tabela) aos alvos, com duração/mods/chance parametrizados. Rola
        /// esquiva compartilhada pelo golpe (no-op para aliados). A chance final (Efetividade×Resistência)
        /// e o escalonamento de controle são resolvidos por <see cref="StatusModule.ApplyStatus"/>.
        /// </summary>
        public static void Status(in SkillContext context, List<UnitController> targets, string statusId,
            float duration, StatModification[] modifications, ref EffectRunState state,
            float dps = -1f, float chance = 1f, float[] durationByRank = null)
        {
            if (targets == null || targets.Count == 0 || string.IsNullOrEmpty(statusId)) return;
            HitChanceResolver.FilterDodged(context, targets, state.dodgeRolls);
            float dur = (durationByRank != null && durationByRank.Length > 0)
                ? RankTiers.ValueFor(durationByRank, context.owner != null ? context.owner.Rank : 1, duration)
                : duration;
            for (int i = 0; i < targets.Count; i++)
            {
                StatusModule sm = targets[i].GetModule<StatusModule>();
                StatsModule ts = targets[i].Stats;
                if (sm == null || ts == null || ts.IsDead) continue;
                sm.ApplyStatus(statusId, dur, modifications, dps, chance, context.owner);
            }
        }

        // === Cura (one-shot) ===

        /// <summary>Cura os alvos (HealCalculator + CauseHeal/ReceiveHeal). scaleByPreviousHits multiplica pelo nº de alvos do efeito anterior.</summary>
        public static void Heal(in SkillContext context, List<UnitController> targets, StatScaling[] scalings,
            ref EffectRunState state, float baseHeal = 0f, bool scaleByPreviousHits = false)
        {
            if (targets == null || targets.Count == 0) return;
            HitChanceResolver.FilterDodged(context, targets, state.dodgeRolls); // no-op p/ aliados
            float raw = HealCalculator.Calculate(context.ownerStats, baseHeal, scalings);
            if (scaleByPreviousHits) raw *= state.previousTargetCount;
            float caused = context.ownerStats != null ? context.ownerStats.CauseHeal(raw) : raw;
            if (caused <= 0f) return;
            for (int i = 0; i < targets.Count; i++)
            {
                StatsModule ts = targets[i].Stats;
                if (ts == null || ts.IsDead) continue;
                ts.ReceiveHeal(caused);
            }
        }

        // === VFX (one-shot) ===

        /// <summary>Toca um efeito visual sobre cada alvo (herda a sorting layer do alvo, renderiza acima).</summary>
        public static void Vfx(in SkillContext context, List<UnitController> targets, AnimationClip clip, ref EffectRunState state,
            float scale = 1f, float alpha = 1f, float fitDiameter = 0f, int sortingOffset = 1, bool followTarget = false,
            float zOffset = 0f, float duration = 0f, float fadeOut = 0f, bool flipX = false, bool flipY = false)
        {
            if (clip == null || targets == null || targets.Count == 0) return;
            HitChanceResolver.FilterDodged(context, targets, state.dodgeRolls);
            for (int i = 0; i < targets.Count; i++)
            {
                UnitController target = targets[i];
                int layerId = 0; int order = sortingOffset;
                var renderer = target.Visual?.SpriteRenderer;
                if (renderer != null) { layerId = renderer.sortingLayerID; order = renderer.sortingOrder + sortingOffset; }
                VfxPlayer.Instance.Play(new VfxRequest
                {
                    clip = clip,
                    parent = followTarget ? target.transform : null,
                    localPosition = followTarget ? new Vector3(0f, 0f, zOffset) : target.transform.position,
                    alpha = alpha, sortingLayerId = layerId, sortingOrder = order, scale = scale,
                    fitDiameter = fitDiameter, duration = duration, fadeOut = fadeOut, flipX = flipX, flipY = flipY
                });
            }
        }

        // === Deslocamento (one-shot, onCast) ===

        /// <summary>Salta para a posição que maximiza a cobertura da AoE (raio) no lado escolhido. Fallback: salta ao alvo atual.</summary>
        public static void LeapToBestAoe(in SkillContext context, float radius, bool clusterEnemies, float duration)
        {
            if (context.owner == null) return;
            CombatModule combat = context.owner.GetModule<CombatModule>();
            if (combat == null) return;

            IReadOnlyList<UnitController> pool = clusterEnemies ? context.enemies : context.allies;
            leapPoints.Clear();
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                {
                    UnitController u = pool[i];
                    if (u == null || !u.IsTargetable) continue;
                    StatsModule st = u.Stats;
                    if (st == null || st.IsDead) continue;
                    leapPoints.Add(u.transform.position);
                }

            Vector3 ownerPos = context.owner.transform.position;
            if (leapPoints.Count == 0)
            {
                if (context.target != null) combat.LeapTo(context.target.transform.position, duration);
                return;
            }
            Vector2 best = AoeClusterTargeting.BestCoveragePoint(leapPoints, radius, ownerPos);
            combat.LeapTo(new Vector3(best.x, best.y, ownerPos.z), duration);
        }

        // === Stats: escala dinâmica de outro stat (persistente/reversível) ===

        /// <summary>Modificador DINÂMICO num stat = percent × stat-fonte do <paramref name="sourceUnit"/> (reavaliado a cada leitura). Ex.: EnergyRegen += 1% do MagicalPower do dono. Reversível.</summary>
        public static void StatModifierPercentOfStat(PersistentEffects persistent, IReadOnlyList<UnitController> targets,
            StatType stat, StatType sourceStat, float percent, UnitController sourceUnit)
        {
            if (targets == null || sourceUnit == null || sourceUnit.Stats == null || Mathf.Approximately(percent, 0f)) return;
            StatsModule src = sourceUnit.Stats;
            string id = "smp_" + Guid.NewGuid().ToString("N");
            for (int i = 0; i < targets.Count; i++)
                targets[i]?.Stats?.GetStatObject(stat)?.AddDynamicModifier(id, () => src.GetStat(sourceStat) * percent);
            persistent.Add(() =>
            {
                for (int i = 0; i < targets.Count; i++)
                    targets[i]?.Stats?.GetStatObject(stat)?.RemoveDynamicModifier(id);
            });
        }

        // === Escudo (one-shot) ===

        /// <summary>Concede um escudo (renova pelo id) aos alvos, valor escalado nos stats do dono (via CauseShield/ReceiveShield).</summary>
        public static void Shield(in SkillContext context, List<UnitController> targets, string shieldId,
            StatScaling[] scalings, ref EffectRunState state, float baseAmount = 0f)
        {
            if (targets == null || targets.Count == 0 || string.IsNullOrEmpty(shieldId)) return;
            HitChanceResolver.FilterDodged(context, targets, state.dodgeRolls); // no-op p/ aliados
            float raw = baseAmount;
            StatsModule owner = context.ownerStats;
            if (owner != null && scalings != null)
                for (int j = 0; j < scalings.Length; j++)
                    raw += owner.GetStat(scalings[j].stat) * scalings[j].multiplier;
            float caused = owner != null ? owner.CauseShield(raw) : raw;
            if (caused <= 0f) return;
            for (int i = 0; i < targets.Count; i++)
            {
                StatsModule ts = targets[i].Stats;
                if (ts == null || ts.IsDead) continue;
                targets[i].GetModule<ShieldModule>()?.AddShield(shieldId, caused);
            }
        }

        // === Ganho de stat permanente (one-shot; acumula no combate) ===

        /// <summary>Soma permanente-no-combate num stat dos alvos: amount + fonte×scalePercent% (do dono). Ex.: ganhar Ataque por golpe.</summary>
        public static void GainStat(in SkillContext context, List<UnitController> targets, StatType stat,
            float amount = 0f, StatType scaleStat = StatType.MagicalPower, float scalePercent = 0f)
        {
            if (targets == null || targets.Count == 0) return;
            float gain = amount;
            if (scalePercent != 0f) gain += (context.ownerStats?.GetStat(scaleStat) ?? 0f) * scalePercent / 100f;
            if (Mathf.Approximately(gain, 0f)) return;
            for (int i = 0; i < targets.Count; i++)
                targets[i].GetModule<StatsModule>()?.ModifyStat(stat, gain);
        }

        // === Energia (one-shot) ===

        /// <summary>Soma energia a cada alvo (no-op para quem não tem energia).</summary>
        public static void GainEnergy(in SkillContext context, List<UnitController> targets, float amount)
        {
            if (targets == null || Mathf.Approximately(amount, 0f)) return;
            for (int i = 0; i < targets.Count; i++)
            {
                StatsModule st = targets[i].Stats;
                if (st == null || st.IsDead) continue;
                st.GetResourceObject(ResourceType.Energy)?.Add(amount);
            }
        }

        // === Purificar (one-shot) ===

        /// <summary>Remove até 'count' debuffs de cada alvo (mais antigos primeiro).</summary>
        public static void Cleanse(in SkillContext context, List<UnitController> targets, int count)
        {
            if (targets == null || count <= 0) return;
            for (int i = 0; i < targets.Count; i++)
            {
                StatsModule st = targets[i].Stats;
                if (st == null || st.IsDead) continue;
                targets[i].GetModule<StatusModule>()?.RemoveDebuffs(count);
            }
        }

        // === Deslocamento até o alvo (one-shot, onCast) ===

        /// <summary>Salta o dono até <paramref name="target"/>, parando a <paramref name="gap"/> de distância.</summary>
        public static void LeapToTarget(in SkillContext context, UnitController target, float duration, float gap)
        {
            if (context.owner == null || target == null) return;
            CombatModule combat = context.owner.GetModule<CombatModule>();
            if (combat == null) return;
            Vector3 ownerPos = context.owner.transform.position;
            Vector3 toTarget = target.transform.position - ownerPos;
            float dist = toTarget.magnitude;
            Vector3 landing = dist > gap ? target.transform.position - toTarget.normalized * gap : ownerPos;
            combat.LeapTo(landing, duration);
        }

        // === Animação extra (one-shot) ===

        /// <summary>Toca uma animação extra (one-shot) por chave nos alvos (via VisualModule).</summary>
        public static void PlayExtra(in SkillContext context, List<UnitController> targets, string animationKey, float speedMultiplier = 1f)
        {
            if (string.IsNullOrEmpty(animationKey) || targets == null) return;
            for (int i = 0; i < targets.Count; i++)
                targets[i].Visual?.PlayExtraAnimation(animationKey, speedMultiplier);
        }

        // === Invocação (one-shot) ===

        /// <summary>Invoca uma unidade do time do dono na posição dada. Retorna a invocação (ou null).</summary>
        public static UnitController SpawnSummon(in SkillContext context, SummonData data, Vector3 position)
        {
            if (data == null || context.owner == null || CombatController.Instance == null) return null;
            return CombatController.Instance.SpawnSummon(data, context.owner.GetTeam(), position, context.owner);
        }

        // === Compartilhamento de dano (persistente/reversível) ===

        /// <summary>Cada <paramref name="sharers"/> redireciona <paramref name="fraction"/> do dano que sofreria ao <paramref name="receiver"/>. Reversível.</summary>
        public static void ApplyDamageShare(PersistentEffects persistent, IReadOnlyList<UnitController> sharers, UnitController receiver, float fraction)
        {
            if (sharers == null || receiver == null || receiver.Stats == null || fraction <= 0f) return;
            string id = "ds_" + Guid.NewGuid().ToString("N");
            StatsModule receiverStats = receiver.Stats;
            for (int i = 0; i < sharers.Count; i++)
                sharers[i]?.Stats?.AddDamageShare(id, receiverStats, fraction);
            persistent.Add(() =>
            {
                for (int i = 0; i < sharers.Count; i++)
                    sharers[i]?.Stats?.RemoveDamageShare(id);
            });
        }
    }
}
