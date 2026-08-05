using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Core;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Tower
{

    /// <summary>
    /// Contagem regressiva de fase (preparação/batalha) que atualiza o HUD. Cada Run cancela a
    /// contagem anterior, evitando vazar um CancellationTokenSource por fase ao longo da run.
    /// </summary>
    public class PhaseTimer
    {
        private readonly TowerRunHUD hud;
        private CancellationTokenSource cts;

        public PhaseTimer(TowerRunHUD hud)
        {
            this.hud = hud;
        }

        /// <summary>Inicia a contagem (cancela a anterior). A Task completa ao esgotar o tempo.</summary>
        public Task Run(float seconds)
        {
            Cancel();
            cts = new CancellationTokenSource();
            return RunVisual(seconds, cts.Token);
        }

        public void Cancel()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private async Task RunVisual(float seconds, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (token.IsCancellationRequested) return;
                elapsed += Time.deltaTime;
                if (hud != null) hud.SetTimer(Mathf.Max(0f, seconds - elapsed));
                await Task.Yield();
            }
        }
    }
}
