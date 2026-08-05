using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Estado acumulado ao longo da cadeia de efeitos de uma skill (passado por referência entre
    /// os efeitos). Permite que um efeito reaja ao anterior — ex.: curar proporcional ao número de
    /// inimigos atingidos pelo dano que veio antes na lista.
    /// </summary>
    public struct EffectRunState
    {
        /// <summary>Quantos alvos o efeito IMEDIATAMENTE anterior da cadeia resolveu.</summary>
        public int previousTargetCount;

        /// <summary>Cache de acerto/esquiva do ATAQUE atual, por alvo (true = acertou). null = ataque
        /// sem esquiva (onCast, always-hit). Preenchido sob demanda por <see cref="HitChanceResolver"/>
        /// e compartilhado por todos os efeitos on-hit — esquivar tira o alvo do ataque inteiro.</summary>
        public Dictionary<UnitController, bool> dodgeRolls;
    }

    /// <summary>
    /// Bloco de efeito reutilizável e data-driven. Cada subclasse faz UMA coisa (dano, cura, status,
    /// vfx, salto...) sobre o conjunto de alvos resolvido pela sua própria <see cref="TargetQuery"/>.
    /// Os artefatos compõem uma lista destes (ver ArtifactData) em vez de
    /// escrever lógica imperativa. Self-describing: <see cref="description"/> é um template lido pelo
    /// tooltip refletindo os próprios campos.
    /// Stateless quanto à unidade — a mesma instância (sub-asset) é compartilhada entre todas as
    /// unidades; só pode cachear dados imutáveis derivados dos campos serializados.
    /// </summary>
    public abstract class SkillEffect : ScriptableObject
    {
        [TextArea(1, 3)] public string description;

        [Tooltip("Quem este efeito atinge. Padrão (CurrentTarget) = o alvo atual da skill.")]
        [SerializeField] private TargetQuery target = new TargetQuery { side = TargetSide.CurrentTarget };

        /// <summary>Resolve os alvos deste efeito no buffer fornecido pelo chamador (sem alocar).</summary>
        public void ResolveTargets(in SkillContext context, List<UnitController> buffer)
            => target.Resolve(TargetContext.FromSkill(context), buffer);

        /// <summary>Aplica o efeito sobre os alvos já resolvidos. <paramref name="state"/> carrega o
        /// resultado do efeito anterior (ex.: nº de alvos atingidos).</summary>
        public abstract void Apply(in SkillContext context, List<UnitController> targets, ref EffectRunState state);
    }
}
