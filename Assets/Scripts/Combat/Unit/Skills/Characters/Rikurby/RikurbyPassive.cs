using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva do Rikurby (composição de módulos, base <see cref="ModularPassive"/>):
    /// - A cada N ataques RECEBIDOS (N por rank: 10/8/6), invoca um Little Rick (cap de vivos por vez).
    /// - Sempre que o Rikurby recebe cura, cada invocação viva é curada por uma % dessa cura (por rank: 10/20/30).
    /// Reativa por hooks (<c>onAttackReceived</c>/<c>onHealReceived</c>), desassinados via <see cref="ModularPassive.Persistent"/>
    /// no OnRemove. O contador e a partilha de cura são estado próprio (não há primitiva de módulo p/ o gatilho).
    /// Os stats do Little Rick derivam do Rikurby via <see cref="SummonStatProfile"/> no spawn — a passiva não mexe em stats.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Skills/Rikurby/Passive")]
    public class RikurbyPassive : ModularPassive
    {
        [Header("Invocação por ataques recebidos")]
        [Tooltip("Ataques recebidos para invocar 1 Little Rick, por rank (índice 0 = rank 1). Ex.: 10/8/6.")]
        [SerializeField] private int[] attacksPerSummonByRank = { 10, 8, 6 };
        [Tooltip("Máximo de Little Ricks vivos ao mesmo tempo.")]
        [SerializeField] private int maxLiveSummons = 8;
        [Tooltip("SummonData do Little Rick invocado.")]
        [SerializeField] private SummonData summonData;
        [Tooltip("Dispersão aleatória da posição de spawn ao redor do Rikurby.")]
        [SerializeField] private float spawnSpread = 1f;

        [Header("Cura compartilhada")]
        [Tooltip("Ao receber cura, cada Little Rick vivo cura esta % da cura recebida, por rank. Ex.: 10/20/30.")]
        [SerializeField] private float[] healSharePercentByRank = { 10f, 20f, 30f };

        // Estado de runtime (clone por unidade).
        private UnitController owner;
        private SkillContext ctx;
        private int attackCounter;

        protected override void OnReset() => attackCounter = 0;

        protected override void Compose(SkillContext context)
        {
            owner = context.owner;
            ctx = context;
            PassiveHooks hooks = context.skills?.Hooks;
            if (hooks == null) return;

            hooks.onAttackReceived += OnAttackReceived;
            hooks.onHealReceived += OnHealReceived;
            Persistent.Add(() =>
            {
                hooks.onAttackReceived -= OnAttackReceived;
                hooks.onHealReceived -= OnHealReceived;
            });
        }

        private void OnAttackReceived(UnitController attacker)
        {
            if (owner == null || summonData == null) return;

            int threshold = RankValue(attacksPerSummonByRank);
            if (threshold <= 0) return;

            attackCounter++;
            while (attackCounter >= threshold)
            {
                attackCounter -= threshold;
                TrySpawnSummon();
            }
        }

        private void TrySpawnSummon()
        {
            if (owner == null || summonData == null) return;
            if (CountLiveSummons() >= maxLiveSummons) return;

            Vector3 pos = owner.transform.position
                + new Vector3(Random.Range(-spawnSpread, spawnSpread), Random.Range(-spawnSpread, spawnSpread), 0f);

            // Stats derivados via SummonStatProfile no SpawnSummon; grafo dono↔invocação via SetOwner.
            SkillModules.SpawnSummon(ctx, summonData, pos);
        }

        private int CountLiveSummons()
        {
            var summons = owner.Summons;
            if (summons == null) return 0;
            int count = 0;
            for (int i = 0; i < summons.Count; i++)
            {
                var s = summons[i];
                var st = s != null ? s.Stats : null;
                if (st != null && !st.IsDead) count++;
            }
            return count;
        }

        private void OnHealReceived(float amount)
        {
            if (owner == null || amount <= 0f) return;

            var summons = owner.Summons;
            if (summons == null) return;

            float share = amount * (RankValue(healSharePercentByRank) * 0.01f);
            if (share <= 0f) return;

            // Partilha = cura CAUSADA pelo Rikurby às invocações: passa pelos módulos de cura
            // (HealBonus do Rikurby na causa, HealReceived da invocação no recebimento).
            float caused = owner.Stats != null ? owner.Stats.CauseHeal(share) : share;
            if (caused <= 0f) return;

            for (int i = 0; i < summons.Count; i++)
            {
                var s = summons[i];
                var st = s != null ? s.Stats : null;
                if (st == null || st.IsDead) continue;
                st.ReceiveHeal(caused);
            }
        }

        private int RankValue(int[] tiersPerRank)
        {
            int idx = RankTiers.IndexFor(owner != null ? owner.Rank : 1, tiersPerRank != null ? tiersPerRank.Length : 0);
            return idx >= 0 ? tiersPerRank[idx] : 0;
        }

        private float RankValue(float[] tiersPerRank) => RankTiers.ValueFor(tiersPerRank, owner != null ? owner.Rank : 1);
    }
}
