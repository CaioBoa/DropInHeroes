using System;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Passiva que contribui EFEITOS ON-HIT (aplicados a cada golpe básico do dono, junto com os on-hit
    /// das demais fontes, compartilhando a esquiva; riders não critam). Registra um callback de código no
    /// <see cref="OnHitModule"/> ao nascer/reviver e desregistra ao morrer/fim. A subclasse compõe os
    /// módulos em <see cref="OnHitStrike"/>. Ex.: passiva do Hami (dano físico bônus + ganho de Ataque).
    /// </summary>
    public abstract class ModularOnHitPassive : ModularPassive
    {
        private OnHitModule registry;
        private string onHitId;

        protected override void Compose(SkillContext context)
        {
            registry = context.owner.GetModule<OnHitModule>();
            if (registry == null) return;
            if (string.IsNullOrEmpty(onHitId)) onHitId = "onhit_passive_" + Guid.NewGuid().ToString("N");
            registry.RegisterCode(onHitId, OnHitSource.Passive, OnHitStrike);
        }

        protected override void Decompose()
        {
            if (registry != null && !string.IsNullOrEmpty(onHitId)) registry.Unregister(onHitId);
        }

        /// <summary>Efeitos aplicados a cada golpe básico do dono (composição de módulos). Não critam.</summary>
        protected abstract void OnHitStrike(SkillContext context, ref EffectRunState state);
    }
}
