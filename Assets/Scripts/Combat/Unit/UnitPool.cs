using UnityEngine;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public class UnitPool : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject unitPrefab;  // Prefab genérico com UnitController

        [Header("Pool Settings")]
        [SerializeField] private int preloadCount = 10;

        private Queue<GameObject> availableUnits = new Queue<GameObject>();
        private List<GameObject> activeUnits = new List<GameObject>();

        public void Initialize() => Initialize(preloadCount);

        /// <summary>
        /// Pré-cria 'count' unidades. O pool ainda cresce sob demanda se 'count' for excedido,
        /// mas dimensionar corretamente evita Instantiate em runtime (ex.: ao invocar no combate).
        /// </summary>
        public void Initialize(int count)
        {
            int target = Mathf.Max(count, 0);
            for (int i = 0; i < target; i++)
            {
                GameObject unit = CreateUnit();
                unit.SetActive(false);
                availableUnits.Enqueue(unit);
            }

            DebugManager.Log($"Pré-carregadas {target} unidades", DebugCategory.Pool);
        }

        private GameObject CreateUnit()
        {
            GameObject unit = Instantiate(unitPrefab, transform);
            return unit;
        }

        public GameObject SpawnUnit(CharacterData data, Vector2 position)
        {
            return SpawnUnit(data, position, UnitConfig.Player);
        }

        public GameObject SpawnUnit(CharacterData data, Vector2 position, UnitConfig config)
        {
            GameObject unit;

            if (availableUnits.Count > 0)
            {
                unit = availableUnits.Dequeue();
            }
            else
            {
                DebugManager.LogWarning("Pool vazio! Criando nova unidade...", DebugCategory.Pool);
                unit = CreateUnit();
            }

            // Posicionar em 2D (Vector2)
            unit.transform.position = new Vector3(position.x, position.y, 0f);

            // Initialize UnitController with character data and config
            if (data != null)
            {
                UnitController controller = unit.GetComponent<UnitController>();
                if (controller != null)
                {
                    controller.Initialize(data, config);
                }
                else
                {
                    DebugManager.LogError("UnitController não encontrado no prefab!", DebugCategory.Pool);
                }
            }

            unit.SetActive(true);
            activeUnits.Add(unit);

            return unit;
        }

        public void ReturnUnit(GameObject unit)
        {
            if (unit == null) return;

            // Reset UnitController to pool state
            UnitController controller = unit.GetComponent<UnitController>();
            if (controller != null)
            {
                controller.ResetToPool();
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
}