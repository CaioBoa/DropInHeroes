using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{
    /// <summary>
    /// Liga o <see cref="StatusModule"/> à pilha world-space (<see cref="StatusStrip"/>) acima da
    /// barra de vida. Reconstrói os ícones (sem números) sempre que os efeitos mudam.
    /// </summary>
    public class StatusStripModule : IUnitModule
    {
        private UnitController controller;
        private StatusModule status;
        private StatsModule stats;
        private StatusStrip strip;

        public void Initialize(UnitController unitController)
        {
            controller = unitController;
            status = controller.GetModule<StatusModule>();
            stats = controller.GetModule<StatsModule>();
            strip = controller.GetComponentInChildren<StatusStrip>(true);

            if (status != null) status.OnEffectsChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (strip == null) return;
            strip.SetEffects(status != null ? status.ActiveEffects : null,
                             stats != null ? stats.Catalog : null);
        }

        public void OnEnabled() => Refresh();

        public void OnDisabled() { }

        public void Cleanup()
        {
            if (status != null) status.OnEffectsChanged -= Refresh;
            status = null;
            stats = null;
            strip = null;
            controller = null;
        }
    }
}
