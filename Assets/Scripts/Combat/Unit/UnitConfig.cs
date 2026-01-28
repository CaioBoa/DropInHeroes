public enum Team
{
    Player = 1,
    Enemy = 2
}

/// <summary>
/// Configuração para inicialização de unidades
/// </summary>
public struct UnitConfig
{
    public Team team;
    public bool enableDrag;
    public bool enableFootprint;

    public static UnitConfig Player => new UnitConfig
    {
        team = Team.Player,
        enableDrag = true,
        enableFootprint = true
    };

    public static UnitConfig Enemy => new UnitConfig
    {
        team = Team.Enemy,
        enableDrag = false,
        enableFootprint = false
    };
}
