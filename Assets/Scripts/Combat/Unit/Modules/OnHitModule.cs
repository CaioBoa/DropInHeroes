using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>Origem de um efeito on-hit — sempre explícita (log de combate; regras futuras por fonte).</summary>
    public enum OnHitSource { Attack, Passive, Artifact }

    /// <summary>
    /// Registro de efeitos ON-HIT da unidade — mecânica PRÓPRIA do jogo, separada do dano intrínseco do
    /// ataque. As fontes (skill de ataque, passiva, artefatos futuros) são consultadas no init de combate
    /// e REGISTRAM aqui seus efeitos (como a inicialização de passivas); a cada ataque básico, o golpe
    /// causa o dano intrínseco e depois TODOS os efeitos registrados aplicam, compartilhando a esquiva
    /// do golpe. Riders não critam e não contam como "ataque recebido" (<see cref="SkillContext.isOnHit"/>).
    /// <see cref="ApplyTimes"/> abre espaço p/ efeitos futuros como "on-hit aplica 2×".
    /// </summary>
    public class OnHitModule : IUnitModule
    {
        private readonly struct Entry
        {
            public readonly string id;
            public readonly OnHitSource source;
            public readonly SkillEffect effect;

            public Entry(string id, OnHitSource source, SkillEffect effect)
            {
                this.id = id;
                this.source = source;
                this.effect = effect;
            }
        }

        /// <summary>Efeito on-hit em CÓDIGO (composição de módulos), registrado por passivas/skills modulares.</summary>
        public delegate void OnHitEffect(SkillContext context, ref EffectRunState state);

        private readonly struct CodeEntry
        {
            public readonly string id;
            public readonly OnHitSource source;
            public readonly OnHitEffect fn;

            public CodeEntry(string id, OnHitSource source, OnHitEffect fn)
            {
                this.id = id;
                this.source = source;
                this.fn = fn;
            }
        }

        private UnitController controller;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<CodeEntry> codeEntries = new List<CodeEntry>();
        private readonly List<UnitController> buffer = new List<UnitController>();

        /// <summary>Quantas vezes cada efeito on-hit aplica por golpe (1 = normal; passivas futuras podem elevar).</summary>
        public int ApplyTimes { get; set; } = 1;

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
            entries.Clear();
            codeEntries.Clear();
            ApplyTimes = 1;
        }

        /// <summary>Registra (ou substitui, pelo id) um efeito on-hit de uma fonte (SkillEffect-SO legado).</summary>
        public void Register(string id, OnHitSource source, SkillEffect effect)
        {
            if (string.IsNullOrEmpty(id) || effect == null) return;
            Unregister(id);
            entries.Add(new Entry(id, source, effect));
        }

        /// <summary>Registra (ou substitui, pelo id) um efeito on-hit em CÓDIGO — usado por passivas modulares.</summary>
        public void RegisterCode(string id, OnHitSource source, OnHitEffect fn)
        {
            if (string.IsNullOrEmpty(id) || fn == null) return;
            Unregister(id);
            codeEntries.Add(new CodeEntry(id, source, fn));
        }

        public void Unregister(string id)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].id == id) entries.RemoveAt(i);
            for (int i = codeEntries.Count - 1; i >= 0; i--)
                if (codeEntries[i].id == id) codeEntries.RemoveAt(i);
        }

        /// <summary>
        /// Um "proc" de on-hit: aplica todos os efeitos registrados. Chamado pelo ataque básico após o
        /// dano intrínseco, com o MESMO cache de esquiva do golpe (esquivar anula o golpe inteiro).
        /// </summary>
        public void ApplyAll(SkillContext context, ref EffectRunState state)
        {
            if (entries.Count == 0 && codeEntries.Count == 0) return;

            context.allowCrit = false; // riders não critam por padrão (equipamento futuro libera)
            context.isOnHit = true;

            for (int t = 0; t < ApplyTimes; t++)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry entry = entries[i];
                    buffer.Clear();
                    entry.effect.ResolveTargets(context, buffer);
                    HitChanceResolver.FilterDodged(context, buffer, state.dodgeRolls);
                    entry.effect.Apply(context, buffer, ref state);
                    state.previousTargetCount = buffer.Count;

                    if (DebugManager.IsEnabled(DebugCategory.Combat))
                        DebugManager.Log($"OnHit [{entry.source}] {entry.effect.name} → {buffer.Count} alvo(s)", DebugCategory.Combat);
                }

                // Efeitos on-hit em CÓDIGO (passivas modulares): resolvem alvos e aplicam módulos internamente.
                for (int i = 0; i < codeEntries.Count; i++)
                    codeEntries[i].fn(context, ref state);
            }
        }
    }
}
