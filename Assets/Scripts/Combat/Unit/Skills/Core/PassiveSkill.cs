using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public abstract class PassiveSkill : ScriptableObject
    {
        [Header("Skill Info")]
        public string displayName;
        [TextArea(2, 5)] public string description;
        [Tooltip("Ícone exibido no painel CharacterInfo. Null = placeholder (quadrado branco).")]
        public Sprite icon;

        /// <summary>
        /// Efeitos ON-HIT nativos que a passiva contribui — registrados no <see cref="OnHitModule"/>
        /// pelo SkillsModule no init de combate e aplicados a cada ataque básico do dono. Null = nenhum.
        /// Grupo identificável (fonte Passive) — base para regras futuras tipo "+X% dano de passiva".
        /// </summary>
        public virtual IReadOnlyList<SkillEffect> OnHitEffects => null;

        // === Ciclo de vida (template): mapeia os 4 momentos para OnApply/OnRemove/OnReset.
        // NÃO sobrescreva estes na passiva concreta — sobrescreva OnApply/OnRemove/OnReset.

        /// <summary>Início do combate: reseta estado e aplica os efeitos persistentes (nascer).</summary>
        public virtual void Initialize(SkillContext context) { OnReset(); OnApply(context); }

        /// <summary>Revive / remoção de selo: reaplica os efeitos (mantém contadores).</summary>
        public virtual void Reactivate(SkillContext context) => OnApply(context);

        /// <summary>Morte / selamento: remove os efeitos, preservando contadores (morrer).</summary>
        public virtual void Deactivate(SkillContext context) => OnRemove();

        /// <summary>Fim do combate: remove os efeitos e reseta ao estado base.</summary>
        public virtual void Clear() { OnRemove(); OnReset(); }

        // === Pontos de extensão — sobrescreva estes.

        /// <summary>Aplica os efeitos persistentes da passiva. Chamado ao NASCER e ao REVIVER.</summary>
        protected virtual void OnApply(SkillContext context) { }

        /// <summary>Remove os efeitos persistentes. Chamado ao MORRER e no FIM de combate.</summary>
        protected virtual void OnRemove() { }

        /// <summary>Reseta contadores/estado ao base (só no início e fim de combate). Passivas sem estado não precisam.</summary>
        protected virtual void OnReset() { }
    }
}
