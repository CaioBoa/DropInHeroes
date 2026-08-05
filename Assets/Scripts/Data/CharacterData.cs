using UnityEngine;
using System.Collections.Generic;
using DropInHeroes.Combat;
using DropInHeroes.Utils;

namespace DropInHeroes.Data
{

    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Data/Character")]
    public class CharacterData : ScriptableObject, IGameData
    {
        [Header("Metadata (Governance)")]
        [SerializeField] private string id;
        [SerializeField] private DataCategory category = DataCategory.Character;

        [Header("Visual")]
        public AnimationClip idleAnimation;
        public AnimationClip runAnimation;
        public AnimationClip dragAnimation;
        public AnimationClip attackAnimation;
        public AnimationClip deathAnimation;
        public AnimationClip deathIdleAnimation;
        public AnimationClip supremeAnimation;
        public AnimationClip stunAnimation;
        public AnimationClip victoryAnimation;

        [Tooltip("Animações extras AVULSAS (one-shot) que as HABILIDADES podem pedir por chave — ex.: " +
                 "uma pose de conjuração de uma passiva ativável. Use GetExtraAnimation(chave) + " +
                 "VisualModule.PlayExtraAnimation. Deixe vazio se a unidade não precisa.")]
        public List<NamedAnimation> extraAnimations = new List<NamedAnimation>();

        [Tooltip("Perfis de animação alternativos (TRANSFORMAÇÕES): cada um substitui o CONJUNTO de clipes " +
                 "(idle/run/attack/…) enquanto ativo. Clipe vazio no perfil cai no base. Troque com " +
                 "VisualModule.SetAnimationProfile(chave) / ResetAnimationProfile().")]
        public List<AnimationProfile> animationProfiles = new List<AnimationProfile>();

        [Tooltip("FORMAS alternativas (stances): cada uma troca o ataque/supremo ATIVOS e o perfil de animação " +
                 "enquanto ativa. Entrada por SkillsModule.SetForm(chave); a lógica de QUANDO trocar mora na " +
                 "passiva (ex.: a passiva da Vela transforma ao acumular 20 críticos). Skill nula = mantém a base.")]
        public List<CharacterForm> forms = new List<CharacterForm>();

        [Tooltip("Escala visual do personagem (1 = tamanho importado). Ajusta o tamanho na tela sem alterar animações; não afeta footprint, colisão ou barras.")]
        public float visualScale = 1f;

        [Tooltip("Ajuste de escala/ancoragem POR ANIMAÇÃO (opcional). Cada entrada casa por CLIPE (base, perfil ou extra): enquanto esse clipe toca, o visual usa visualScale × scale e desloca os pés por offsetY. Sem entrada = visualScale, offset 0. Autorar em Tools ▸ DropInHeroes ▸ Character Visual Config.")]
        public List<AnimClipTuning> animClipTunings = new List<AnimClipTuning>();

        [Header("UI em combate (world-space, por personagem)")]
        [Tooltip("Altura (Y em unidades de mundo, relativa à raiz) da barra de vida/energia e do status, ACIMA do personagem. Ajuste por personagem — models maiores precisam de valor maior. Parametrizável e independente do visualScale.")]
        public float overheadHeight = 1.14f;
        [Tooltip("Escala do anel de seleção (raio). 0.9 = tamanho do prefab. O anel fica nos pés e renderiza atrás do personagem.")]
        public float selectionRingScale = 0.9f;
        [Tooltip("Altura (Y) do anel de seleção no chão, relativa à raiz. Mais negativo = mais para os pés.")]
        public float selectionRingOffsetY = -0.45f;

        [Header("Character Type")]
        public CharacterType primaryType = CharacterType.Classic;
        public CharacterType secondaryType = CharacterType.None;

        [Header("Portraits & Name")]
        public Sprite defaultSprite;
        public Sprite cardPortrait;
        [Tooltip("Retrato grande 2x1 (vertical) exibido no Overview da tela de Personagens. Null = usa defaultSprite/cardPortrait.")]
        public Sprite portrait2x1;
        public string displayName;

        [Header("Lore")]
        [TextArea(4, 12)] public string lore;

        [Header("Skills")]
        public BaseAttackSkill baseSkill;
        public SupremeSkill supremeSkill;
        public PassiveSkill passiveSkill;

        [Header("Default Build")]
        [Tooltip("O ARTEFATO (id) da build padrão é editado aqui; os NÓS da árvore são autorados por Tools ▸ DropInHeroes ▸ Stat Tree Editor (modo Build), clicando nos nós com as mesmas regras do jogo. As 3 builds custom do jogador vivem no BuildStore, fora do asset.")]
        public CharacterBuild defaultBuild = new CharacterBuild();

