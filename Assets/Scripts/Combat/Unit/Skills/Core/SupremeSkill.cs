using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Base dos SUPREMOS — o ÚNICO tipo de skill com custo de energia. É a fonte do MaxEnergy da
    /// unidade (via <see cref="StatBudget"/>); ataques básicos e passivas não expõem energia.
    /// O slot <c>CharacterData.supremeSkill</c> só aceita este tipo.
    /// </summary>
    public abstract class SupremeSkill : ActiveSkill
    {
        [Header("Energia")]
        [Tooltip("Energia necessária para conjurar. Fonte do MaxEnergy da unidade (MaxEnergy não é stat de orçamento).")]
        public float energyCost = 50f;
    }
}
