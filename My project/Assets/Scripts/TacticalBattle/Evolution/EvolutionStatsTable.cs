using System;
using System.Collections.Generic;
using TacticalBattle.Core;

namespace TacticalBattle.Evolution
{
    [Serializable]
    public struct BaseTierStats
    {
        public int maxHp;
        public int attack;
        public int defense;
        public int movementBudget;

        public BaseTierStats(int maxHp, int attack, int defense, int movementBudget)
        {
            this.maxHp = maxHp;
            this.attack = attack;
            this.defense = defense;
            this.movementBudget = movementBudget;
        }
    }

    public static class EvolutionStatsTable
    {
        // Tabela padrão de stats base indexada por Tier
        private static readonly Dictionary<EvolutionTier, BaseTierStats> DefaultStatsByTier = new Dictionary<EvolutionTier, BaseTierStats>
        {
            { EvolutionTier.Rookie,   new BaseTierStats(maxHp: 100, attack: 20, defense: 10, movementBudget: 3) },
            { EvolutionTier.Champion, new BaseTierStats(maxHp: 180, attack: 40, defense: 20, movementBudget: 4) },
            { EvolutionTier.Ultimate, new BaseTierStats(maxHp: 280, attack: 65, defense: 35, movementBudget: 4) },
            { EvolutionTier.Mega,     new BaseTierStats(maxHp: 400, attack: 95, defense: 55, movementBudget: 5) }
        };

        // Permite registrar tabelas personalizadas por espécie
        private static readonly Dictionary<string, Dictionary<EvolutionTier, BaseTierStats>> SpeciesStatsTables = 
            new Dictionary<string, Dictionary<EvolutionTier, BaseTierStats>>();

        public static void RegisterSpeciesTable(string speciesId, Dictionary<EvolutionTier, BaseTierStats> table)
        {
            SpeciesStatsTables[speciesId] = table;
        }

        public static BaseTierStats GetStatsForTier(string speciesId, EvolutionTier tier)
        {
            if (!string.IsNullOrEmpty(speciesId))
            {
                var appmon = Appmon.AppmonDatabase.Get(speciesId);
                if (appmon != null)
                {
                    return new BaseTierStats(appmon.hp, appmon.atk, appmon.def, appmon.mov);
                }

                if (SpeciesStatsTables.TryGetValue(speciesId, out var speciesTable))
                {
                    if (speciesTable.TryGetValue(tier, out var stats)) return stats;
                }
            }

            if (DefaultStatsByTier.TryGetValue(tier, out var defaultStats))
            {
                return defaultStats;
            }

            return new BaseTierStats(100, 20, 10, 3);
        }

        public static void ApplyBaseStatsForTier(UnitState unit, EvolutionTier tier)
        {
            BaseTierStats baseStats = GetStatsForTier(unit.speciesId, tier);

            float hpRatio = unit.maxHp > 0 ? (float)unit.hp / unit.maxHp : 1.0f;
            unit.maxHp = baseStats.maxHp;
            unit.hp = Math.Clamp((int)Math.Round(baseStats.maxHp * hpRatio), 1, baseStats.maxHp);
            unit.attack = baseStats.attack;
            unit.defense = baseStats.defense;
            unit.movementBudget = baseStats.movementBudget;
        }
    }
}
