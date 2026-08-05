using System;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Base das passivas escritas como COMPOSIÇÃO DE MÓDULOS: a subclasse só declara os parâmetros
    /// numéricos (por rank) e implementa <see cref="Compose"/> chamando <c>SkillModules.*</c> sobre
    /// alvos resolvidos por <c>Targets.*</c>. Os efeitos persistentes registrados via <see cref="Persistent"/>
    /// são revertidos automaticamente ao morrer / no fim de combate — a subclasse não gerencia remoção.
    /// Idempotente no revive: reverte antes de recompor.
    /// </summary>
    public abstract class ModularPassive : PassiveSkill
    {
        /// <summary>Efeitos persistentes desta ativação — revertidos sozinhos no OnRemove.</summary>
        protected readonly PersistentEffects Persistent = new PersistentEffects();

        protected override void OnApply(SkillContext context)
        {
            Persistent.RevertAll(); // idempotente: revive não duplica
            if (context.owner != null) Compose(context);
        }

        protected override void OnRemove()
        {
            Persistent.RevertAll();
            Decompose();
        }

        /// <summary>Compõe os módulos da passiva. Chamado ao NASCER e ao REVIVER.</summary>
        protected abstract void Compose(SkillContext context);

        /// <summary>Limpeza além dos efeitos persistentes (ex.: desregistrar on-hit). Chamado ao MORRER e no fim.</summary>
        protected virtual void Decompose() { }
    }

    /// <summary>
    /// Coleção de reversões de efeitos persistentes aplicados por uma passiva/skill. Cada módulo que
    /// registra um modificador reversível anexa aqui a ação de removê-lo; <see cref="RevertAll"/> desfaz
    /// tudo (ordem inversa) e limpa. Um por clone de passiva (estado de runtime).
    /// </summary>
    public sealed class PersistentEffects
    {
        private readonly List<Action> reverts = new List<Action>();

        public void Add(Action revert)
        {
            if (revert != null) reverts.Add(revert);
        }

        public void RevertAll()
        {
            for (int i = reverts.Count - 1; i >= 0; i--) reverts[i]?.Invoke();
            reverts.Clear();
        }
    }
}
