using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Atributo numérico de uma unidade.
    ///
    /// ⚠️ CONTRATO DE SERIALIZAÇÃO — os valores são explícitos de propósito.
    /// Este enum é serializado como INT dentro dos .asset (ex.: <c>type: 16</c> em Hami.asset).
    /// O número, e não o nome, é o que fica gravado em disco e, no futuro, em save de jogador.
    ///
    /// Regras, sem exceção:
    ///   1. NUNCA reordenar. Nunca inserir um valor no meio.
    ///   2. Stat novo ANEXA no fim, com o próximo número livre.
    ///   3. Stat aposentado mantém seu número reservado (marque como obsoleto, não remova).
    /// Quebrar qualquer uma delas remapeia silenciosamente todos os assets já autorados.
    ///
    /// Ver também o contrato de reflexão: <c>CharacterData.GetBaseStat(StatType.X)</c> resolve
    /// o campo <c>baseX</c> por nome — renomear um valor aqui exige renomear o campo lá.
    /// </summary>
    public enum StatType
    {
        Attack = 0,
        Defense = 1,
        Speed = 2,
        Range = 3,
        MaxHealth = 4,
        MaxEnergy = 5,
        EnergyRegeneration = 6,
        CritRate = 7,
        CritDamage = 8,
        CritDamageResistance = 9,
        Penetration = 10,
        PenetrationResistance = 11,
        DamageBonus = 12,
        DamageReduction = 13,
        Effectiveness = 14,
        EffectivenessResistance = 15,
        MagicalPower = 16,
        MagicalDefense = 17,
        MagicalPenetration = 18,
        MagicalPenetrationResistance = 19,
        Control = 20,
        Tenacity = 21,
        Lifesteal = 22,
        Accuracy = 23,
        Dodge = 24,

        // Base 100 (0 = 100%): bônus/penalidade ADITIVOS de cura/escudo, causados e recebidos.
        // Aplicados pelos módulos CauseHeal/ReceiveHeal e CauseShield/ReceiveShield (ver StatsModule).
        HealBonus = 25,       // cura CAUSADA por esta unidade (self, aliados, lifesteal)
        HealReceived = 26,    // cura RECEBIDA por esta unidade (de si, de aliados, lifesteal)
        ShieldBonus = 27,     // escudo CONCEDIDO por esta unidade
        ShieldReceived = 28   // escudo RECEBIDO por esta unidade

        // Próximo valor livre: 29
    }
}
