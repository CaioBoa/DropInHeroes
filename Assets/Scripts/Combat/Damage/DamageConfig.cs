public struct DamageConfig
{
    public bool cannotCrit;
    public bool alwaysCrit;
    public bool trueDamage;
    public bool extinction;

    public static DamageConfig Default => new DamageConfig();
}
