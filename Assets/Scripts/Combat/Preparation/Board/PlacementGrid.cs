using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Gerencia a grade de tiles que define áreas válidas de posicionamento
    /// </summary>
    public class PlacementGrid : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap placementTilemap;
        [SerializeField] private Grid grid;

        [Header("Visual Settings")]
        [SerializeField] private float dragAlpha = 0.3f;    // Transparência durante drag
        [SerializeField] private float idleAlpha = 0f;      // Invisível quando não arrasta

        private HashSet<Vector3Int> validCells;
        private TilemapRenderer tilemapRenderer;
        private Material tilemapMaterialInstance; // instância criada por .material; destruída em OnDestroy
        private bool isInitialized = false;

        // === INITIALIZATION ===

        private void Awake()
        {
            // Obter TilemapRenderer
            if (placementTilemap != null)
            {
                tilemapRenderer = placementTilemap.GetComponent<TilemapRenderer>();
            }
        }

        public void Initialize()
        {
            if (isInitialized) return;

            // Validar referências
            if (placementTilemap == null)
            {
                DebugManager.LogError("PlacementTilemap não atribuído!", DebugCategory.Initialization);
                return;
            }

            if (grid == null)
            {
                DebugManager.LogError("Grid não atribuído!", DebugCategory.Initialization);
                return;
            }

            // Cachear células válidas
            CacheValidCells();

            // Iniciar invisível
            SetTilemapAlpha(idleAlpha);

            isInitialized = true;
            DebugManager.Log($"PlacementGrid inicializado com {validCells.Count} células válidas", DebugCategory.Initialization);
        }

        // === VALIDATION ===

        public bool IsPositionValid(Vector2 worldPosition)
        {
            if (!isInitialized || placementTilemap == null || grid == null)
            {
                return false;
            }

            // Converter world position → cell position
            Vector3Int cellPos = grid.WorldToCell(new Vector3(worldPosition.x, worldPosition.y, 0f));

            // Verificar se existe tile nessa célula (lookup O(1))
            return validCells.Contains(cellPos);
        }

        /// <summary>
        /// Snap de posição para o centro da célula mais próxima
        /// </summary>
        public Vector2 SnapToNearestCell(Vector2 worldPosition)
        {
            if (grid == null)
            {
                DebugManager.LogWarning("Grid não configurado!", DebugCategory.Drag);
                return worldPosition;
            }

            // Converter para cell position e depois voltar para o centro da célula
            Vector3Int cellPos = grid.WorldToCell(new Vector3(worldPosition.x, worldPosition.y, 0f));
            Vector3 snapped = grid.GetCellCenterWorld(cellPos);
            return new Vector2(snapped.x, snapped.y);
        }

        // === VISUALIZATION ===

        public void ShowValidTiles()
        {
            SetTilemapAlpha(dragAlpha);
        }

        public void HideValidTiles()
        {
            SetTilemapAlpha(idleAlpha);
        }

        private void SetTilemapAlpha(float alpha)
        {
            if (tilemapRenderer == null) return;

            // .material instancia um material em runtime; cachear a instância evita acesso repetido
            // e permite destruí-la em OnDestroy (sem isto, vaza ao recarregar a cena).
            if (tilemapMaterialInstance == null)
                tilemapMaterialInstance = tilemapRenderer.material;

            Color color = tilemapMaterialInstance.color;
            color.a = alpha;
            tilemapMaterialInstance.color = color;
        }

        private void OnDestroy()
        {
            if (tilemapMaterialInstance != null)
                Destroy(tilemapMaterialInstance);
        }

        // === CACHING ===

        private void CacheValidCells()
        {
            validCells = new HashSet<Vector3Int>();

            if (placementTilemap == null) return;

            // Iterar sobre bounds do tilemap
            BoundsInt bounds = placementTilemap.cellBounds;

            foreach (var pos in bounds.allPositionsWithin)
            {
                if (placementTilemap.HasTile(pos))
                {
                    validCells.Add(pos);
                }
            }
        }

        // === DEBUG ===

        private void OnDrawGizmosSelected()
        {
            if (placementTilemap == null || grid == null) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);

            BoundsInt bounds = placementTilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (placementTilemap.HasTile(pos))
                {
                    Vector3 worldPos = grid.GetCellCenterWorld(pos);
                    Vector3 cellSize = grid.cellSize;
                    Gizmos.DrawCube(worldPos, cellSize);
                }
            }
        }
    }
}
