using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// ESCUDOS da unidade — módulo geral e reutilizável (feature de combate, não lógica de personagem).
    /// Um escudo é uma reserva nomeada de absorção: o dano direto consome escudos ANTES da vida
    /// (ver <see cref="StatsModule.ApplyDamage"/>). Re-aplicar o mesmo id RENOVA o escudo (recast
    /// substitui, não acumula); ids diferentes coexistem e são consumidos na ordem de aplicação.
    /// Escudos duram até quebrar ou até o próximo início de combate/pool (sem duração por tempo —
    /// adicionar quando algum efeito precisar).
    /// </summary>
    public class ShieldModule : IUnitModule
    {
        private class Shield
        {
            public string id;
            public float remaining;
        }

        private UnitController controller;
        private readonly List<Shield> shields = new List<Shield>();

        /// <summary>Disparado quando o total de escudo muda (aplicar/absorver/quebrar) — para UI futura.</summary>
        public event System.Action OnChanged;

        public float TotalShield
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < shields.Count; i++) total += shields[i].remaining;
                return total;
            }
        }

        public void Initialize(UnitController unitController) => controller = unitController;
        public void OnEnabled() { }
        public void OnDisabled() { }

        public void Cleanup()
        {
            shields.Clear();
            controller = null;
        }

        public void ResetForPool() => Clear();

        /// <summary>Aplica/renova um escudo nomeado. Reaplicar o mesmo id substitui o valor restante.
        /// O valor recebido é escalado pelo ShieldReceived do PORTADOR (módulo "receber escudo").</summary>
        public void AddShield(string id, float amount)
        {
            if (string.IsNullOrEmpty(id)) return;
            amount = controller?.Stats != null ? controller.Stats.ReceiveShield(amount) : amount;
            if (amount <= 0f) return;

            for (int i = 0; i < shields.Count; i++)
                if (shields[i].id == id)
                {
                    shields[i].remaining = amount;
                    OnChanged?.Invoke();
                    return;
                }

            shields.Add(new Shield { id = id, remaining = amount });
            OnChanged?.Invoke();
        }

        public void RemoveShield(string id)
        {
            for (int i = shields.Count - 1; i >= 0; i--)
                if (shields[i].id == id) shields.RemoveAt(i);
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Consome escudos com o dano recebido (ordem de aplicação) e retorna o dano RESTANTE que deve
        /// ir para a vida. Escudos zerados quebram (são removidos).
        /// </summary>
        public float AbsorbDamage(float damage)
        {
            if (damage <= 0f || shields.Count == 0) return damage;

            float remaining = damage;
            for (int i = 0; i < shields.Count && remaining > 0f; i++)
            {
                float absorbed = remaining < shields[i].remaining ? remaining : shields[i].remaining;
                shields[i].remaining -= absorbed;
                remaining -= absorbed;
            }

            for (int i = shields.Count - 1; i >= 0; i--)
                if (shields[i].remaining <= 0f) shields.RemoveAt(i);

            OnChanged?.Invoke();
            return remaining;
        }

        public void Clear()
        {
            if (shields.Count == 0) return;
            shields.Clear();
            OnChanged?.Invoke();
        }
    }
}