        [Header("Comportamento / Categorias")]
        [Tooltip("Categorias da unidade. Usadas por regras de combate e modificadores condicionais (ex.: dano DE/PARA invocações).")]
        public UnitTag tags = UnitTag.Hero;
        [Tooltip("O que a unidade pode fazer em combate. Heróis = All. Invocações podem ter funções reduzidas (ex.: só Move|Attack, sem energia/supremo).")]
        public UnitCapability capabilities = UnitCapability.All;

        [Header("Invocações")]
        [Tooltip("Máximo de invocações desta unidade em campo ao mesmo tempo. Usado para dimensionar o pool.")]
        public int maxSummons = 0;

        [Header("Base Stats (pontos de orçamento)")]
        [Tooltip("Pontos de stat alocados a esta unidade. O valor BRUTO = GeneralBaseStats + pontos × statPointValue (ver StatBudget). Aloque só os stats em que a unidade investe; o restante vem do piso geral. MaxEnergy é EXCLUÍDO — é o custo do supremo, definido na supreme skill (energyCost).")]
        [SerializeField] private List<StatPointEntry> statPoints = new List<StatPointEntry>();

        public string ID => id;
        public DataCategory Category => category;

        /// <summary>Pontos alocados a um stat (0 se não alocado). O valor bruto é composto por <see cref="StatBudget"/>.</summary>
        public float GetStatPoints(StatType type)
        {
            for (int i = 0; i < statPoints.Count; i++)
                if (statPoints[i].type == type) return statPoints[i].points;
            return 0f;
        }

        /// <summary>Clip de uma animação extra pela sua chave (null se a unidade não a define). Usado pelas skills que pedem uma animação própria.</summary>
        public AnimationClip GetExtraAnimation(string key)
        {
            if (string.IsNullOrEmpty(key) || extraAnimations == null) return null;
            for (int i = 0; i < extraAnimations.Count; i++)
                if (extraAnimations[i].key == key) return extraAnimations[i].clip;
            return null;
        }

        /// <summary>Perfil de animação (transformação) pela chave, ou null se não existir.</summary>
        public AnimationProfile GetAnimationProfile(string key)
        {
            if (string.IsNullOrEmpty(key) || animationProfiles == null) return null;
            for (int i = 0; i < animationProfiles.Count; i++)
                if (animationProfiles[i] != null && animationProfiles[i].key == key) return animationProfiles[i];
            return null;
        }

        /// <summary>Forma (stance) pela chave, ou null se não existir.</summary>
        public CharacterForm GetForm(string key)
        {
            if (string.IsNullOrEmpty(key) || forms == null) return null;
            for (int i = 0; i < forms.Count; i++)
                if (forms[i] != null && forms[i].key == key) return forms[i];
            return null;
        }

        /// <summary>Ajuste por clipe. Retorna false (tuning=default) se o clipe não tiver entrada.</summary>
        public bool TryGetClipTuning(AnimationClip clip, out AnimClipTuning tuning)
        {
            tuning = default;
            if (clip == null || animClipTunings == null) return false;
            for (int i = 0; i < animClipTunings.Count; i++)
                if (animClipTunings[i].clip == clip) { tuning = animClipTunings[i]; return true; }
            return false;
        }

    #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = name.ToLower().Replace(" ", "_");
            }

