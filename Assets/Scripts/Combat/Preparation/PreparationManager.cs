using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Gerencia a fase de preparação de combate onde o jogador posiciona unidades aliadas
/// </summary>
public class PreparationManager : MonoBehaviour
{
    public static PreparationManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CarouselController carouselController;
    [SerializeField] private UnitPool unitPool;
    [SerializeField] private PositionIndicator positionIndicator;
    [SerializeField] private Camera mainCamera;

    [Header("Battle Field Settings")]
    [SerializeField] private Vector2 battleFieldCenter = Vector2.zero;
    [SerializeField] private Vector2 battleFieldSize = new Vector2(10f, 6f);
    [SerializeField] private float swapDetectionRadius = 1.5f;

    [Header("Limits")]
    [SerializeField] private int maxUnitsOnField = 4;

    // State
    private List<BattleFieldUnit> fieldUnits = new List<BattleFieldUnit>();
    private Rect battleFieldBounds;
    private TaskCompletionSource<bool> confirmationTCS;

    // === INITIALIZATION ===

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Calcular bounds do campo de batalha
        battleFieldBounds = new Rect(
            battleFieldCenter.x - battleFieldSize.x / 2f,
            battleFieldCenter.y - battleFieldSize.y / 2f,
            battleFieldSize.x,
            battleFieldSize.y
        );

