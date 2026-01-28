using UnityEngine;

public static class DamageCalculator
{
    public static DamageResult Calculate(
        StatsModule source,
        StatsModule target,
        float baseDamage,
        StatScaling[] scalings,
        DamageConfig config)
    {
        float raw = baseDamage;

        if (source != null && scalings != null)
        {
            for (int i = 0; i < scalings.Length; i++)
                raw += source.GetStat(scalings[i].stat) * scalings[i].multiplier;
        }

        bool isCrit = false;
        if (!config.cannotCrit && source != null)
        {
            isCrit = config.alwaysCrit || Random.value < source.CritRate;
            if (isCrit)
                raw *= source.CritDamage;
        }

        float final_ = config.trueDamage || target == null
            ? raw
            : Mathf.Max(0f, raw - target.Defense);

        return new DamageResult
        {
            rawDamage = raw,
            finalDamage = final_,
            isCritical = isCrit,
            isExtinction = config.extinction
        };
    }
}
