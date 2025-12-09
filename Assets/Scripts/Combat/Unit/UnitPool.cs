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

    public GameObject SpawnUnit(CharacterData data, Vector2 position)
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

        // Posicionar em 2D (Vector2)
        unit.transform.position = new Vector3(position.x, position.y, 0f);

        // Aplicar sprite do CharacterData
        if (data != null)
        {
            SpriteRenderer renderer = unit.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = data.defaultSprite;
            }

            // Aplicar animações (se houver Animator)
            Animator animator = unit.GetComponent<Animator>();
            if (animator != null)
            {
                // TODO: Aplicar AnimatorController ou clips específicos do personagem
                // Depende da estrutura do seu Animator
                // animator.runtimeAnimatorController = data.animatorController;
            }
        }

        unit.SetActive(true);
        activeUnits.Add(unit);

        return unit;
    }

    public void ReturnUnit(GameObject unit)
    {
        if (unit == null) return;

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