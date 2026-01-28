using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Orquestra todo o combate: inicia, gerencia morte de unidades, detecta vitória
/// </summary>
public class CombatController : MonoBehaviour
{
    public static CombatController Instance { get; private set; }

    [SerializeField] private float victoryDelay = 2f;

    private List<UnitController> playerUnits = new List<UnitController>();
    private List<UnitController> enemyUnits = new List<UnitController>();
    private List<UnitController> allCombatUnits = new List<UnitController>();
    private bool isCombatActive;

    public event Action<Team> OnCombatEnded;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Inicia combate entre duas equipes
    /// </summary>
    public void StartCombat(IReadOnlyList<UnitController> players, List<UnitController> enemies)
    {
        if (isCombatActive)
        {
            DebugManager.LogWarning("Combate já está ativo!", DebugCategory.Combat);
            return;
        }

        playerUnits = new List<UnitController>(players);
        enemyUnits = new List<UnitController>(enemies);
        allCombatUnits.Clear();
        allCombatUnits.AddRange(playerUnits);
        allCombatUnits.AddRange(enemyUnits);
        isCombatActive = true;

        // Configurar unidades do player
        foreach (var unit in playerUnits)
        {
            DisablePreparationModules(unit);
            EnableCombat(unit, enemyUnits);
        }

        // Configurar unidades inimigas
        foreach (var unit in enemyUnits)
        {
            DisablePreparationModules(unit);
            EnableCombat(unit, playerUnits);
        }

        DebugManager.Log($"Combate iniciado: {playerUnits.Count} vs {enemyUnits.Count}", DebugCategory.Combat);
    }

    private void DisablePreparationModules(UnitController unit)
    {
        unit.GetModule<DragModule>()?.OnDisabled();
        unit.GetModule<FootprintModule>()?.OnDisabled();
    }

    private void EnableCombat(UnitController unit, List<UnitController> targets)
    {
        unit.SetState(UnitState.Combat);
        unit.GetModule<HealthBarModule>()?.OnEnabled();
        unit.GetModule<CombatModule>()?.StartCombat(targets);
    }

    /// <summary>
    /// Chamado quando uma unidade morre
    /// </summary>
    public void OnUnitDied(UnitController unit)
    {
        if (!isCombatActive) return;

        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);

        DebugManager.Log($"Unidade morreu. Player: {playerUnits.Count}, Enemy: {enemyUnits.Count}", DebugCategory.Combat);

        // Verificar vitória
        if (playerUnits.Count == 0)
            EndCombat(Team.Enemy);
        else if (enemyUnits.Count == 0)
            EndCombat(Team.Player);
    }

    private void EndCombat(Team winner)
    {
        isCombatActive = false;
        StartCoroutine(EndCombatRoutine(winner));
    }

    private IEnumerator EndCombatRoutine(Team winner)
    {
        var winningUnits = winner == Team.Player ? playerUnits : enemyUnits;

        foreach (var unit in winningUnits)
        {
            if (unit != null)
                unit.GetModule<CombatModule>()?.SetState(CombatState.Victory);
        }

        foreach (var unit in allCombatUnits)
        {
            if (unit != null)
                unit.GetModule<HealthBarModule>()?.OnDisabled();
        }

        yield return new WaitForSeconds(victoryDelay);

        allCombatUnits.Clear();
        DebugManager.Log($"Combate encerrado! Vencedor: {winner}", DebugCategory.Combat);
        OnCombatEnded?.Invoke(winner);
    }

    public bool IsCombatActive => isCombatActive;
}
