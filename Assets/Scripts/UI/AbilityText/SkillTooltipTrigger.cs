using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DropInHeroes.UI
{
    /// <summary>
    /// Em cada ícone de skill do painel CharacterInfo: abre o <see cref="AbilityTooltip"/> ao hover e
    /// fecha ao sair. Os dados da skill (nome, descrição e o objeto p/ reflexão dos valores) são
    /// definidos pelo <see cref="CharacterInfoPanel"/> via <see cref="SetSkill"/>.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private AbilityTooltip tooltip;

        private object skill;
        private string skillName, description;

        public void SetSkill(object skill, string name, string description)
        {
            this.skill = skill;
            skillName = name;
            this.description = description;
        }

        /// <summary>Mesma coisa, mas ligando o tooltip em runtime — para gatilhos em prefabs instanciados
        /// (ex.: pips de status), que não podem referenciar o AbilityTooltip da cena no asset.</summary>
        public void SetSkill(object skill, string name, string description, AbilityTooltip runtimeTooltip)
        {
            if (runtimeTooltip != null) tooltip = runtimeTooltip;
            SetSkill(skill, name, description);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (skill == null || tooltip == null) return;
            tooltip.Show(skillName, description, skill, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip != null) tooltip.Hide();
        }
    }
}
