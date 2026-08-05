using System;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Módulo REUTILIZÁVEL de compartilhamento de dano: uma unidade redireciona parte do dano que
    /// sofreria a um "protetor" (ex.: a passiva do Guliver protege os aliados com Guliver). Não é
    /// lógica específica do Guliver — qualquer unidade tem o seu via <see cref="StatsModule"/>, e
    /// passivas/efeitos só registram entradas com <c>AddDamageShare</c>.
    ///
    /// Prevenção de loop: o dano JÁ redirecionado é aplicado no protetor com
    /// <c>DamageResult.ignoreDamageShare = true</c> (ver StatsModule.ApplyDamage), então NUNCA é
    /// re-compartilhado — duas unidades protegendo uma à outra não entram em loop infinito.
    /// </summary>
    public class DamageShareRegistry
    {
        private readonly List<DamageShareEntry> shares = new List<DamageShareEntry>();
        private static readonly Comparison<DamageShareEntry> ByValueDesc = (a, b) => b.value.CompareTo(a.value);

        public bool IsEmpty => shares.Count == 0;

        /// <summary>Registra um share com protetor fixo.</summary>
        public void Add(string id, StatsModule protector, float value) => Add(id, () => protector, value);

        /// <summary>Registra um share com protetor resolvido no momento do dano (ex.: "a invocação com mais vida").</summary>
        public void Add(string id, Func<StatsModule> protectorResolver, float value)
        {
            shares.Add(new DamageShareEntry { id = id, protectorResolver = protectorResolver, value = value });
            shares.Sort(ByValueDesc);
        }

        public void Remove(string id) => shares.RemoveAll(d => d.id == id);

        public bool Has(string id) => shares.Exists(d => d.id == id);

        public void Clear() => shares.Clear();

        /// <summary>
        /// Melhor protetor válido (maior fração) para 'self', pulando o próprio 'self' e protetores
        /// mortos (cai para o próximo se o mais forte estiver inválido). false = ninguém pode proteger.
        /// </summary>
        public bool TryResolveProtector(StatsModule self, out StatsModule protector, out float fraction)
        {
            for (int i = 0; i < shares.Count; i++)
            {
                StatsModule resolved = shares[i].protectorResolver?.Invoke();
                if (resolved != null && resolved != self && !resolved.IsDead)
                {
                    protector = resolved;
                    fraction = shares[i].value;
                    return true;
                }
            }
            protector = null;
            fraction = 0f;
            return false;
        }
    }
}
