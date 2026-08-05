using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Loadout da unidade em combate: guarda a build equipada (pontos da árvore + artefato) e a aplica
    /// no INÍCIO do combate. Parte de STAT via <see cref="StatsModule.ApplyBuildStats"/> (preview ==
    /// combate); parte de COMPORTAMENTO do artefato reaproveitando os sistemas existentes:
    /// - <c>onHitEffects</c> → OnHitModule do portador (fonte Artifact);
    /// - <c>summonOnHitEffects</c> → OnHitModule das invocações do portador (via SummonModule);
    /// - <c>onBattleStartEffects</c> → rodados 1× (escudo/energia/redirecionamento).
    /// Removido no retorno ao pool (os submódulos resetam). A build vem do deploy — interim: defaultBuild
    /// do personagem; depois: a build escolhida na tela de picks.
    /// </summary>
    public class LoadoutModule : IUnitModule
    {
        private UnitController controller;
        private CharacterBuild build;
        private ArtifactData artifact;
        private readonly List<UnitController> buffer = new List<UnitController>();
        private bool statsApplied;

        public CharacterBuild Build => build;
        public ArtifactData Artifact => artifact;

        public void Initialize(UnitController unitController) => controller = unitController;
        public void OnEnabled() { }
        public void OnDisabled() { }
        public void Cleanup() { ResetForPool(); controller = null; }

        public void ResetForPool()
        {
            build = null;
            artifact = null;
            statsApplied = false;
        }

        /// <summary>Equipa uma build. O artefato é resolvido no ApplyToCombat (via DataManager).</summary>
        public void Equip(CharacterBuild characterBuild)
        {
            build = characterBuild;
            artifact = null;
        }

        /// <summary>Equipa com o artefato já resolvido (ex.: tela de picks / testes) — não usa DataManager.</summary>
        public void Equip(CharacterBuild characterBuild, ArtifactData resolvedArtifact)
        {
            build = characterBuild;
            artifact = resolvedArtifact;
        }

        /// <summary>
        /// Aplica a build no início do combate. Chamado por <see cref="CombatModule.StartCombat"/> DEPOIS
        /// de allies/enemies setados e do PrepareForCombat (que zera energia). Dois ciclos de vida:
        /// - STAT + vida cheia + registro de on-hit/invocação: UMA vez por vida da unidade (reset no pool),
        ///   para não re-curar a vida a cada rodada nem re-somar stats;
        /// - efeitos de início de combate (escudo/energia/redirecionamento): a CADA rodada (renovam).
        /// </summary>
        public void ApplyToCombat()
        {
            if (controller == null) return;

            if (artifact == null && build != null && !string.IsNullOrEmpty(build.artifactId))
                artifact = DataManager.GetArtifact(build.artifactId);

            if (!statsApplied)
            {
                statsApplied = true;

                // Parte de STAT: pontos da árvore + grants do artefato → modifiers (mesma matemática do preview).
                StatsModule stats = controller.Stats;
                stats?.ApplyBuildStats(build, DataManager.GetSharedStatTree(), artifact, controller.Rank);
                stats?.GetResourceObject(ResourceType.Health)?.SetToMax(); // nasce em combate com vida cheia (já com a build)

                if (artifact != null)
                {
                    // On-hit do portador (fonte Artifact).
                    OnHitModule reg = controller.GetModule<OnHitModule>();
                    if (reg != null && artifact.onHitEffects != null)
                        for (int i = 0; i < artifact.onHitEffects.Length; i++)
                            if (artifact.onHitEffects[i] != null)
                                reg.Register("artifact_onhit_" + i, OnHitSource.Artifact, artifact.onHitEffects[i]);

                    // On-hit das invocações do portador (Camisa) — vivas e futuras.
                    SummonModule summon = controller.GetModule<SummonModule>();
                    if (summon != null && artifact.summonOnHitEffects != null)
                        for (int i = 0; i < artifact.summonOnHitEffects.Length; i++)
                            if (artifact.summonOnHitEffects[i] != null)
                                summon.AddSummonOnHitEffect("artifact_summononhit_" + i, artifact.summonOnHitEffects[i]);
                }
            }

            // Início de combate (renova a cada rodada): escudo, energia, redirecionamento.
            if (artifact != null && artifact.onBattleStartEffects != null && artifact.onBattleStartEffects.Length > 0)
                RunBattleStart(artifact.onBattleStartEffects);
        }

        private void RunBattleStart(SkillEffect[] effects)
        {
            CombatModule combat = controller.GetModule<CombatModule>();
            var ctx = new SkillContext
            {
                owner = controller,
                ownerStats = controller.Stats,
                ownerVisual = controller.Visual,
                skills = controller.GetModule<SkillsModule>(),
                enemies = combat != null ? combat.Targets : null,
                allies = combat != null ? combat.Allies : null,
                allowCrit = true
            };

            var state = new EffectRunState();
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffect fx = effects[i];
                if (fx == null) continue;
                buffer.Clear();
                fx.ResolveTargets(ctx, buffer);
                fx.Apply(ctx, buffer, ref state);
                state.previousTargetCount = buffer.Count;
            }
        }
    }
}