        Debug.Log("[PreparationManager] Inicializado");
    }

    private void Start()
    {
        // Inicializar carousel com todos os personagens
        InitializeCarousel();

        // Desativar indicador inicialmente
        if (positionIndicator != null)
        {
            positionIndicator.Hide();
        }
    }

    private void InitializeCarousel()
    {
        if (carouselController == null)
        {
            Debug.LogWarning("[PreparationManager] CarouselController não atribuído!");
            return;
        }

        // Obter todos os personagens do DataManager
        List<CharacterData> allCharacters = DataManager.GetAllCharacters();
        carouselController.Initialize(allCharacters);
        Debug.Log($"[PreparationManager] Carousel inicializado com {allCharacters.Count} personagens");
    }

    // === PHASE MANAGEMENT ===

    public async Task StartPreparationPhase()
    {
        Debug.Log("[PreparationManager] Iniciando fase de preparação...");

        confirmationTCS = new TaskCompletionSource<bool>();

        // Limpar unidades anteriores se houver
        ClearAllUnits();

        // TODO: Mostrar UI de preparação
        // TODO: Ativar interação com carousel

        Debug.Log("[PreparationManager] Fase de preparação iniciada. Aguardando jogador...");
    }

    public void ConfirmPositioning()
    {
        // Chamado pelo botão "Iniciar Batalha" (a ser implementado)
        confirmationTCS?.TrySetResult(true);
    }

    // === DRAG AND DROP CALLBACKS ===

    public void OnDragStart(CharacterData characterData)
    {
        if (positionIndicator != null)
        {
            positionIndicator.Show();
        }

        Debug.Log($"[PreparationManager] Drag iniciado: {characterData.displayName}");
    }

    public void OnDragUpdate(Vector2 screenPos)
    {
        if (positionIndicator == null) return;

        // Converter screen → world position
        Vector2 worldPos = ScreenToWorldPosition(screenPos);

        // Atualizar posição do indicador
        positionIndicator.UpdatePosition(worldPos);

        // Detectar unidade próxima
        BattleFieldUnit nearUnit;
        bool hasNearUnit = GetUnitNearPosition(worldPos, out nearUnit);

        // Validar posição
        bool isValid = IsPositionValid(worldPos);

        // Atualizar estado visual do indicador
        if (!isValid)
        {
            positionIndicator.SetState(PositionIndicatorState.Invalid);
        }
        else if (hasNearUnit)
        {
            positionIndicator.SetState(PositionIndicatorState.Swap);
        }
        else
        {
            positionIndicator.SetState(PositionIndicatorState.Valid);
        }
    }

    public void OnDragEnd(CharacterData characterData, Vector2 screenPos)
    {
        if (positionIndicator != null)
        {
            positionIndicator.Hide();
        }

        Vector2 worldPos = ScreenToWorldPosition(screenPos);

        // Validar posição
        if (!IsPositionValid(worldPos))
        {
            Debug.Log("[PreparationManager] Posição inválida, drag cancelado");
            return;
        }

        // Detectar unidade próxima para swap
        BattleFieldUnit nearUnit;
        if (GetUnitNearPosition(worldPos, out nearUnit))
        {
            // Swap com unidade existente
            SwapUnits(nearUnit, characterData);
        }
        else
        {
            // Spawn nova unidade se não exceder limite
            if (fieldUnits.Count < maxUnitsOnField)
            {
                SpawnUnitAtPosition(characterData, worldPos);
            }
            else
            {
                Debug.LogWarning("[PreparationManager] Limite de unidades atingido (4)");
            }
        }
    }

    // === UNIT MANAGEMENT ===

    private void SpawnUnitAtPosition(CharacterData characterData, Vector2 position)
    {
        if (unitPool == null)
        {
            Debug.LogError("[PreparationManager] UnitPool não atribuído!");
            return;
        }

        // Spawnar unidade via pool
        GameObject unitInstance = unitPool.SpawnUnit(characterData, position);

        if (unitInstance == null)
        {
            Debug.LogError("[PreparationManager] Falha ao spawnar unidade!");
            return;
        }

        // Criar wrapper BattleFieldUnit
        BattleFieldUnit battleUnit = unitInstance.AddComponent<BattleFieldUnit>();
        battleUnit.Initialize(characterData, unitInstance, position);

        // Adicionar à lista
        fieldUnits.Add(battleUnit);

        Debug.Log($"[PreparationManager] Unidade spawnada: {characterData.displayName} em {position}");
    }

    private void SwapUnits(BattleFieldUnit existingUnit, CharacterData newCharacterData)
    {
        Vector2 position = existingUnit.CurrentPosition;

        // Remover unidade antiga
        RemoveUnit(existingUnit);

        // Spawnar nova unidade na mesma posição
        SpawnUnitAtPosition(newCharacterData, position);

        Debug.Log($"[PreparationManager] Unidade trocada por {newCharacterData.displayName}");
    }

    private void RemoveUnit(BattleFieldUnit unit)
    {
        if (unit == null) return;

        // Retornar ao pool
        if (unitPool != null && unit.UnitInstance != null)
        {
            unitPool.ReturnUnit(unit.UnitInstance);
        }

        // Remover da lista
        fieldUnits.Remove(unit);

        // Destruir wrapper
        Destroy(unit);

        Debug.Log("[PreparationManager] Unidade removida");
    }

    private void ClearAllUnits()
    {
        for (int i = fieldUnits.Count - 1; i >= 0; i--)
        {
            RemoveUnit(fieldUnits[i]);
        }

        fieldUnits.Clear();
        Debug.Log("[PreparationManager] Todas as unidades removidas");
    }

    // === HELPER METHODS ===

    public bool GetUnitNearPosition(Vector2 position, out BattleFieldUnit nearestUnit)
    {
        nearestUnit = null;
        float closestDistance = float.MaxValue;

        foreach (var unit in fieldUnits)
        {
            float distance = Vector2.Distance(position, unit.CurrentPosition);

            if (distance < swapDetectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                nearestUnit = unit;
            }
        }

        return nearestUnit != null;
    }

    public bool IsPositionValid(Vector2 position)
    {
        // Verificar se está dentro do battleFieldBounds
        return battleFieldBounds.Contains(position);
    }

    public Vector2 ScreenToWorldPosition(Vector2 screenPos)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[PreparationManager] MainCamera não encontrada!");
            return Vector2.zero;
        }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));
        return new Vector2(worldPos.x, worldPos.y);
    }

    // === DEBUG ===

    private void OnDrawGizmosSelected()
    {
        // Desenhar battleFieldBounds no editor
        Rect bounds = new Rect(
            battleFieldCenter.x - battleFieldSize.x / 2f,
            battleFieldCenter.y - battleFieldSize.y / 2f,
            battleFieldSize.x,
            battleFieldSize.y
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.center.y, 0f), new Vector3(bounds.width, bounds.height, 0.1f));
    }
}