            // MaxEnergy vem da supreme skill (energyCost), não do orçamento de pontos — mantém a lista limpa.
            if (statPoints != null)
                statPoints.RemoveAll(e => e.type == StatType.MaxEnergy);
        }
    #endif
    }

    /// <summary>Pontos de orçamento alocados a um stat de uma unidade. Bruto = piso geral + pontos × statPointValue.</summary>
    [System.Serializable]
    public struct StatPointEntry
    {
        public StatType type;
        public float points;
    }

    /// <summary>
    /// Ajuste visual de UM clipe: enquanto ele toca, o nó Visual usa <c>visualScale × scale</c> e
    /// desloca os pés por <c>offsetY</c>. Chaveado pelo clipe (base/perfil/extra), aplicado por
    /// <c>VisualModule.Tick</c> em combate. Opcionalmente a escala/offset variam no TEMPO do clipe
    /// (curvas em tempo normalizado 0..1) — para corrigir arte cujo tamanho muda durante a animação.
    /// </summary>
    [System.Serializable]
    public struct AnimClipTuning
    {
        public AnimationClip clip;
        [Tooltip("Escala X constante (1 = igual; 0 = não definido → 1). Corrige largura de arte espremida/alongada do Ludo.")]
        public float scaleX;
        [Tooltip("Escala Y constante (1 = igual; 0 = não definido → 1).")]
        public float scaleY;
        [Tooltip("Deslocamento X constante (mundo).")]
        public float offsetX;
        [Tooltip("Deslocamento Y constante (mundo) — alinhar os pés.")]
        public float offsetY;
        [Tooltip("Opcional: escala X ao longo do tempo NORMALIZADO (0..1). Com ≥2 chaves, substitui scaleX.")]
        public AnimationCurve scaleXCurve;
        [Tooltip("Opcional: escala Y ao longo do tempo NORMALIZADO (0..1). Com ≥2 chaves, substitui scaleY.")]
        public AnimationCurve scaleYCurve;
        [Tooltip("Opcional: offsetX ao longo do tempo NORMALIZADO (0..1). Com ≥2 chaves, substitui offsetX.")]
        public AnimationCurve offsetXCurve;
        [Tooltip("Opcional: offsetY ao longo do tempo NORMALIZADO (0..1). Com ≥2 chaves, substitui offsetY.")]
        public AnimationCurve offsetYCurve;

        private static bool Has(AnimationCurve c) => c != null && c.length >= 2;
        private static float Norm(float v) => v <= 0f ? 1f : v;   // 0 = não definido → 1
        public bool IsTimeVarying => Has(scaleXCurve) || Has(scaleYCurve) || Has(offsetXCurve) || Has(offsetYCurve);
        public float EvalScaleX(float nt) => Has(scaleXCurve) ? scaleXCurve.Evaluate(nt) : Norm(scaleX);
        public float EvalScaleY(float nt) => Has(scaleYCurve) ? scaleYCurve.Evaluate(nt) : Norm(scaleY);
        public float EvalOffsetX(float nt) => Has(offsetXCurve) ? offsetXCurve.Evaluate(nt) : offsetX;
        public float EvalOffsetY(float nt) => Has(offsetYCurve) ? offsetYCurve.Evaluate(nt) : offsetY;
    }

    /// <summary>Animação extra da unidade, identificada por uma chave estável que as skills referenciam.</summary>
    [System.Serializable]
    public struct NamedAnimation
    {
        [Tooltip("Chave estável referenciada pela skill (ex.: \"cast_especial\"). Não depende da ordem na lista.")]
        public string key;
        public AnimationClip clip;
    }

    /// <summary>
    /// Conjunto alternativo de clipes (uma TRANSFORMAÇÃO), identificado por chave. Substitui o conjunto
    /// base enquanto ativo; qualquer clipe deixado nulo cai no clipe base do CharacterData.
    /// </summary>
    [System.Serializable]
    public class AnimationProfile
    {
        [Tooltip("Chave estável da transformação (ex.: \"lobo\"). Referenciada por SetAnimationProfile.")]
        public string key;
        public AnimationClip idleAnimation;
        public AnimationClip runAnimation;
        public AnimationClip dragAnimation;
        public AnimationClip attackAnimation;
        public AnimationClip deathAnimation;
        public AnimationClip deathIdleAnimation;
        public AnimationClip supremeAnimation;
        public AnimationClip stunAnimation;
        public AnimationClip victoryAnimation;
    }

    /// <summary>
    /// Uma FORMA (stance) alternativa: enquanto ativa, troca o ataque/supremo da unidade e o perfil de
    /// animação. Ativada por <c>SkillsModule.SetForm(chave)</c> — a decisão de QUANDO ativar é da passiva.
    /// Skill nula = mantém a skill base do <see cref="CharacterData"/> (permite formas que só mudam a arte).
    /// </summary>
    [System.Serializable]
    public class CharacterForm
    {
        [Tooltip("Chave estável da forma (ex.: \"jaguarete\"). Referenciada por SetForm.")]
        public string key;
        [Tooltip("Ataque básico desta forma. Null = mantém o ataque base do CharacterData.")]
        public BaseAttackSkill baseSkill;
        [Tooltip("Supremo desta forma. Null = mantém o supremo base do CharacterData.")]
        public SupremeSkill supremeSkill;
        [Tooltip("Chave do perfil de animação (em animationProfiles) aplicado enquanto nesta forma. Vazio = conjunto base.")]
        public string animationProfileKey;
    }
}
