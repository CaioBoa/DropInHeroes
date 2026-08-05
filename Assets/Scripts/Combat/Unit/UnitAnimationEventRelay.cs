using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Relê eventos de animação disparados no nó "Visual" (que contém o Animator)
    /// para o UnitController na raiz. Necessário porque AnimationEvents são entregues
    /// ao GameObject do Animator, e o Animator vive no filho visual escalável.
    /// </summary>
    public class UnitAnimationEventRelay : MonoBehaviour
    {
        private UnitController controller;

        private void Awake()
        {
            controller = GetComponentInParent<UnitController>();
        }

        // Chamado pelo Animation Event do clipe de ataque (frame de impacto).
        public void OnAttackHitFrame()
        {
            controller?.OnAttackHitFrame();
        }
    }
}
