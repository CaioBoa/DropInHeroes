using UnityEngine;
using System.Collections.Generic;

public class UnitPool : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject unitPrefab;  // Prefab genérico com UnitController

    [Header("Pool Settings")]
    [SerializeField] private int preloadCount = 10;

    private Queue<GameObject> availableUnits = new Queue<GameObject>();
    private List<GameObject> activeUnits = new List<GameObject>();

    public void Initialize()
    {
        // Pré-criar unidades
        for (int i = 0; i < preloadCount; i++)
        {
            GameObject unit = CreateUnit();
            unit.SetActive(false);
            availableUnits.Enqueue(unit);
        }

        Debug.Log($"[UnitPool] Pré-carregadas {preloadCount} unidades");
    }

    private GameObject CreateUnit()
    {
        GameObject unit = Instantiate(unitPrefab, transform);
        return unit;
    }

    public GameObject SpawnUnit(CharacterData data, Vector3 position)
    {
        GameObject unit;

        if (availableUnits.Count > 0)
        {
            unit = availableUnits.Dequeue();
        }
        else
        {
            Debug.LogWarning("[UnitPool] Pool vazio! Criando nova unidade...");
            unit = CreateUnit();
        }

        unit.transform.position = position;
        unit.SetActive(true);

        // Inicializar com dados de combate
        UnitController controller = unit.GetComponent<UnitController>();
        if (controller != null)
        {
            controller.Initialize(data);
        }

        activeUnits.Add(unit);
        return unit;
    }

    public void ReturnUnit(GameObject unit)
    {
        if (unit == null) return;

        // Resetar unidade
        UnitController controller = unit.GetComponent<UnitController>();
        if (controller != null)
        {
            controller.ResetUnit();
        }

        unit.SetActive(false);
        activeUnits.Remove(unit);
        availableUnits.Enqueue(unit);
    }

    public void ReturnAllUnits()
    {
        // Retornar todas unidades ativas ao pool
        for (int i = activeUnits.Count - 1; i >= 0; i--)
        {
            ReturnUnit(activeUnits[i]);
        }
    }
}