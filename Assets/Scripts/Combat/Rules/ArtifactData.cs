using System;
using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Artefato equipável de uma build. Aplicado à unidade ESPECÍFICA que o equipa, em combate, pelo
    /// <see cref="LoadoutModule"/> (não mais por regra global de tag). Composto data-driven: grants de
    /// stat (entram no preview + combate, mesma matemática via <see cref="StatBudget.PointsToValue"/>) +
    /// listas de <see cref="SkillEffect"/> por gatilho (on-hit / início de combate / on-hit das invocações
    /// do portador). Reaproveita os sistemas existentes (OnHitModule, ShieldModule, DamageShare, SummonModule).
    /// </summary>
    [CreateAssetMenu(fileName = "NewArtifact", menuName = "Game/Items/Artifact")]
    public class ArtifactData : CombatItem, IGameData, ISkillEffectSource
    {
        [Header("Metadata (Governance)")]
        [SerializeField] private string id;
        [SerializeField] private DataCategory category = DataCategory.Artifact;

        [Header("Apresentação")]
        public Sprite icon;

        [Header("Efeitos de stat (entram no preview + combate)")]
        [Tooltip("Modificadores de stat aplicados ao portador. flat em PONTOS (× statPointValue); percent: 0.30 = +30%.")]
        public StatGrant[] statGrants = Array.Empty<StatGrant>();

        [Header("Comportamentos (aplicados ao portador em combate)")]
        [Tooltip("Efeitos ON-HIT do portador (registrados no OnHitModule, fonte Artifact). Ex.: +dano mágico ao contato, +energia ao contato.")]
        public SkillEffect[] onHitEffects = Array.Empty<SkillEffect>();
        [Tooltip("Efeitos rodados UMA VEZ no início do combate (cada um mira via sua TargetQuery). Ex.: escudo + energia aos aliados; redirecionamento de dano.")]
        public SkillEffect[] onBattleStartEffects = Array.Empty<SkillEffect>();
        [Tooltip("Efeitos ON-HIT registrados nas INVOCAÇÕES do portador (vivas e futuras). Ex.: invocações causam dano ao contato.")]
        public SkillEffect[] summonOnHitEffects = Array.Empty<SkillEffect>();

        public string ID => id;
        public DataCategory Category => category;

        // Descrição no tooltip (mesma pipeline das skills): os efeitos por gatilho, na ordem em que o
        // ArtifactTextBuilder os lista. Os grants de stat entram como cabeçalho (não são SkillEffects).
        public IEnumerable<SkillEffect> DescribableEffects
        {
            get
            {
                if (onHitEffects != null) foreach (var e in onHitEffects) if (e != null) yield return e;
                if (summonOnHitEffects != null) foreach (var e in summonOnHitEffects) if (e != null) yield return e;
                if (onBattleStartEffects != null) foreach (var e in onBattleStartEffects) if (e != null) yield return e;
            }
        }

        // Artefatos NÃO usam mais o sistema de regras globais por tag — são aplicados à unidade
        // específica que os equipa, pelo LoadoutModule. Mantido vazio pela base CombatItem.
        public override IEnumerable<CombatRule> CreateRules() { yield break; }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = name.ToLower().Replace(" ", "_");
        }
    #endif
    }

    /// <summary>Contribuição de stat de um artefato: pontos flat (× statPointValue) na base + percent no fold (1+Σpercent).</summary>
    [Serializable]
    public struct StatGrant
    {
        public StatType stat;
        [Tooltip("Pontos flat concedidos (× statPointValue → valor bruto). Coeso com base e árvore.")]
        public float points;
        [Tooltip("0.30 = +30%.")]
        public float percent;
    }
}
