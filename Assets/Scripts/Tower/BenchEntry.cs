using UnityEngine;
using DropInHeroes.Combat;
using DropInHeroes.Data;

/// <summary>
/// Slot do bench da Torre. Começa vazio (Character=null). Preenchido por ordem
/// de compra na loja. Persiste rank/posição entre fases — o unit em si volta
/// ao pool a cada fim de combate (reset total), respawnado na próxima prep
/// usando LastBoardPosition se WasDeployed for true.
/// </summary>
public class BenchEntry
{
    public CharacterData Character;
    public int Rank;
    public UnitController DeployedController;
    public Vector2 LastBoardPosition;
    public bool WasDeployed;

    public bool IsEmpty => Character == null || Rank <= 0;
    public bool IsDeployed => DeployedController != null;
}
