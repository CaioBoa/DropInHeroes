using UnityEngine;

/// <summary>
/// Valida posições de placement de unidades
/// Responsabilidade: regras de validação e detecção de overlaps
/// </summary>
public class PlacementValidator
{
    private readonly PlacementGrid grid;
    private readonly float swapDetectionRadius;

    // === CONSTRUCTOR ===

    public PlacementValidator(PlacementGrid placementGrid, float swapRadius)
    {
        grid = placementGrid;
        swapDetectionRadius = swapRadius;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Verifica se uma posição está dentro dos limites do grid
    /// </summary>
    public bool IsPositionValid(Vector2 position)
    {
        if (grid == null)
        {
            DebugManager.LogWarning("PlacementGrid não configurado!", DebugCategory.Drag);
            return false;
        }

        return grid.IsPositionValid(position);
    }

    /// <summary>
    /// Detecta se há uma unidade próxima à posição (overlap)
    /// Compara footprint com footprint
    /// </summary>
    public UnitController GetOverlappingUnit(Vector2 position, BoardManager board)
    {
        if (board == null) return null;

        UnitController nearestUnit = null;
        float closestDistance = float.MaxValue;

        foreach (var unit in board.GetAllUnits())
        {
            if (unit == null) continue;

            // Comparar posição do FOOTPRINT da unidade no board
            var footprintModule = unit.GetModule<FootprintModule>();
            if (footprintModule == null) continue;

            Vector2 unitFootprintPos = footprintModule.GetCurrentWorldPosition();
            float distance = Vector2.Distance(position, unitFootprintPos);

            if (distance < swapDetectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                nearestUnit = unit;
            }
        }

        return nearestUnit;
    }

    /// <summary>
    /// Snap de posição para o grid mais próximo
    /// </summary>
    public Vector2 SnapToGrid(Vector2 position)
    {
        if (grid == null)
        {
            DebugManager.LogWarning("PlacementGrid não configurado!", DebugCategory.Drag);
            return position;
        }

        return grid.SnapToNearestCell(position);
    }

    // === PROPERTIES ===

    public float SwapRadius => swapDetectionRadius;
}
