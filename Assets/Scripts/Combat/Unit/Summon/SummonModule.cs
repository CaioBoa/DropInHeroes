using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Módulo da mecânica de invocação. Centraliza o que antes vivia espalhado (grafo dono↔invocação no
    /// UnitController, snapshot de stats na passiva do Rikurby):
    /// - Grafo dono↔invocação (owner + lista de summons), delegado pelo UnitController.
    /// - Derivação de stats no spawn via <see cref="SummonStatProfile"/> da CharacterData da invocação.
    /// - Camada data-driven de BUFFS que uma unidade concede às SUAS invocações — aplicados às vivas e
    ///   às futuras, removidos no fim. É a base para itens como "minhas invocações causam +40% de dano"
    ///   ou "minhas invocações têm +X de vida" (o item só chama AddSummon*; sem código novo por item).
    ///
    /// Nota sobre o caso inverso ("sofrer -20% de dano DE invocações"): é um modificador de dano de
    /// ENTRADA da própria unidade, condicionado à tag da fonte — usa o sistema existente
    /// (<see cref="StatsModule.AddIncomingDamageModifier"/> + <see cref="DamageContext.SourceHasTag"/>,
    /// ou <c>ctx.source.Controller.Owner</c> para "invocações de tal unidade"). Não precisa deste módulo.
    /// </summary>
    public class SummonModule : IUnitModule
    {
        private UnitController controller;

        // Grafo dono↔invocação: quem invocou esta unidade e as invocações que ela criou.
        private UnitController owner;
        private readonly List<UnitController> summons = new List<UnitController>();

        // Buffs que ESTA unidade concede às suas invocações (id → buff). Reaplicados a cada nova invocação.
        private readonly Dictionary<string, SummonBuff> summonBuffs = new Dictionary<string, SummonBuff>();

        public UnitController Owner => owner;
        public IReadOnlyList<UnitController> Summons => summons;

        // === IUnitModule ===

        public void Initialize(UnitController unitController) => controller = unitController;
        public void OnEnabled() { }
        public void OnDisabled() { }

        public void Cleanup()
        {
            ResetForPool();
            controller = null;
        }

        public void ResetForPool()
        {
            owner = null;
            summons.Clear();
            summonBuffs.Clear();
        }

        // === Grafo dono↔invocação ===

        public void SetOwner(UnitController newOwner)
        {
            owner = newOwner;
            newOwner?.GetModule<SummonModule>()?.RegisterSummon(controller);
        }

        private void RegisterSummon(UnitController summon)
        {
            if (summon == null || summons.Contains(summon)) return;
            summons.Add(summon);
            ApplyBuffsTo(summon); // buffs de invocação desta unidade valem para a recém-nascida
        }

        public void UnregisterSummon(UnitController summon)
        {
            if (summons.Remove(summon))
                RemoveBuffsFrom(summon);
        }

        // === Derivação de stats no spawn ===

        /// <summary>
        /// Deriva os stats desta invocação a partir do dono, via o <see cref="SummonStatProfile"/> da
        /// <see cref="Data.SummonData"/> dela (snapshot no momento da invocação). No-op sem dono/perfil.
        /// Chamado por <see cref="CombatController.SpawnSummon"/> logo após SetOwner e antes das regras de item.
        /// </summary>
        public void DeriveStatsFromOwner()
        {
            if (owner == null || controller == null) return;
            var data = controller.GetCharacterData() as Data.SummonData;
            data?.SummonStatProfile?.Apply(controller.Stats, owner.Stats);
        }

        // === Camada de buffs para as próprias invocações (infra data-driven) ===

        /// <summary>"Minhas invocações causam +X% de dano" (multiplier 1.4 = +40%). Vale p/ vivas e futuras.</summary>
        public void AddSummonDamageDealtMultiplier(string id, float multiplier)
            => AddSummonBuff(SummonBuff.DamageDealt(id, multiplier));

        /// <summary>"Minhas invocações ganham +flat / +percent de um stat". Vale p/ vivas e futuras.</summary>
        public void AddSummonStatBonus(string id, StatType stat, float flat, float percent)
            => AddSummonBuff(SummonBuff.Stat(id, stat, flat, percent));

        /// <summary>Registra um efeito ON-HIT (fonte Artifact) nas invocações do dono — vivas e futuras.
        /// Ex.: Camisa da Antisocial (invocações causam dano físico ao contato). O efeito roda no
        /// OnHitModule de cada invocação, então escala nos stats DELA.</summary>
        public void AddSummonOnHitEffect(string id, SkillEffect effect)
            => AddSummonBuff(SummonBuff.OnHit(id, effect));

        public void RemoveSummonBuff(string id)
        {
            if (!summonBuffs.TryGetValue(id, out SummonBuff buff)) return;
            for (int i = 0; i < summons.Count; i++)
                if (summons[i] != null) buff.RemoveFrom(summons[i]);
            summonBuffs.Remove(id);
        }

        private void AddSummonBuff(SummonBuff buff)
        {
            if (summonBuffs.TryGetValue(buff.Id, out SummonBuff existing))
                for (int i = 0; i < summons.Count; i++)
                    if (summons[i] != null) existing.RemoveFrom(summons[i]);

            summonBuffs[buff.Id] = buff;
            for (int i = 0; i < summons.Count; i++)
                if (summons[i] != null) buff.ApplyTo(summons[i]);
        }

        private void ApplyBuffsTo(UnitController summon)
        {
            foreach (var kv in summonBuffs) kv.Value.ApplyTo(summon);
        }

        private void RemoveBuffsFrom(UnitController summon)
        {
            foreach (var kv in summonBuffs) kv.Value.RemoveFrom(summon);
        }

        /// <summary>Buff concreto aplicado às invocações do dono. Reaplicável por id (vivas e futuras).</summary>
        private readonly struct SummonBuff
        {
            private enum Kind { DamageDealt, Stat, OnHit }

            public readonly string Id;
            private readonly Kind kind;
            private readonly StatType stat;
            private readonly float a; // DamageDealt: multiplicador | Stat: flat
            private readonly float b; // Stat: percent
            private readonly SkillEffect effect; // OnHit: efeito registrado no OnHitModule da invocação

            private SummonBuff(string id, Kind kind, StatType stat, float a, float b, SkillEffect effect)
            {
                Id = id; this.kind = kind; this.stat = stat; this.a = a; this.b = b; this.effect = effect;
            }

            public static SummonBuff DamageDealt(string id, float multiplier)
                => new SummonBuff(id, Kind.DamageDealt, StatType.Attack, multiplier, 0f, null);

            public static SummonBuff Stat(string id, StatType stat, float flat, float percent)
                => new SummonBuff(id, Kind.Stat, stat, flat, percent, null);

            public static SummonBuff OnHit(string id, SkillEffect effect)
                => new SummonBuff(id, Kind.OnHit, StatType.Attack, 0f, 0f, effect);

            public void ApplyTo(UnitController summon)
            {
                StatsModule s = summon.Stats;
                if (s == null) return;
                if (kind == Kind.OnHit) summon.GetModule<OnHitModule>()?.Register(Id, OnHitSource.Artifact, effect);
                else if (kind == Kind.DamageDealt)
                {
                    float mult = a; // copiar p/ local: lambda em struct não pode capturar membro de instância
                    s.AddOutgoingDamageModifier(Id, _ => mult);
                }
                else s.GetStatObject(stat)?.AddModifier(Id, a, b);
            }

            public void RemoveFrom(UnitController summon)
            {
                if (kind == Kind.OnHit) { summon.GetModule<OnHitModule>()?.Unregister(Id); return; }
                StatsModule s = summon.Stats;
                if (s == null) return;
                if (kind == Kind.DamageDealt) s.RemoveOutgoingDamageModifier(Id);
                else s.GetStatObject(stat)?.RemoveModifier(Id);
            }
        }
    }
}
