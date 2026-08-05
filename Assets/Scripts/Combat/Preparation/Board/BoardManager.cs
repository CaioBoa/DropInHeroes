using System;
using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Gerencia a lista de unidades posicionadas no board
    /// Responsabilidade única: tracking de unidades no tabuleiro
    /// </summary>
    public class BoardManager
    {
        private readonly List<UnitController> boardUnits = new List<UnitController>();
        private readonly int maxUnitsOnField;

        // === EVENTS ===
        public event Action<UnitController> OnUnitAdded;
        public event Action<UnitController> OnUnitRemoved;
        public event Action OnBoardFull;
        public event Action OnBoardAvailable;

        // === CONSTRUCTOR ===

        public BoardManager(int maxUnits)
        {
            maxUnitsOnField = maxUnits;
        }

        // === PUBLIC API ===

        /// <summary>
        /// Adiciona uma unidade ao board
        /// </summary>
        public void AddUnit(UnitController unit)
        {
            if (unit == null)
            {
                DebugManager.LogError("Tentativa de adicionar unidade nula!", DebugCategory.Drag);
                return;
            }

            if (boardUnits.Contains(unit))
            {
                DebugManager.LogWarning($"Unidade {unit.GetCharacterData()?.displayName} já está no board!", DebugCategory.Drag);
                return;
            }

            bool wasFull = IsFull;

            boardUnits.Add(unit);
            OnUnitAdded?.Invoke(unit);

            DebugManager.Log($"Unidade adicionada: {unit.GetCharacterData()?.displayName} ({UnitCount}/{maxUnitsOnField})", DebugCategory.Drag);

            // Notificar se ficou cheio
            if (!wasFull && IsFull)
            {
                OnBoardFull?.Invoke();
            }
        }

        /// <summary>
        /// Remove uma unidade do board
        /// </summary>
        public void RemoveUnit(UnitController unit)
        {
            if (unit == null)
            {
                DebugManager.LogError("Tentativa de remover unidade nula!", DebugCategory.Drag);
                return;
            }

            bool wasFull = IsFull;

            if (boardUnits.Remove(unit))
            {
                OnUnitRemoved?.Invoke(unit);
                DebugManager.Log($"Unidade removida: {unit.GetCharacterData()?.displayName} ({UnitCount}/{maxUnitsOnField})", DebugCategory.Drag);

                // Notificar se ficou disponível
                if (wasFull && !IsFull)
                {
                    OnBoardAvailable?.Invoke();
                }
            }
            else
            {
                DebugManager.LogWarning($"Unidade {unit.GetCharacterData()?.displayName} não estava no board!", DebugCategory.Drag);
            }
        }

        /// <summary>
        /// Verifica se uma unidade específica está no board
        /// </summary>
        public bool Contains(UnitController unit)
        {
            return boardUnits.Contains(unit);
        }

        /// <summary>
        /// Retorna as unidades no board como read-only. Expõe a própria List (que já implementa
        /// IReadOnlyList) em vez de AsReadOnly(), evitando alocar um wrapper a cada chamada — este
        /// método é percorrido por frame durante o drag.
        /// </summary>
        public IReadOnlyList<UnitController> GetAllUnits()
        {
            return boardUnits;
        }

        /// <summary>
        /// Remove todas as unidades do board
        /// </summary>
        public void Clear()
        {
            bool wasFull = IsFull;

            boardUnits.Clear();
            DebugManager.Log("Board limpo", DebugCategory.Drag);

            if (wasFull)
            {
                OnBoardAvailable?.Invoke();
            }
        }

        // === PROPERTIES ===

        /// <summary>
        /// Verifica se o board está cheio
        /// </summary>
        public bool IsFull => boardUnits.Count >= maxUnitsOnField;

        /// <summary>
        /// Retorna o número atual de unidades no board
        /// </summary>
        public int UnitCount => boardUnits.Count;

        /// <summary>
        /// Retorna o número máximo de unidades permitidas
        /// </summary>
        public int MaxUnits => maxUnitsOnField;

        /// <summary>
        /// Retorna o número de slots disponíveis
        /// </summary>
        public int AvailableSlots => maxUnitsOnField - boardUnits.Count;
    }
}
