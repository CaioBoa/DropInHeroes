using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Skill composta por efeitos reutilizáveis (<see cref="SkillEffect"/>). Exposta para que o
    /// tooltip renderize a descrição da skill + a de cada efeito (cada um refletindo seus campos).
    /// </summary>
    public interface ISkillEffectSource
    {
        IEnumerable<SkillEffect> DescribableEffects { get; }
    }
}
