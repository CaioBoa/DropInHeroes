using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Orquestra todo o combate: inicia, gerencia morte de unidades, detecta vitória
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        public static CombatController Instance { get; private set; }

        [SerializeField] private float victoryDelay = 2f;
        [Tooltip("Pool usado para spawnar invocações no meio do combate.")]
        [SerializeField] private UnitPool unitPool;

        private List<UnitController> playerUnits = new List<UnitController>();
        private List<UnitController> enemyUnits = new List<UnitController>();
        private List<UnitController> allCombatUnits = new List<UnitController>();
        private readonly List<UnitController> summons = new List<UnitController>();
        private readonly CombatRulesRegistry rules = new CombatRulesRegistry();
        private bool isCombatActive;

        /// <summary>Regras de combate ativas (itens/relíquias/regras globais por categoria).</summary>
        public CombatRulesRegistry Rules => rules;

        public event Action<Team> OnCombatEnded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                DebugManager.LogWarning("CombatController duplicado na cena — destruindo a cópia.", DebugCategory.Combat);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Diagnóstico + higiene: se a instância ativa for destruída, registra (com stack) e limpa
            // a referência estática para evitar acessar um objeto destruído.
            if (Instance == this)
            {
                DebugManager.LogWarning($"CombatController.Instance destruído (isCombatActive={isCombatActive}). Veja o stack para a origem.", DebugCategory.Combat);
                Instance = null;
            }
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

            // Início de combate em DUAS FASES para não apagar escudos concedidos no início do combate:
            // Fase 1 — reset (zera energia + limpa escudos) de TODAS as unidades ANTES de qualquer grant.
            foreach (var unit in playerUnits)
            {
                DisablePreparationModules(unit);
                ResetUnitForCombat(unit, playerUnits, enemyUnits);
            }
            foreach (var unit in enemyUnits)
            {
                DisablePreparationModules(unit);
                ResetUnitForCombat(unit, enemyUnits, playerUnits);
            }

            // Fase 2 — begin (build/artefato + passiva + ativação). Como todos já resetaram, o escudo que
            // uma passiva/artefato concede a aliados NÃO é mais limpo por ninguém (bug order-dependent).
            foreach (var unit in playerUnits) BeginUnitCombat(unit);
            foreach (var unit in enemyUnits) BeginUnitCombat(unit);

            DebugManager.Log($"Combate iniciado: {playerUnits.Count} vs {enemyUnits.Count}", DebugCategory.Combat);
        }

        private void DisablePreparationModules(UnitController unit)
        {
            unit.GetModule<DragModule>()?.OnDisabled();
            unit.GetModule<FootprintModule>()?.OnDisabled();
        }

        // Início de combate de UMA unidade em duas fases (usado por invocações spawnadas no meio do
        // combate — unidade única, sem outra unidade limpando seu escudo). O batch inicial em StartCombat
        // chama ResetUnitForCombat/BeginUnitCombat em passes SEPARADOS (todos resetam antes de qualquer grant).
        private void EnableCombat(UnitController unit, List<UnitController> allies, List<UnitController> enemies)
        {
            ResetUnitForCombat(unit, allies, enemies);
            BeginUnitCombat(unit);
        }

        // Fase 1: estado de combate + reset (zera energia/escudos), sem conceder nada.
        private void ResetUnitForCombat(UnitController unit, List<UnitController> allies, List<UnitController> enemies)
        {
            unit.SetState(UnitState.Combat);
            unit.GetModule<CombatModule>()?.ResetForCombat(allies, enemies);
        }

        // Fase 2: grants de início de combate (build/artefato + passiva) + barras + regras de item.
        private void BeginUnitCombat(UnitController unit)
        {
            unit.GetModule<CombatModule>()?.BeginCombat();
            // A energia foi zerada no reset; exibir as barras depois garante o snap inicial real (energia em 0).
            unit.GetModule<HealthBarModule>()?.OnEnabled();
            // Barra de energia só para quem pode usar o supremo (heróis e invocações com UseSupreme).
            if (unit.HasCapability(UnitCapability.UseSupreme))
                unit.GetModule<EnergyBarModule>()?.OnEnabled();
            // Aplica regras de combate (itens/relíquias) que casam com esta unidade — inclusive
            // invocações que entram no meio do combate.
            rules.RegisterUnit(unit);
        }

        /// <summary>Registra um item: adiciona suas regras ao registro (aplicadas às unidades em combate).</summary>
        public void RegisterItem(CombatItem item)
        {
            if (item == null) return;
            foreach (var rule in item.CreateRules())
                rules.AddRule(rule);
        }

        // === INVOCAÇÕES ===

        /// <summary>
        /// Spawna uma invocação no meio do combate e a integra ao time. Retorna o controller
        /// (para o caller aplicar stats/aura). Invocações não contam no critério de vitória.
        /// </summary>
        public UnitController SpawnSummon(CharacterData data, Team team, Vector2 position, UnitController owner = null)
        {
            if (!isCombatActive || unitPool == null || data == null) return null;

            // O que a invocação pode fazer vem das UnitCapability da CharacterData dela.
            GameObject obj = unitPool.SpawnUnit(data, position, UnitConfig.Summon(team));
            UnitController unit = obj != null ? obj.GetComponent<UnitController>() : null;
            if (unit == null) return null;

            unit.SetOwner(owner);
            // Deriva os stats a partir do dono (perfil na CharacterData da invocação) ANTES das regras
            // de item, que entram no EnableCombat abaixo e se somam por cima.
            unit.GetModule<SummonModule>()?.DeriveStatsFromOwner();

            List<UnitController> allies = team == Team.Player ? playerUnits : enemyUnits;
            List<UnitController> enemies = team == Team.Player ? enemyUnits : playerUnits;

            // As listas são compartilhadas por referência com as unidades já em combate,
            // então adicionar aqui já insere a invocação nos alvos/aliados de todos.
            allies.Add(unit);
            allCombatUnits.Add(unit);
            summons.Add(unit);

            EnableCombat(unit, allies, enemies);
            // Nasce com vida cheia (após derivação + regras de item, que podem alterar a Vida Máxima).
            unit.Stats?.GetResourceObject(ResourceType.Health)?.SetToMax();
            DebugManager.Log($"Invocação adicionada ({team}): {data.displayName}", DebugCategory.Combat);
            return unit;
        }

        /// <summary>Remove uma invocação do combate e a devolve ao pool (ex.: recast que substitui).</summary>
        public void DespawnSummon(UnitController summon)
        {
            if (summon == null || !summons.Remove(summon)) return; // só age se ainda rastreada

            DetachUnitFromCombat(summon);
            if (unitPool != null) unitPool.ReturnUnit(summon.gameObject);
        }

        /// <summary>Desregistra a unidade do owner/regras e a remove de todas as listas de combate.</summary>
        private void DetachUnitFromCombat(UnitController unit)
        {
            unit.Owner?.UnregisterSummon(unit);
            rules.UnregisterUnit(unit);
            playerUnits.Remove(unit);
            enemyUnits.Remove(unit);
            allCombatUnits.Remove(unit);
        }

        /// <summary>
        /// Chamado quando uma unidade morre
        /// </summary>
        public void OnUnitDied(UnitController unit)
        {
            if (!isCombatActive) return;

            // Remove de todas as listas (sem isto, EndCombatRoutine itera referências mortas/recicladas).
            DetachUnitFromCombat(unit);

            // Invocação morta não deixa corpo: some na hora (volta ao pool). Também não conta p/ vitória.
            if (unit.IsSummon)
            {
                summons.Remove(unit);
                if (unitPool != null) unitPool.ReturnUnit(unit.gameObject);
            }

            // Invocações não contam para vitória: um time com apenas invocações já perdeu.
            int playerReal = CountNonSummon(playerUnits);
            int enemyReal = CountNonSummon(enemyUnits);

            DebugManager.Log($"Unidade morreu. Player real: {playerReal}, Enemy real: {enemyReal}", DebugCategory.Combat);

            if (playerReal == 0) EndCombat(Team.Enemy);
            else if (enemyReal == 0) EndCombat(Team.Player);
        }

        private static int CountNonSummon(List<UnitController> units)
        {
            int count = 0;
            for (int i = 0; i < units.Count; i++)
                if (units[i] != null && !units[i].IsSummon) count++;
            return count;
        }

        private void EndCombat(Team winner)
        {
            isCombatActive = false;
            // Todas as invocações restantes (dos dois times) somem ao fim do combate, ganhando ou perdendo.
            ReturnAllSummonsToPool();
            StartCoroutine(EndCombatRoutine(winner));
        }

        /// <summary>
        /// Força o término do combate com um vencedor (usado para timeout de fase).
        /// </summary>
        public void ForceEndCombat(Team winner)
        {
            if (!isCombatActive) return;
            EndCombat(winner);
        }

        /// <summary>
        /// Aborta o combate IMEDIATAMENTE, sem rotina de vitória nem <see cref="OnCombatEnded"/> — para
        /// reinício instantâneo (modo Sandbox). Interrompe a EndCombatRoutine em curso, devolve invocações
        /// ao pool e limpa o estado. As unidades NÃO-invocação ficam por conta do caller (que as devolve
        /// ao pool e recria a formação). Só o CombatController roda corrotinas neste objeto.
        /// </summary>
        public void AbortCombat()
        {
            if (!isCombatActive && allCombatUnits.Count == 0 && summons.Count == 0) return;

            StopAllCoroutines(); // cancela qualquer EndCombatRoutine pendente
            isCombatActive = false;
            ReturnAllSummonsToPool();
            allCombatUnits.Clear();
            playerUnits.Clear();
            enemyUnits.Clear();
            rules.Clear();
            DebugManager.Log("Combate abortado (Sandbox reset).", DebugCategory.Combat);
        }

        private IEnumerator EndCombatRoutine(Team winner)
        {
            var winningUnits = winner == Team.Player ? playerUnits : enemyUnits;
            var losingUnits = winner == Team.Player ? enemyUnits : playerUnits;

            foreach (var unit in winningUnits)
            {
                if (unit != null)
                    unit.GetModule<CombatModule>()?.SetState(CombatState.Victory);
            }

            // Quem perdeu o round e seguiu VIVO (ex.: fim por timeout, sem wipe) morre ao fim do round —
            // senão as unidades perdedoras continuariam lutando (ticando o FSM próprio) durante a dança de
            // vitória. SetState(Dead) para o combate e toca a animação de morte. Mortas já saíram das listas.
            foreach (var unit in losingUnits)
            {
                if (unit != null)
                    unit.GetModule<CombatModule>()?.SetState(CombatState.Dead);
            }

            foreach (var unit in allCombatUnits)
            {
                if (unit != null)
                {
                    unit.GetModule<HealthBarModule>()?.OnDisabled();
                    unit.GetModule<EnergyBarModule>()?.OnDisabled();
                }
            }

            yield return new WaitForSeconds(victoryDelay);

            allCombatUnits.Clear();
            rules.Clear();
            DebugManager.Log($"Combate encerrado! Vencedor: {winner}", DebugCategory.Combat);
            OnCombatEnded?.Invoke(winner);
        }

        private void ReturnAllSummonsToPool()
        {
            // Detacha cada invocação das listas de combate antes de devolver ao pool — assim a rotina
            // de vitória e allCombatUnits não seguram referências a unidades já recicladas.
            for (int i = summons.Count - 1; i >= 0; i--)
            {
                UnitController summon = summons[i];
                if (summon == null) continue;
                DetachUnitFromCombat(summon);
                if (unitPool != null) unitPool.ReturnUnit(summon.gameObject);
            }
            summons.Clear();
        }

        public bool IsCombatActive => isCombatActive;
    }
}
