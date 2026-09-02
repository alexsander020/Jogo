using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.Combat;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Tests
{
    public static class AppmonAndComboTestSuite
    {
        public static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[TEST FAILED] ❌ {testName}");
            }
            Debug.Log($"[TEST PASSED] ✔️ {testName}");
        }

        public static int RunAllTests()
        {
            int passed = 0;
            Debug.Log("=================================================");
            Debug.Log("INICIANDO TESTES DO COMPÊNDIO APPMON E COMBOS");
            Debug.Log("=================================================");

            // 1. TESTES DO BANCO DE DADOS DE APPMON
            Test_AppmonDatabase_All30Registered(); passed++;
            Test_AppmonDatabase_StatsAndAttributesExactMatch(); passed++;
            Test_AppmonDatabase_SuperAppmonFusions(); passed++;
            Test_AppmonDatabase_InheritedSkills(); passed++;
            Test_AppmonDatabase_DualTypesCelestialAndDemon(); passed++;

            // 2. TESTES DE STATUS EFFECTS & PASSIVAS
            Test_StatusEffects_ImmobilizeAndStun(); passed++;
            Test_Passives_Immunities(); passed++;
            Test_Passives_RebirthFromAshes(); passed++;
            Test_Passives_SatanmonUncontrolledFury(); passed++;
            Test_Passives_LucifermonPrideDamageBoost(); passed++;

            // 3. TESTES DOS 18 TERRENOS
            Test_Terrains_All18RegisteredAndProperties(); passed++;

            // 4. TESTES DO MANUAL DE 14 ATAQUES COMBINADOS
            Test_CombinedAttacks_CatalogCompleteness(); passed++;
            Test_CombinedAttacks_HydroSonicShockwave(); passed++;
            Test_CombinedAttacks_CelestialHarmonyArray(); passed++;
            Test_CombinedAttacks_CataclysmicPandemonium(); passed++;

            Debug.Log("=================================================");
            Debug.Log($"SUCESSO: TODOS OS {passed} TESTES DE APPMON E COMBOS FORAM APROVADOS!");
            Debug.Log("=================================================");

            return passed;
        }

        // =========================================================================
        // 1. BANCO DE DADOS
        // =========================================================================
        public static void Test_AppmonDatabase_All30Registered()
        {
            var all = AppmonDatabase.GetAll();
            Assert(all.Count == 30, $"AppmonDatabase deve conter exatamente 30 Appmon cadastrados (Encontrados: {all.Count}).");
        }

        public static void Test_AppmonDatabase_StatsAndAttributesExactMatch()
        {
            // Valida Data-Viper
            var viper = AppmonDatabase.Get("Data-Viper");
            Assert(viper != null, "Data-Viper deve existir no banco.");
            Assert(viper.hp == 120 && viper.mp == 60 && viper.atk == 45 && viper.def == 55 &&
                   viper.intStat == 40 && viper.spi == 50 && viper.spd == 52 && viper.crt == 5,
                   "Atributos exatos do Data-Viper conferem com o Compêndio.");
            Assert(viper.primaryCategory == FunctionalCategory.Security, "Data-Viper é do tipo Security.");

            // Valida Satanmon
            var satan = AppmonDatabase.Get("Satanmon");
            Assert(satan != null, "Satanmon deve existir no banco.");
            Assert(satan.hp == 2200 && satan.mp == 850 && satan.atk == 360 && satan.def == 180 &&
                   satan.intStat == 190 && satan.spi == 170 && satan.spd == 300 && satan.crt == 45,
                   "Atributos exatos do Satanmon conferem com o Compêndio.");
        }

        public static void Test_AppmonDatabase_SuperAppmonFusions()
        {
            var hydro = AppmonDatabase.FindFusion("Data-Viper", "Shitakumon");
            Assert(hydro != null && hydro.name == "Hydro-Vipermon", "Fusão Data-Viper + Shitakumon resulta em Hydro-Vipermon.");

            var sonic = AppmonDatabase.FindFusion("Glitch-Hound", "Sound-Beat");
            Assert(sonic != null && sonic.name == "Sonic-Debugger", "Fusão Glitch-Hound + Sound-Beat resulta em Sonic-Debugger.");

            var architect = AppmonDatabase.FindFusion("Craft-Craft", "Data-Viper");
            Assert(architect != null && architect.name == "Architectmon", "Fusão Craft-Craft + Data-Viper resulta em Architectmon.");

            var magma = AppmonDatabase.FindFusion("Flame-Log", "Craft-Craft");
            Assert(magma != null && magma.name == "Magma-Logmon", "Fusão Flame-Log + Craft-Craft resulta em Magma-Logmon.");

            var electro = AppmonDatabase.FindFusion("Volt-Plug", "Shadow-Cam");
            Assert(electro != null && electro.name == "Electro-Cammon", "Fusão Volt-Plug + Shadow-Cam resulta em Electro-Cammon.");

            var bioMag = AppmonDatabase.FindFusion("Bio-Patch", "Magnet-Core");
            Assert(bioMag != null && bioMag.name == "Bio-Magnetmon", "Fusão Bio-Patch + Magnet-Core resulta em Bio-Magnetmon.");
        }

        public static void Test_AppmonDatabase_InheritedSkills()
        {
            var poseidon = AppmonDatabase.Get("Poseidon-Vipermon");
            Assert(poseidon != null, "Poseidon-Vipermon existe no banco.");
            Assert(poseidon.skills.Exists(s => s.skillName == "Abyssal Dominion"), "Possui a nova habilidade Abyssal Dominion.");
            Assert(poseidon.skills.Exists(s => s.skillName == "Quarantine Lock"), "Herda Quarantine Lock.");
            Assert(poseidon.skills.Exists(s => s.skillName == "Jato de Água"), "Herda Jato de Água.");
            Assert(poseidon.skills.Exists(s => s.skillName == "Hydro Quarantine"), "Herda Hydro Quarantine.");
        }

        public static void Test_AppmonDatabase_DualTypesCelestialAndDemon()
        {
            var genbu = AppmonDatabase.Get("Genbu-Architectmon");
            Assert(genbu != null && genbu.IsDualType, "Genbu-Architectmon possui tipo duplo.");
            Assert(genbu.primaryCategory == FunctionalCategory.Security && genbu.secondaryCategory == FunctionalCategory.Tool,
                "Genbu-Architectmon é Security / Tool.");

            var seiryu = AppmonDatabase.Get("Seiryu-Vipermon");
            Assert(seiryu != null && seiryu.primaryCategory == FunctionalCategory.Security && seiryu.secondaryCategory == FunctionalCategory.System,
                "Seiryu-Vipermon é Security / System.");

            var suzaku = AppmonDatabase.Get("Suzaku-Beatmon");
            Assert(suzaku != null && suzaku.primaryCategory == FunctionalCategory.Entertainment && suzaku.secondaryCategory == FunctionalCategory.System,
                "Suzaku-Beatmon é Entertainment / System.");

            var byakko = AppmonDatabase.Get("Byakko-Houndmon");
            Assert(byakko != null && byakko.primaryCategory == FunctionalCategory.System && byakko.secondaryCategory == FunctionalCategory.Tool,
                "Byakko-Houndmon é System / Tool.");
        }

        // =========================================================================
        // 2. STATUS EFFECTS E PASSIVAS
        // =========================================================================
        public static void Test_StatusEffects_ImmobilizeAndStun()
        {
            var go = new GameObject("TestUnit");
            var unit = go.AddComponent<Unit>();
            var ch = go.AddComponent<AppmonCharacter>();

            Assert(ch.CanMove() && ch.CanAct(), "Unidade nova sem debuffs pode mover e agir.");

            ch.ApplyStatus(StatusEffectType.Immobilized, 1);
            Assert(!ch.CanMove() && ch.CanAct(), "Imobilizado bloqueia movimentação mas permite ações.");

            ch.ApplyStatus(StatusEffectType.Stun, 1);
            Assert(!ch.CanMove() && !ch.CanAct(), "Atordoado bloqueia movimentação e ações.");

            GameObject.DestroyImmediate(go);
        }

        public static void Test_Passives_Immunities()
        {
            var go = new GameObject("TestSonic");
            var unit = go.AddComponent<Unit>();
            var ch = go.AddComponent<AppmonCharacter>();
            ch.InitializeFromAppmon("Sonic-Debugger");

            ch.ApplyStatus(StatusEffectType.Blind, 2);
            Assert(!ch.HasStatus(StatusEffectType.Blind), "Cancelamento de Ruído (Sonic-Debugger) anula debuffs.");

            GameObject.DestroyImmediate(go);
        }

        public static void Test_Passives_RebirthFromAshes()
        {
            var go = new GameObject("TestSuzaku");
            var unit = go.AddComponent<Unit>();
            unit.stats = go.AddComponent<Stats>();
            var ch = go.AddComponent<AppmonCharacter>();
            ch.InitializeFromAppmon("Suzaku-Beatmon");

            unit.stats.SetStat(StatEnum.HP, 0);
            bool revived = ch.TryTriggerRebirth();
            Assert(revived, "Suzaku-Beatmon renasce imediatamente na primeira morte.");
            Assert(unit.stats.GetStat(StatEnum.HP) == 1050, "HP restaurado para 50% de 2100 (1050).");

            bool secondDeathRevive = ch.TryTriggerRebirth();
            Assert(!secondDeathRevive, "Renascer das Cinzas só ativa uma única vez.");

            GameObject.DestroyImmediate(go);
        }

        public static void Test_Passives_SatanmonUncontrolledFury()
        {
            var satanData = AppmonDatabase.Get("Satanmon");
            Assert(satanData.passiveId == "uncontrolled_fury", "Satanmon possui Fúria Descontrolada.");
        }

        public static void Test_Passives_LucifermonPrideDamageBoost()
        {
            var lucifData = AppmonDatabase.Get("Lucifermon");
            Assert(lucifData.passiveId == "pride_arrogance", "Lucifermon possui Soberba.");
        }

        // =========================================================================
        // 3. TERRENOS
        // =========================================================================
        public static void Test_Terrains_All18RegisteredAndProperties()
        {
            TerrainType[] requiredTerrains = new[]
            {
                TerrainType.Flooded, TerrainType.Fire, TerrainType.Magma, TerrainType.StealthGrid,
                TerrainType.CodeWall, TerrainType.NoiseZone, TerrainType.DigitalOcean, TerrainType.RockyMapping,
                TerrainType.Electrified, TerrainType.ThermalTrack, TerrainType.WindBlades, TerrainType.SovereignThrone,
                TerrainType.InertiaZone, TerrainType.FalseMirrors, TerrainType.CorruptedWater, TerrainType.GoldenTrap,
                TerrainType.ChaosFire, TerrainType.ChargeTile
            };

            foreach (var t in requiredTerrains)
            {
                var data = TerrainDatabase.Get(t);
                Assert(!string.IsNullOrEmpty(data.displayName), $"Terreno {t} está cadastrado com nome legível: {data.displayName}");
            }

            var ocean = TerrainDatabase.Get(TerrainType.DigitalOcean);
            Assert(ocean.movementCost == 3, "Oceano Digital tem custo de movimento 3.");

            var codeWall = TerrainDatabase.Get(TerrainType.CodeWall);
            Assert(!codeWall.isWalkable, "Muralha de Código é intransponível.");
        }

        // =========================================================================
        // 4. ATAQUES COMBINADOS
        // =========================================================================
        public static void Test_CombinedAttacks_CatalogCompleteness()
        {
            Assert(CombinedAttackService.Catalog.Count == 14, 
                $"Catálogo de Ataques Combinados deve conter exatamente 14 combos (Encontrados: {CombinedAttackService.Catalog.Count}).");

            for (int i = 1; i <= 14; i++)
            {
                var id = (CombinedAttackId)i;
                Assert(CombinedAttackService.Catalog.ContainsKey(id), $"Combo {id} está registrado no catálogo.");
            }
        }

        public static void Test_CombinedAttacks_HydroSonicShockwave()
        {
            var def = CombinedAttackService.Catalog[CombinedAttackId.HydroSonicShockwave];
            Assert(def.participantA == "Hydro-Vipermon" && def.participantB == "Sonic-Debugger",
                "Hydro-Sonic Shockwave tem participantes corretos.");
        }

        public static void Test_CombinedAttacks_CelestialHarmonyArray()
        {
            var def = CombinedAttackService.Catalog[CombinedAttackId.CelestialHarmonyArray];
            Assert(def.participantA == "Genbu-Architectmon" && def.participantB == "Seiryu-Vipermon",
                "Celestial Harmony Array tem os guardiões celestiais corretos.");
        }

        public static void Test_CombinedAttacks_CataclysmicPandemonium()
        {
            var def = CombinedAttackService.Catalog[CombinedAttackId.CataclysmicPandemonium];
            Assert(def.participantA == "Satanmon" && def.participantB == "Lucifermon",
                "Cataclysmic Pandemonium tem os demônios Satanmon e Lucifermon.");
        }
    }
}
