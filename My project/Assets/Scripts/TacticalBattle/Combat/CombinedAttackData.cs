using System;
using System.Collections.Generic;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Combat
{
    public enum CombinedAttackId
    {
        HydroSonicShockwave = 1,
        AegisThermalBarricade = 2,
        OverclockedStealthAmbush = 3,
        PolarMagneticVortex = 4,
        CelestialHarmonyArray = 5,
        VermilionThunderStorm = 6,
        WhiteTigerFortressShred = 7,
        OmegaTsunamiOverdrive = 8,
        CataclysmicPandemonium = 9,
        AbyssalDevourer = 10,
        GoldenSlothTrap = 11,
        PhantasmagoricLustFire = 12,
        ToxicSystemCorruption = 13,
        AbsoluteZeroOverclock = 14
    }

    [Serializable]
    public class CombinedAttackDefinition
    {
        public CombinedAttackId id;
        public string name;
        public string participantA;
        public string participantB;
        public string terrainRequirementDesc;
        [TextArea(2, 4)]
        public string description;

        public CombinedAttackDefinition(
            CombinedAttackId id,
            string name,
            string participantA,
            string participantB,
            string terrainReq,
            string description)
        {
            this.id = id;
            this.name = name;
            this.participantA = participantA;
            this.participantB = participantB;
            this.terrainRequirementDesc = terrainReq;
            this.description = description;
        }
    }
}
