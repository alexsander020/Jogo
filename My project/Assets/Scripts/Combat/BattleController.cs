using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public static BattleController Instance;

    public List<Unit> allUnits = new List<Unit>();
    public List<Unit> turnQueue = new List<Unit>();
    public Unit currentUnit;
    public int roundCount = 0;

    public event Action<Unit> OnTurnStart;
    public event Action<Unit> OnTurnEnd;
    public event Action<Team> OnBattleEnd;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        allUnits.Remove(unit);
        turnQueue.Remove(unit);
    }

    public void InitBattle()
    {
        roundCount = 1;
        BuildTurnQueue();
        Debug.Log($"[NetShift Battle] Batalha iniciada. Rodada {roundCount}. Total de unidades: {allUnits.Count}");
    }

    public void BuildTurnQueue()
    {
        turnQueue.Clear();
        // Clona unidades vivas
        List<Unit> activeUnits = allUnits.FindAll(u => u != null && u.gameObject.activeInHierarchy);
        
        // Ordena por velocidade (SPEED) decrescente
        activeUnits.Sort((a, b) =>
        {
            int speedA = a.stats != null ? a.stats.GetStat(StatEnum.SPEED) : 10;
            int speedB = b.stats != null ? b.stats.GetStat(StatEnum.SPEED) : 10;
            return speedB.CompareTo(speedA);
        });

        turnQueue.AddRange(activeUnits);
    }

    public Unit GetNextUnit()
    {
        // Remove unidades mortas ou nulas da fila
        turnQueue.RemoveAll(u => u == null || !u.gameObject.activeInHierarchy);

        if (turnQueue.Count == 0)
        {
            roundCount++;
            BuildTurnQueue();
            Debug.Log($"[NetShift Battle] Iniciando Rodada {roundCount}...");
        }

        if (turnQueue.Count > 0)
        {
            currentUnit = turnQueue[0];
            turnQueue.RemoveAt(0);
            return currentUnit;
        }

        return null;
    }

    public void StartCurrentUnitTurn()
    {
        if (currentUnit == null) return;

        currentUnit.StartTurn();
        Debug.Log($"[NetShift Battle] Turno de: {currentUnit.unitName} (Time: {currentUnit.team}, Cat: {currentUnit.category}, Protocol: {currentUnit.protocol})");
        OnTurnStart?.Invoke(currentUnit);
    }

    public void EndCurrentUnitTurn()
    {
        if (currentUnit != null)
        {
            currentUnit.EndTurn();
            OnTurnEnd?.Invoke(currentUnit);
        }

        // Verifica condição de vitória/derrota
        if (CheckBattleEnd(out Team winner))
        {
            Debug.Log($"[NetShift Battle] Batalha encerrada! Vencedor: {winner}");
            OnBattleEnd?.Invoke(winner);
            return;
        }

        // Passa para o próximo turno na StateMachine
        if (StateMachineController.Instance != null)
        {
            StateMachineController.Instance.ChangeTo<TurnStartState>();
        }
    }

    public bool CheckBattleEnd(out Team winner)
    {
        bool hasPlayer = allUnits.Exists(u => u != null && u.team == Team.Player && u.gameObject.activeInHierarchy);
        bool hasEnemy = allUnits.Exists(u => u != null && u.team == Team.Enemy && u.gameObject.activeInHierarchy);

        if (!hasPlayer)
        {
            winner = Team.Enemy;
            return true;
        }
        if (!hasEnemy)
        {
            winner = Team.Player;
            return true;
        }

        winner = Team.Neutral;
        return false;
    }
}
