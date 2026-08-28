using System;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Integration
{
    public static class UnitAdapter
    {
        public static TacticalAttribute MapProtocolToAttribute(ProtocolTrinity protocol)
        {
            return protocol switch
            {
                ProtocolTrinity.Firewall => TacticalAttribute.Vaccine,
                ProtocolTrinity.Ping => TacticalAttribute.Data,
                ProtocolTrinity.Overclock => TacticalAttribute.Virus,
                _ => TacticalAttribute.Free
            };
        }

        public static ProtocolTrinity MapAttributeToProtocol(TacticalAttribute attribute)
        {
            return attribute switch
            {
                TacticalAttribute.Vaccine => ProtocolTrinity.Firewall,
                TacticalAttribute.Data => ProtocolTrinity.Ping,
                TacticalAttribute.Virus => ProtocolTrinity.Overclock,
                _ => ProtocolTrinity.Firewall
            };
        }

        public static GridFacing MapFacing(FacingDirection facing)
        {
            return facing switch
            {
                FacingDirection.North => GridFacing.North,
                FacingDirection.East => GridFacing.East,
                FacingDirection.South => GridFacing.South,
                FacingDirection.West => GridFacing.West,
                _ => GridFacing.South
            };
        }

        public static FacingDirection MapFacingBack(GridFacing facing)
        {
            return facing switch
            {
                GridFacing.North => FacingDirection.North,
                GridFacing.East => FacingDirection.East,
                GridFacing.South => FacingDirection.South,
                GridFacing.West => FacingDirection.West,
                _ => FacingDirection.South
            };
        }

        public static EvolutionTier MapEvolutionRank(EvolutionRank rank)
        {
            return rank switch
            {
                EvolutionRank.Standard => EvolutionTier.Rookie,
                EvolutionRank.Super => EvolutionTier.Champion,
                EvolutionRank.Ultimate => EvolutionTier.Ultimate,
                EvolutionRank.God => EvolutionTier.Mega,
                _ => EvolutionTier.Rookie
            };
        }

        public static EvolutionRank MapEvolutionTierBack(EvolutionTier tier)
        {
            return tier switch
            {
                EvolutionTier.Rookie => EvolutionRank.Standard,
                EvolutionTier.Champion => EvolutionRank.Super,
                EvolutionTier.Ultimate => EvolutionRank.Ultimate,
                EvolutionTier.Mega => EvolutionRank.God,
                _ => EvolutionRank.Standard
            };
        }

        public static TacticalTeam MapTeam(Team team)
        {
            return team switch
            {
                Team.Player => TacticalTeam.Player,
                Team.Enemy => TacticalTeam.Enemy,
                Team.Neutral => TacticalTeam.Neutral,
                _ => TacticalTeam.Player
            };
        }

        public static Team MapTacticalTeamBack(TacticalTeam team)
        {
            return team switch
            {
                TacticalTeam.Player => Team.Player,
                TacticalTeam.Enemy => Team.Enemy,
                TacticalTeam.Neutral => Team.Neutral,
                _ => Team.Player
            };
        }

        public static UnitState CreateUnitStateFromMono(Unit unit)
        {
            if (unit == null) return null;

            int hp = unit.stats != null ? unit.stats.GetStat(StatEnum.HP) : 100;
            int maxHp = unit.stats != null ? unit.stats.GetStat(StatEnum.MaxHp) : 100;
            int sp = unit.stats != null ? unit.stats.GetStat(StatEnum.SP) : 50;
            int maxSp = unit.stats != null ? unit.stats.GetStat(StatEnum.MaxSp) : 50;
            int atk = unit.stats != null ? unit.stats.GetStat(StatEnum.ATK) : 20;
            int def = unit.stats != null ? unit.stats.GetStat(StatEnum.DEF) : 10;
            int mov = unit.stats != null ? unit.stats.GetStat(StatEnum.MOV) : 3;

            var state = new UnitState
            {
                id = unit.gameObject.GetInstanceID().ToString(),
                name = unit.unitName,
                speciesId = unit.unitName,
                team = MapTeam(unit.team),
                coord = new GridCoord(unit.gridPosition.x, unit.gridPosition.y, unit.gridPosition.z),
                facing = MapFacing(unit.facing),
                attribute = MapProtocolToAttribute(unit.protocol),
                hp = hp,
                maxHp = maxHp,
                attack = atk,
                defense = def,
                movementBudget = mov,
                canEvolveThisTurn = true
            };

            state.evolution.currentTier = MapEvolutionRank(unit.rank);
            state.evolution.currentSP = sp;
            state.evolution.maxSP = maxSp;

            return state;
        }

        public static void SyncBackToMono(UnitState state, Unit unit)
        {
            if (state == null || unit == null) return;

            unit.unitName = state.name;
            unit.team = MapTacticalTeamBack(state.team);
            unit.facing = MapFacingBack(state.facing);
            unit.protocol = MapAttributeToProtocol(state.attribute);
            unit.rank = MapEvolutionTierBack(state.evolution.currentTier);

            if (unit.stats != null)
            {
                unit.stats.SetStat(StatEnum.HP, state.hp);
                unit.stats.SetStat(StatEnum.MaxHp, state.maxHp);
                unit.stats.SetStat(StatEnum.SP, state.evolution.currentSP);
                unit.stats.SetStat(StatEnum.MaxSp, state.evolution.maxSP);
                unit.stats.SetStat(StatEnum.ATK, state.attack);
                unit.stats.SetStat(StatEnum.DEF, state.defense);
                unit.stats.SetStat(StatEnum.MOV, state.movementBudget);
            }
        }
    }
}
