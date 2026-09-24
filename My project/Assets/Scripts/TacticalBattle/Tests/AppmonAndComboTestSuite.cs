using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.AppLink;
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

            // 5. TESTES DE MAPEAMENTO DOS EFEITOS VISUAIS (FREE SLASH VFX E WATER SPELL)
            Test_AttackVfx_DatabaseMapping(); passed++;
            Test_WaterWall_PersistentLoopingBarrier5Turns(); passed++;

            // 6. TESTES DO SISTEMA DE APP-LINK
            Test_AppLink_System(); passed++;

            // 7. TESTES DA UI DE BUFFS DO BATTLE HUD
            Test_BattleHUD_LinkBuffsPanel(); passed++;

            // 8. TESTES DO SISTEMA DE FUSÃO EM BATALHA (APP GAPPAI)
            Test_AppGappai_CompatibilityTable(); passed++;
            Test_AppGappai_TemporaryFusionExecution(); passed++;
            Test_AppGappai_BattleEndRestoration(); passed++;
            Test_AppGappai_FieldLinkSimpleBonus(); passed++;

            Debug.Log("=================================================");
            Debug.Log($"SUCESSO: TODOS OS {passed} TESTES DE APPMON, COMBOS, APP-LINK E APP GAPPAI FORAM APROVADOS!");
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
            Assert(poseidon.skills.Exists(s => s.skillName == "Water Jet" || s.skillName == "Jato de Água"), "Herda Water Jet / Jato de Água.");
            Assert(poseidon.skills.Exists(s => s.skillName == "Water Wall" || s.skillName == "Parede de Água"), "Herda Water Wall / Parede de Água.");
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

        public static void Test_AttackVfx_DatabaseMapping()
        {
            // 1. Ataques elementais diretos
            var fireSkill = new SkillData { id = "firewall_flare", skillName = "Firewall Flare", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(fireSkill) == "Slash Projectile VFX Fire", "Firewall Flare mapeia para Slash Projectile VFX Fire.");

            var waterSkill = new SkillData { id = "water_jet", skillName = "Water Jet", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(waterSkill) == "Slash Water VFX", "Water Jet mapeia para Slash Water VFX.");

            var waterSkillPt = new SkillData { id = "water_jet", skillName = "Jato de Água", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(waterSkillPt) == "Slash Water VFX", "Jato de Água também mapeia para Slash Water VFX.");

            var waterWallSkill = new SkillData { id = "water_wall", skillName = "Water Wall", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(waterWallSkill) == "WaterSpell2", "Water Wall mapeia para WaterSpell2.");

            var waterWallSkillPt = new SkillData { id = "water_wall", skillName = "Parede de Água", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(waterWallSkillPt) == "WaterSpell2", "Parede de Água também mapeia para WaterSpell2.");

            var elecSkill = new SkillData { id = "spark_zap", skillName = "Spark Zap", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(elecSkill) == "Slash Projectile VFX Eletric", "Spark Zap mapeia para Slash Projectile VFX Eletric.");

            var earthSkill = new SkillData { id = "tool_strike", skillName = "Tool Strike", maxRange = 1 };
            Assert(AttackVfxDatabase.ResolveVfxName(earthSkill) == "Slash Earth VFX", "Tool Strike mapeia para Slash Earth VFX.");

            // 2. Cortes básicos e múltiplos
            var basicSkill = SkillData.CreateBasicAttack("Atacar", 85, 2);
            Assert(AttackVfxDatabase.ResolveVfxName(basicSkill) == "Slash VFX", "Ataque básico mapeia para Slash VFX.");

            var multiSkill = new SkillData { id = "error_bite", skillName = "Error Bite", maxRange = 1 };
            Assert(AttackVfxDatabase.ResolveVfxName(multiSkill) == "Multiple Slashes", "Error Bite mapeia para Multiple Slashes.");

            // 3. Fallback inteligente por descrição/nome
            var customFire = new SkillData { skillName = "Chamas Devastadoras", description = "Queima o alvo com fogo intenso", maxRange = 1 };
            Assert(AttackVfxDatabase.ResolveVfxName(customFire) == "Slash Fire VFX", "Habilidade com palavra-chave 'fogo' melee mapeia para Slash Fire VFX.");

            var customRangedWater = new SkillData { skillName = "Onda Abissal", description = "Dispara água em alta pressão", maxRange = 3 };
            Assert(AttackVfxDatabase.ResolveVfxName(customRangedWater) == "Slash Projectile VFX Water", "Habilidade com palavra-chave 'água' ranged mapeia para Slash Projectile VFX Water.");
        }

        // =========================================================================
        // 6. TESTES DO SISTEMA DE APP-LINK
        // =========================================================================
        public static void Test_AppLink_System()
        {
            AppLinkService.ResetAllLinks();

            // Setup de unidade para teste
            var go1 = new GameObject("TestFieldUnit1");
            var unit1 = go1.AddComponent<Unit>();
            var stats1 = go1.AddComponent<Stats>();
            stats1.InitializeStatsIfEmpty();
            unit1.stats = stats1;
            unit1.unitName = "Test-Security-Unit";
            unit1.category = FunctionalCategory.Security;
            stats1.SetStat(StatEnum.ATK, 100);
            stats1.SetStat(StatEnum.DEF, 80);

            // 1. Escalonamento Standard (+5% base, +7.5% com compatibilidade)
            var dataViper = AppmonDatabase.Get("Data-Viper"); // Standard, Security
            Assert(dataViper != null, "Data-Viper existe no banco.");
            var calcStdComp = AppLinkService.CalculateBonus(unit1, dataViper);
            Assert(calcStdComp.hasCompatibility, "Compatibilidade ativada (mesma categoria Security).");
            Assert(Mathf.Approximately(calcStdComp.compatibilityMultiplier, 1.5f), "Multiplicador de compatibilidade é 1.5x.");
            Assert(calcStdComp.statBonuses.Count > 0, "Possui bônus de status.");
            Assert(Mathf.Approximately(calcStdComp.statBonuses[0].finalPercent, 7.5f), "Bônus final do Standard compatível é 7.5% (5% * 1.5).");

            var glitchHound = AppmonDatabase.Get("Glitch-Hound"); // Standard, System
            var calcStdNoComp = AppLinkService.CalculateBonus(unit1, glitchHound);
            Assert(!calcStdNoComp.hasCompatibility, "Sem compatibilidade (Security vs System).");
            Assert(Mathf.Approximately(calcStdNoComp.compatibilityMultiplier, 1.0f), "Multiplicador é 1.0x.");
            Assert(Mathf.Approximately(calcStdNoComp.statBonuses[0].finalPercent, 5.0f), "Bônus final do Standard sem compatibilidade é 5.0%.");

            // 2. Escalonamento Super (+10% base, +15% com compatibilidade)
            var architect = AppmonDatabase.Get("Architectmon"); // Super, Security
            Assert(architect != null, "Architectmon existe no banco.");
            var calcSuperComp = AppLinkService.CalculateBonus(unit1, architect);
            Assert(calcSuperComp.hasCompatibility, "Super compatível.");
            Assert(Mathf.Approximately(calcSuperComp.statBonuses[0].finalPercent, 15.0f), "Super compatível concede +15% (+10% * 1.5) no atributo principal.");
            Assert(calcSuperComp.statBonuses[0].stat == StatEnum.ATK, "Atributo principal é ATK (maior stat = 100).");

            var magmaLog = AppmonDatabase.Get("Magma-Logmon"); // Super, System
            var calcSuperNoComp = AppLinkService.CalculateBonus(unit1, magmaLog);
            Assert(!calcSuperNoComp.hasCompatibility, "Super sem compatibilidade.");
            Assert(Mathf.Approximately(calcSuperNoComp.statBonuses[0].finalPercent, 10.0f), "Super sem compatibilidade concede +10% no atributo principal.");

            // 3. Escalonamento Ultimate (+15% em 2 atributos: 8% e 7%, ou 12% e 10.5% com compatibilidade)
            var genbu = AppmonDatabase.Get("Genbu-Architectmon"); // Ultimate, Security / Tool
            Assert(genbu != null, "Genbu-Architectmon existe no banco.");
            var calcUltComp = AppLinkService.CalculateBonus(unit1, genbu);
            Assert(calcUltComp.hasCompatibility, "Ultimate compatível.");
            Assert(calcUltComp.statBonuses.Count == 2, "Ultimate concede bônus em 2 atributos principais.");
            Assert(Mathf.Approximately(calcUltComp.statBonuses[0].finalPercent, 12.0f), "Bônus primário Ultimate compatível é 12.0% (8% * 1.5).");
            Assert(Mathf.Approximately(calcUltComp.statBonuses[1].finalPercent, 10.5f), "Bônus secundário Ultimate compatível é 10.5% (7% * 1.5).");

            // 4. Escalonamento God (+25% base, +37.5% com compatibilidade)
            var deus = AppmonDatabase.Get("Deusmon"); // God, System
            var calcGodNoComp = AppLinkService.CalculateBonus(unit1, deus);
            Assert(!calcGodNoComp.hasCompatibility, "Deusmon sem compatibilidade com Security.");
            Assert(Mathf.Approximately(calcGodNoComp.statBonuses[0].finalPercent, 25.0f), "God sem compatibilidade concede +25% no atributo principal.");

            var goSys = new GameObject("TestSysUnit");
            var unitSys = goSys.AddComponent<Unit>();
            var statsSys = goSys.AddComponent<Stats>();
            statsSys.InitializeStatsIfEmpty();
            unitSys.stats = statsSys;
            unitSys.category = FunctionalCategory.System;
            statsSys.SetStat(StatEnum.ATK, 100);
            var calcGodComp = AppLinkService.CalculateBonus(unitSys, deus);
            Assert(calcGodComp.hasCompatibility, "Deusmon compatível com System.");
            Assert(Mathf.Approximately(calcGodComp.statBonuses[0].finalPercent, 37.5f), "God com compatibilidade concede +37.5% (25% * 1.5) no atributo principal.");

            // 5. Aplicação, Exclusividade, Limite Máximo e Remoção
            int atkBefore = unit1.stats.GetStat(StatEnum.ATK);
            bool linked = AppLinkService.ApplyLink(unit1, architect, out string linkErr);
            Assert(linked, $"App-Link aplicado com sucesso: {linkErr}");
            Assert(unit1.IsLinked, "unit1 está marcada como IsLinked.");
            Assert(unit1.linkedBagAppmon == architect, "unit1 está vinculada a Architectmon.");
            Assert(AppLinkService.IsAppmonLinked(architect), "Architectmon está marcado como [LINKADO].");
            Assert(unit1.stats.GetStat(StatEnum.ATK) > atkBefore, "ATK da unidade aumentou com o vínculo.");
            Assert(unit1.hasActed, "Vincular App-Link consome a ação do turno (hasActed = true).");

            // Regra: Apenas 1 App-Link por personagem (Substituição/Troca dinâmica sem empilhar vínculos)
            glitchHound = AppmonDatabase.Get("Glitch-Hound");
            bool swapped = AppLinkService.ApplyLink(unit1, glitchHound, out string swapErr);
            Assert(swapped, "Troca de parceiro realizada com sucesso no mesmo personagem.");
            Assert(unit1.IsLinked && unit1.linkedBagAppmon.name == "Glitch-Hound", "unit1 agora possui como parceiro único o Glitch-Hound.");
            Assert(!AppLinkService.IsAppmonLinked(architect), "O parceiro antigo Architectmon foi desvinculado e liberado.");
            Assert(AppLinkService.ActiveLinkCount == 1, "Personagem mantém estritamente 1 único link ativo (Apenas 1 Link por personagem).");

            // Exclusividade 1-para-1: Não pode linkar o mesmo Appmon da Bag em outro personagem
            var go2 = new GameObject("TestFieldUnit2");
            var unit2 = go2.AddComponent<Unit>();
            var stats2 = go2.AddComponent<Stats>();
            stats2.InitializeStatsIfEmpty();
            unit2.stats = stats2;
            unit2.unitName = "Test-Unit-2";
            bool canLinkSame = AppLinkService.CanLink(unit2, glitchHound, out string exclusivityReason);
            Assert(!canLinkSame, "Não é permitido vincular um Appmon que já está linkado a outra unidade.");

            // Cada personagem em campo pode possuir seu respectivo 1 Link
            var dataViperBag = AppmonDatabase.Get("Data-Viper");
            bool linked2 = AppLinkService.ApplyLink(unit2, dataViperBag, out string linkErr2);
            Assert(linked2, "Segundo personagem do time vinculou seu próprio parceiro com sucesso.");
            Assert(AppLinkService.ActiveLinkCount == 2, "Exatamente 2 links ativos no time (1 por personagem).");

            var go3 = new GameObject("TestFieldUnit3");
            var unit3 = go3.AddComponent<Unit>();
            var stats3 = go3.AddComponent<Stats>();
            stats3.InitializeStatsIfEmpty();
            unit3.stats = stats3;
            unit3.unitName = "Test-Unit-3";
            var craftCraftBag = AppmonDatabase.Get("Craft-Craft");
            bool linked3 = AppLinkService.ApplyLink(unit3, craftCraftBag, out string linkErr3);
            Assert(linked3, "Terceiro combatente também pôde criar seu 1 Link individual.");
            Assert(AppLinkService.ActiveLinkCount == 3, "Três personagens do time possuem seu respectivo 1 Link.");

            // Remoção de link e restauração de atributos
            bool unlinked = AppLinkService.RemoveLink(unit1);
            Assert(unlinked, "unit1 desvinculada com sucesso.");
            Assert(!unit1.IsLinked, "unit1 não está mais vinculada.");
            Assert(!AppLinkService.IsAppmonLinked(glitchHound), "Glitch-Hound não está mais [LINKADO].");
            Assert(unit1.stats.GetStat(StatEnum.ATK) == atkBefore, "ATK restaurado ao valor original após desvincular.");
            Assert(AppLinkService.ActiveLinkCount == 2, "2 links ativos restantes para os outros combatentes.");

            // 6. Distribuição de XP Pós-Batalha (100% Campo, 50% Bag Linkada)
            bool xpReceived = false;
            int capturedFieldXp = 0;
            int capturedBagXp = 0;
            Action<int, int> xpHandler = (f, b) =>
            {
                xpReceived = true;
                capturedFieldXp = f;
                capturedBagXp = b;
            };
            AppLinkService.OnXpDistributed += xpHandler;
            AppLinkService.DistributeBattleXp(120);
            Assert(xpReceived && capturedFieldXp == 120 && capturedBagXp == 60, "XP pós-batalha distribui 100% (120 XP) para combatentes e 50% (60 XP) para parceiros vinculados na Bag.");
            AppLinkService.OnXpDistributed -= xpHandler;

            // Limpeza de testes
            AppLinkService.ResetAllLinks();
            Assert(AppLinkService.ActiveLinkCount == 0, "Todos os links resetados com sucesso.");
            GameObject.DestroyImmediate(go1);
            GameObject.DestroyImmediate(go2);
            GameObject.DestroyImmediate(go3);
            GameObject.DestroyImmediate(goSys);
        }

        public static void Test_BattleHUD_LinkBuffsPanel()
        {
            var hudGo = new GameObject("TestBattleHUD");
            var hud = hudGo.AddComponent<BattleHUD>();
            Assert(hud != null, "BattleHUD instanciado com sucesso.");

            var unitGo = new GameObject("TestHUDUnit");
            var unit = unitGo.AddComponent<Unit>();
            var stats = unitGo.AddComponent<Stats>();
            stats.InitializeStatsIfEmpty();
            unit.stats = stats;
            unit.unitName = "TestAgumon";
            unit.category = FunctionalCategory.Security;
            unit.team = Team.Player;
            stats.SetStat(StatEnum.ATK, 100);
            stats.SetStat(StatEnum.DEF, 80);

            // 1. Estado sem Link (painel deve estar oculto e botão de Link ativo)
            hud.UpdateTurnBanner(unit);
            hud.UpdateLinkBuffsPanel(unit);
            hud.UpdateActionMenuSelection(0, unit);
            Assert(!unit.IsLinked, "Unidade inicial não possui Link.");
            Assert(hud.linkBuffsPanel != null && !hud.linkBuffsPanel.activeSelf, "Painel de Link deve ficar OCULTO quando não há link.");
            Assert(hud.IsActionAvailable(4), "Botão de Link (ação 4) deve estar ativo quando a unidade não está vinculada e pode agir.");

            // 2. Estado com Link e Sinergia (painel deve ficar ativo e botão de Link desabilitado)
            var architect = AppmonDatabase.Get("Architectmon"); // Super, Security (Sinergia com Security!)
            bool linked = AppLinkService.ApplyLink(unit, architect, out string err);
            Assert(linked, "Link aplicado na unidade para teste de UI.");
            hud.UpdateTurnBanner(unit);
            hud.UpdateLinkBuffsPanel(unit);
            hud.UpdateActionMenuSelection(0, unit);
            Assert(unit.IsLinked, "Unidade agora está vinculada e o HUD reflete os buffs.");
            Assert(hud.linkBuffsPanel != null && hud.linkBuffsPanel.activeSelf, "Painel de Link deve ficar ATIVO quando um Link for feito.");
            Assert(!hud.IsActionAvailable(4), "Botão de Link (ação 4) deve ficar DESABILITADO (interactable = false) após o link ser feito.");

            // 2.5 Teste de Isolamento Estrito: O link de unit1 NÃO pode ser duplicado para unit2
            var unit2Go = new GameObject("TestHUDUnit2");
            var unit2 = unit2Go.AddComponent<Unit>();
            var stats2 = unit2Go.AddComponent<Stats>();
            stats2.InitializeStatsIfEmpty();
            unit2.stats = stats2;
            unit2.unitName = "TestCreature2";
            unit2.category = FunctionalCategory.Social;
            unit2.team = Team.Player;

            hud.UpdateTurnBanner(unit2);
            hud.UpdateLinkBuffsPanel(unit2);
            hud.UpdateActionMenuSelection(0, unit2);
            Assert(!unit2.IsLinked, "Segunda criatura NÃO possui Link.");
            Assert(hud.linkBuffsPanel != null && !hud.linkBuffsPanel.activeSelf, "Painel de Link da segunda criatura DEVE ficar OCULTO (o link da primeira não pode vazar).");
            Assert(hud.IsActionAvailable(4), "Botão de Link da segunda criatura DEVE estar ATIVO e liberado para fazer seu próprio link único.");

            // Validação de Reserva (Bag): O parceiro de unit1 (Architectmon) NÃO pode estar na Bag de unit2!
            var bagForUnit2 = AppLinkService.GetBagAppmons(unit2);
            Assert(!bagForUnit2.Exists(a => a.name == "Architectmon" || a.id == "Architectmon"), "Architectmon NÃO deve constar na reserva (Bag) de unit2 pois já está em uso por unit1.");

            // Validação de Bloqueio 1-para-1: unit2 não pode linkar ao mesmo parceiro
            bool canLinkSame = AppLinkService.CanLink(unit2, architect, out string blockReason);
            Assert(!canLinkSame, "unit2 NÃO pode conectar ao mesmo parceiro de unit1.");

            // Validação de Link Diferente: unit2 conecta ao seu próprio parceiro exclusivo (ex: Glitch-Hound)
            var glitchHound = AppmonDatabase.Get("Glitch-Hound");
            bool linked2 = AppLinkService.ApplyLink(unit2, glitchHound, out string err2);
            Assert(linked2, "unit2 conecta com sucesso ao Glitch-Hound como seu parceiro próprio.");
            Assert(unit.linkedBagAppmon.name == "Architectmon", "unit1 mantém Architectmon como parceiro exclusivo.");
            Assert(unit2.linkedBagAppmon.name == "Glitch-Hound", "unit2 possui Glitch-Hound como parceiro exclusivo.");
            Assert(unit.linkedBagAppmon != unit2.linkedBagAppmon, "Os parceiros de unit1 e unit2 são obrigatoriamente DIFERENTES!");

            AppLinkService.RemoveLink(unit2);
            GameObject.DestroyImmediate(unit2Go);

            // 3. Desvincular e verificar que o painel volta a ficar oculto e o botão volta a ficar ativo
            AppLinkService.RemoveLink(unit);
            hud.UpdateTurnBanner(unit);
            hud.UpdateLinkBuffsPanel(unit);
            hud.UpdateActionMenuSelection(0, unit);
            Assert(!unit.IsLinked, "Unidade desvinculada com sucesso e HUD atualizado.");
            Assert(hud.linkBuffsPanel != null && !hud.linkBuffsPanel.activeSelf, "Painel de Link volta a ficar OCULTO após desvincular.");
            Assert(hud.IsActionAvailable(4), "Botão de Link volta a ficar ativo após o link ser removido.");

            // Limpeza
            AppLinkService.ResetAllLinks();
            if (hud.canvas != null && hud.canvas.gameObject != hudGo)
            {
                GameObject.DestroyImmediate(hud.canvas.gameObject);
            }
            GameObject.DestroyImmediate(hudGo);
            GameObject.DestroyImmediate(unitGo);
        }

        // =========================================================================
        // 8. TESTES DE FUSÃO EM BATALHA (APP GAPPAI)
        // =========================================================================

        public static void Test_AppGappai_CompatibilityTable()
        {
            var viper = AppmonDatabase.Get("Data-Viper");
            var shitaku = AppmonDatabase.Get("Shitakumon");
            var glitch = AppmonDatabase.Get("Glitch-Hound");
            var sound = AppmonDatabase.Get("Sound-Beat");

            // 1. Validação de pares compatíveis
            bool ok1 = AppGappaiService.CheckCompatibility(viper, shitaku, out AppmonData res1, out string reason1);
            Assert(ok1 && res1 != null && res1.name == "Hydro-Vipermon" && res1.rank == EvolutionRank.Super,
                "Data-Viper + Shitakumon é COMPATÍVEL para Fusão em Hydro-Vipermon (Super).");

            bool ok2 = AppGappaiService.CheckCompatibility(glitch, sound, out AppmonData res2, out string reason2);
            Assert(ok2 && res2 != null && res2.name == "Sonic-Debugger" && res2.rank == EvolutionRank.Super,
                "Glitch-Hound + Sound-Beat é COMPATÍVEL para Fusão em Sonic-Debugger (Super).");

            // Ordem inversa dos pais também deve ser compatível
            bool okReverse = AppGappaiService.CheckCompatibility(shitaku, viper, out AppmonData resRev, out _);
            Assert(okReverse && resRev != null && resRev.name == "Hydro-Vipermon",
                "Ordem inversa (Shitakumon + Data-Viper) resulta identicamente em Hydro-Vipermon.");

            // Validação de par Magnet-Core + Sound-Beat (Incompatível pois não existe no Compêndio de Personagens)
            var magnet = AppmonDatabase.Get("Magnet-Core");
            bool okMagnetSound = AppGappaiService.CheckCompatibility(magnet, sound, out AppmonData resMag, out _);
            Assert(!okMagnetSound && resMag == null,
                "Magnet-Core + Sound-Beat é INCOMPATÍVEL para Fusão (segue estritamente o compêndio).");

            // Validação de fusão canônica de Magnet-Core: Magnet-Core + Bio-Patch = Bio-Magnetmon
            var bioPatch = AppmonDatabase.Get("Bio-Patch");
            bool okBioMag = AppGappaiService.CheckCompatibility(magnet, bioPatch, out AppmonData resBioMag, out _);
            Assert(okBioMag && resBioMag != null && resBioMag.name == "Bio-Magnetmon",
                "Magnet-Core + Bio-Patch é COMPATÍVEL para Fusão em Bio-Magnetmon (Super).");

            // Validação de fusão Ultimate canônica: Hydro-Vipermon + Architectmon = Poseidon-Vipermon
            var hydro = AppmonDatabase.Get("Hydro-Vipermon");
            var architect = AppmonDatabase.Get("Architectmon");
            bool okUltimate = AppGappaiService.CheckCompatibility(hydro, architect, out AppmonData resUlt, out _);
            Assert(okUltimate && resUlt != null && resUlt.name == "Poseidon-Vipermon" && resUlt.rank == EvolutionRank.Ultimate,
                "Hydro-Vipermon + Architectmon é COMPATÍVEL para Fusão em Poseidon-Vipermon (Ultimate).");

            // 2. Validação de pares incompatíveis (nada pode acontecer)
            bool okIncompat = AppGappaiService.CheckCompatibility(viper, glitch, out AppmonData resInc, out string reasonInc);
            Assert(!okIncompat && resInc == null,
                "Data-Viper + Glitch-Hound é INCOMPATÍVEL para fusão direta (nenhuma ação permitida).");

            // 3. Validação de mesmice (mesmo monstro não pode fundir consigo mesmo)
            bool okSame = AppGappaiService.CheckCompatibility(viper, viper, out _, out _);
            Assert(!okSame, "Mesmo Appmon não pode fundir consigo mesmo.");
        }

        public static void Test_AppGappai_TemporaryFusionExecution()
        {
            // Cria GameObjects de teste simulando a batalha
            GameObject bGo = new GameObject("BattleController_Test", typeof(BattleController));
            BattleController bCtrl = bGo.GetComponent<BattleController>();
            BattleController.Instance = bCtrl;

            GameObject u1Go = new GameObject("UnitA", typeof(Unit), typeof(Stats), typeof(AppmonCharacter));
            Unit u1 = u1Go.GetComponent<Unit>();
            u1.ApplyAppmon("Data-Viper");
            TileLogic tileA = new TileLogic(new Vector3Int(2, 2, 0), Vector3.zero, null, TerrainType.Standard);
            u1.PlaceAtTile(tileA);

            GameObject u2Go = new GameObject("UnitB", typeof(Unit), typeof(Stats), typeof(AppmonCharacter));
            Unit u2 = u2Go.GetComponent<Unit>();
            u2.ApplyAppmon("Shitakumon");
            TileLogic tileB = new TileLogic(new Vector3Int(3, 2, 0), new Vector3(1, 0, 0), null, TerrainType.Standard);
            u2.PlaceAtTile(tileB);

            bCtrl.RegisterUnit(u1);
            bCtrl.RegisterUnit(u2);
            bCtrl.currentUnit = u1;

            var hydroData = AppmonDatabase.Get("Hydro-Vipermon");
            Assert(hydroData != null, "Hydro-Vipermon deve existir no banco.");

            // Executa Fusão Temporária
            Unit fusedUnit = AppGappaiService.ExecuteFusion(u1, u2, hydroData);

            // Asserções
            Assert(fusedUnit != null, "Unidade fundida foi criada com sucesso.");
            Assert(fusedUnit.IsTemporaryFusion, "Flag IsTemporaryFusion DEVE ser verdadeira na criatura fundida.");
            Assert(fusedUnit.unitName == "Hydro-Vipermon", "Nome da unidade fundida corresponde ao Appmon resultante.");
            Assert(fusedUnit.rank == EvolutionRank.Super, "Rank da unidade fundida é Super.");
            Assert(!u1Go.activeSelf, "Monstro original A (Data-Viper) foi removido do campo.");
            Assert(!u2Go.activeSelf, "Monstro original B (Shitakumon) foi removido do campo.");
            Assert(tileA.content == fusedUnit.gameObject, "A unidade fundida ocupa o tile original da unidade A.");
            Assert(tileB.content == null, "O tile da unidade B foi devidamente desocupado.");

            // Limpeza
            AppGappaiService.RevertAllFusions();
            GameObject.DestroyImmediate(u1Go);
            GameObject.DestroyImmediate(u2Go);
            GameObject.DestroyImmediate(bGo);
        }

        public static void Test_AppGappai_BattleEndRestoration()
        {
            GameObject bGo = new GameObject("BattleController_Test2", typeof(BattleController));
            BattleController bCtrl = bGo.GetComponent<BattleController>();
            BattleController.Instance = bCtrl;

            GameObject u1Go = new GameObject("UnitA", typeof(Unit), typeof(Stats), typeof(AppmonCharacter));
            Unit u1 = u1Go.GetComponent<Unit>();
            u1.ApplyAppmon("Glitch-Hound");
            u1.stats.SetStat(StatEnum.HP, 80); // HP modificado antes da fusão
            TileLogic tileA = new TileLogic(new Vector3Int(1, 1, 0), Vector3.zero, null, TerrainType.Standard);
            u1.PlaceAtTile(tileA);

            GameObject u2Go = new GameObject("UnitB", typeof(Unit), typeof(Stats), typeof(AppmonCharacter));
            Unit u2 = u2Go.GetComponent<Unit>();
            u2.ApplyAppmon("Sound-Beat");
            u2.stats.SetStat(StatEnum.HP, 65);
            TileLogic tileB = new TileLogic(new Vector3Int(1, 2, 0), new Vector3(0, 1, 0), null, TerrainType.Standard);
            u2.PlaceAtTile(tileB);

            bCtrl.RegisterUnit(u1);
            bCtrl.RegisterUnit(u2);
            bCtrl.currentUnit = u1;

            var sonicData = AppmonDatabase.Get("Sonic-Debugger");
            Unit fusedUnit = AppGappaiService.ExecuteFusion(u1, u2, sonicData);

            Assert(AppGappaiService.ActiveFusions.Count == 1, "Existe 1 fusão ativa em combate.");
            Assert(fusedUnit != null && fusedUnit.IsTemporaryFusion, "Unidade fundida ativa.");

            // Simula fim de combate (RevertAllFusions)
            AppGappaiService.HandleBattleEnd(Team.Player);

            // Validações pós-batalha
            Assert(AppGappaiService.ActiveFusions.Count == 0, "Lista de fusões ativas foi esvaziada.");
            Assert(u1Go.activeSelf, "Monstro original A (Glitch-Hound) foi reativado no campo.");
            Assert(u2Go.activeSelf, "Monstro original B (Sound-Beat) foi reativado no campo.");
            Assert(u1.stats.GetStat(StatEnum.HP) == 80, "HP do monstro original A foi restaurado com exatidão.");
            Assert(u2.stats.GetStat(StatEnum.HP) == 65, "HP do monstro original B foi restaurado com exatidão.");
            Assert(u1.currentTile == tileA, "Monstro original A restaurado no seu tile correspondente.");
            Assert(u2.currentTile == tileB, "Monstro original B restaurado no seu tile correspondente.");

            // Limpeza
            GameObject.DestroyImmediate(u1Go);
            GameObject.DestroyImmediate(u2Go);
            GameObject.DestroyImmediate(bGo);
        }

        public static void Test_AppGappai_FieldLinkSimpleBonus()
        {
            GameObject u1Go = new GameObject("UnitA", typeof(Unit), typeof(Stats), typeof(AppmonCharacter));
            Unit u1 = u1Go.GetComponent<Unit>();
            u1.ApplyAppmon("Data-Viper");
            int initialAtk = u1.stats.GetStat(StatEnum.ATK);

            GameObject u2Go = new GameObject("UnitB", typeof(Unit), typeof(Stats), typeof(AppmonCharacter));
            Unit u2 = u2Go.GetComponent<Unit>();
            u2.ApplyAppmon("Glitch-Hound"); // Incompatível para fusão direta

            bool okLink = AppGappaiService.ExecuteFieldLink(u1, u2, out string summary);
            Assert(okLink, "Executou com sucesso o App-Link simples de campo.");
            Assert(u1.stats.GetStat(StatEnum.ATK) > initialAtk, "ATK de Unit A aumentou com o bônus temporário de App-Link.");
            Assert(u1.hasActed, "Unit A consumiu a ação do turno após receber o App-Link.");

            GameObject.DestroyImmediate(u1Go);
            GameObject.DestroyImmediate(u2Go);
        }

        public static void Test_WaterWall_PersistentLoopingBarrier5Turns()
        {
            // 1. Identificação de Habilidade Water Wall e Parede de Água
            var sk1 = new SkillData { id = "water_wall", skillName = "Water Wall" };
            var sk2 = new SkillData { id = "water_wall", skillName = "Parede de Água" };
            var sk3 = new SkillData { id = "water_wall", skillName = "Parede de Agua" };
            var skOther = new SkillData { id = "water_jet", skillName = "Water Jet" };

            Assert(WaterWallService.IsWaterWallSkill(sk1), "WaterWallService identifica 'Water Wall'.");
            Assert(WaterWallService.IsWaterWallSkill(sk2), "WaterWallService identifica 'Parede de Água'.");
            Assert(WaterWallService.IsWaterWallSkill(sk3), "WaterWallService identifica 'Parede de Agua'.");
            Assert(!WaterWallService.IsWaterWallSkill(skOther), "WaterWallService não confunde Water Jet com Water Wall.");

            // 2. Validação dos dados da habilidade no compêndio de Shitakumon
            var shitakumon = AppmonDatabase.Get("Shitakumon");
            Assert(shitakumon != null, "Shitakumon está registrado no banco de dados.");
            var wallSkill = shitakumon.skills.Find(s => s.id == "water_wall");
            Assert(wallSkill != null, "Shitakumon possui a habilidade water_wall.");
            Assert(wallSkill.minRange == 0, "Water Wall tem minRange = 0 para permitir conjuração em volta do próprio personagem.");
            Assert(wallSkill.statusDurationTurns == 4, "Water Wall está configurado para 4 turnos de duração.");
            Assert(wallSkill.aoeRadius == 1, "Water Wall possui raio de área 1 (cobre 3x3 tiles).");
            Assert(wallSkill.hasTerrainCreation, "Water Wall cria terreno.");
            Assert(wallSkill.createsTerrain == TerrainType.Flooded, "Water Wall transforma em Terreno Alagado (Flooded).");
            Assert(wallSkill.attackVfxName == "WaterSpell2", "Water Wall mapeia para WaterSpell2.");

            // 3. Validação do ciclo de vida da Barreira de Água (4 turnos e desaparecimento)
            GameObject uGo = new GameObject("CasterUnit", typeof(Unit), typeof(Stats));
            Unit u = uGo.GetComponent<Unit>();
            u.unitName = "Shitakumon";

            WaterWallBarrier barrier = WaterWallService.CreateWaterWallBarrier(u, u, wallSkill, 4);
            Assert(barrier != null, "Barreira de Água instanciada com sucesso.");
            Assert(barrier.remainingTurns == 4, "Barreira inicia com exatamente 4 turnos ativos.");
            Assert(!barrier.isExpiring, "Barreira não está em estado de expiração no início.");

            // Simula contagem regressiva de 4 turnos
            barrier.Renew(4);
            for (int i = 4; i > 1; i--)
            {
                barrier.remainingTurns--;
                Assert(barrier.remainingTurns == i - 1, $"Turno consumido: restam {barrier.remainingTurns} turnos.");
            }
            barrier.remainingTurns--;
            Assert(barrier.remainingTurns == 0, "Após 4 turnos consumidos, o contador chega a 0.");
            barrier.Expire();
            Assert(barrier.isExpiring, "Ao zerar os 4 turnos, a barreira entra no modo de expiração e desaparece.");

            // Limpeza
            WaterWallService.ClearAllBarriers();
            GameObject.DestroyImmediate(uGo);
        }
    }
}
