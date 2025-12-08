/// <summary>
/// Interface base para todos os dados do jogo
/// Garante que todo dado tem ID único e pode ser catalogado
/// </summary>
public interface IGameData
{
    string ID { get; }
    DataCategory Category { get; }
}

public enum DataCategory
{
    Character,
    Dialogue,
    Battle
}