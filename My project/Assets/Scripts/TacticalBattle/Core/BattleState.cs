using System;
using System.Collections.Generic;

namespace TacticalBattle.Core
{
    [Serializable]
    public class EvolutionState
    {
        public EvolutionTier currentTier = EvolutionTier.Rookie;
        public int currentSP = 50;
        public int maxSP = 50;
        public Dictionary<EvolutionTier, int> spCostPerTurn = new Dictionary<EvolutionTier, int>
        {
            { EvolutionTier.Rookie, 0 },    // Rookie NUNCA consome SP (Invariante)
            { EvolutionTier.Champion, 10 }, // TODO_DESIGN_CONFIRM: Custo por turno Champion
            { EvolutionTier.Ultimate, 20 }, // TODO_DESIGN_CONFIRM: Custo por turno Ultimate
            { EvolutionTier.Mega, 30 }       // TODO_DESIGN_CONFIRM: Custo por turno Mega
        };

        public EvolutionState Clone()
        {
            var clone = new EvolutionState
            {
                currentTier = this.currentTier,
                currentSP = this.currentSP,
                maxSP = this.maxSP,
                spCostPerTurn = new Dictionary<EvolutionTier, int>(this.spCostPerTurn)
            };
            return clone;
        }
    }

    [Serializable]
    public struct TurnActionBudget
    {
        public bool canMove;
        public bool canUseMainAction; // Atacar ou Talk
        public bool canEvolve;        // Independente de canMove e canUseMainAction

        public static TurnActionBudget CreateFull(bool canEvolve = true)
        {
            return new TurnActionBudget
            {
                canMove = true,
                canUseMainAction = true,
                canEvolve = canEvolve
            };
        }
    }

    [Serializable]
    public class TalkTarget
    {
        public string unitId;
        public PersonalityTrait personality = PersonalityTrait.Brave;
        public float affinity = 0f;                   // 0–100
        public float affinityThresholdToRecruit = 70f; // TODO_DESIGN_CONFIRM: Limiar de recrutamento
        public float itemGrantThreshold = 40f;         // TODO_DESIGN_CONFIRM: Limiar de concessão de item
        public int questionsAnswered = 0;              // Até 3 perguntas
        public const int MaxQuestions = 3;

        public TalkTarget Clone()
        {
            return new TalkTarget
            {
                unitId = this.unitId,
                personality = this.personality,
                affinity = this.affinity,
                affinityThresholdToRecruit = this.affinityThresholdToRecruit,
                itemGrantThreshold = this.itemGrantThreshold,
                questionsAnswered = this.questionsAnswered
            };
        }
    }

    [Serializable]
    public class TalkSession
    {
        public TalkTarget target;
        public TalkPhase phase = TalkPhase.Question1;
        public float affinityGainedThisSession = 0f;
        public TalkResult result = TalkResult.Pending;

        public TalkSession(TalkTarget target)
        {
            this.target = target;
            this.phase = TalkPhase.Question1;
            this.affinityGainedThisSession = 0f;
            this.result = TalkResult.Pending;
        }
    }

    [Serializable]
    public class UnitState
    {
        public string id;
        public string name;
        public string speciesId; // Usado para buscar stats base na tabela
        public TacticalTeam team;
        public GridCoord coord;
        public GridFacing facing;
        public TacticalAttribute attribute;
        public EvolutionState evolution;
        public int hp;
        public int maxHp;
        public int attack;
        public int defense;
        public int movementBudget;
        public int maxClimbHeight; // Limite de subida de degrau sem habilidade especial (padrão: 1)
        public bool canEvolveThisTurn;
        public bool isGuarding;
        public TalkTarget talkTarget;
        public float affinityWithPlayer; // Para aliados humanos / recrutados

        public UnitState()
        {
            evolution = new EvolutionState();
            maxClimbHeight = 1;
            movementBudget = 3;
            canEvolveThisTurn = true;
        }
    }

    [Serializable]
    public class GridCell
    {
        public GridCoord coord;
        public int terrainCost; // Custo de movimento (padrão = 1)
        public bool isWalkable;

        public GridCell(int x, int y, int z = 0, int terrainCost = 1, bool isWalkable = true)
        {
            this.coord = new GridCoord(x, y, z);
            this.terrainCost = terrainCost;
            this.isWalkable = isWalkable;
        }
    }

    [Serializable]
    public class GridState
    {
        public int width;
        public int height;
        public Dictionary<string, GridCell> cells = new Dictionary<string, GridCell>();
        public Dictionary<string, string> occupancy = new Dictionary<string, string>(); // "x,y" -> unitId

        public static string Key(int x, int y) => $"{x},{y}";
        public static string Key(GridCoord c) => $"{c.x},{c.y}";

        public bool IsInside(int x, int y)
        {
            return cells.ContainsKey(Key(x, y));
        }

        public GridCell GetCell(int x, int y)
        {
            string k = Key(x, y);
            cells.TryGetValue(k, out GridCell cell);
            return cell;
        }

        public void SetOccupant(GridCoord coord, string unitId)
        {
            string k = Key(coord);
            if (string.IsNullOrEmpty(unitId))
            {
                occupancy.Remove(k);
            }
            else
            {
                occupancy[k] = unitId;
            }
        }

        public string GetOccupant(GridCoord coord)
        {
            string k = Key(coord);
            occupancy.TryGetValue(k, out string unitId);
            return unitId;
        }
    }

    [Serializable]
    public class KarmaState
    {
        public float moral = 50f;   // 0–100 (Associado a Vaccine)
        public float harmony = 50f; // 0–100 (Associado a Data)
        public float wrath = 50f;   // 0–100 (Associado a Virus)

        public void AddMoral(float amount) => moral = Math.Clamp(moral + amount, 0f, 100f);
        public void AddHarmony(float amount) => harmony = Math.Clamp(harmony + amount, 0f, 100f);
        public void AddWrath(float amount) => wrath = Math.Clamp(wrath + amount, 0f, 100f);
    }

    [Serializable]
    public class BattleState
    {
        public List<UnitState> units = new List<UnitState>();
        public GridState grid = new GridState();
        public KarmaState karma = new KarmaState();
        public List<string> turnOrder = new List<string>();
        public int currentTurnIndex = 0;
        public int roundCount = 1;

        public UnitState GetUnitById(string id)
        {
            return units.Find(u => u.id == id);
        }
    }
}
