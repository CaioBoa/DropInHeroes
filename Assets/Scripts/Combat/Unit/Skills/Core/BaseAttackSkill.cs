using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Base semântica para ataques básicos. Sem membros próprios — a composição (dano + efeitos) vive
    /// nas subclasses de <see cref="ModularBaseAttack"/> (código). Mantida como marcador de tipo do
    /// slot de ataque básico.
    /// </summary>
    public abstract class BaseAttackSkill : ActiveSkill
    {
    }
}
