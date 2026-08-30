using System;
using System.Collections.Generic;
using TacticalBattle.Attribute;
using TacticalBattle.Combat;
using TacticalBattle.Core;
using TacticalBattle.Core.EventSystem;
using TacticalBattle.Evolution;
using TacticalBattle.Grid;
using TacticalBattle.Integration;
using TacticalBattle.Talk;
using UnityEngine;

namespace TacticalBattle.Tests
{
    public static class TacticalBattleTestSuite
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
            Debug.Log("INICIANDO SUÍTE DE TESTES DO SISTEMA TÁTICO 2D");
            Debug.Log("=================================================");

            // 1. TESTES DO GRID & PATHFINDING
            Test_Grid_ReachableCells_MovementBudgetZero(); passed++;
            Test_Grid_CannotMoveToOccupiedCell(); passed++;
            Test_Grid_HeightDifferenceBlocksMovement(); passed++;
            Test_Terrain_MovementCostAndImpassability(); passed++;
            Test_Terrain_DefenseReduction(); passed++;

            // 2. TESTES DE ATRIBUTOS
            Test_Attributes_StrictAsymmetry(); passed++;
            Test_Attributes_FreeIsAlwaysNeutral(); passed++;

            // 3. TESTES DE EVOLUÇÃO E SP
            Test_Evolution_RookieNeverConsumesSP(); passed++;
            Test_Evolution_SPNeverNegative(); passed++;
            Test_Evolution_ForcedDevolutionAtZeroSP(); passed++;
            Test_Evolution_DevolutionBeforeActions(); passed++;
            Test_Evolution_CannotEvolveTwiceInSameTurn(); passed++;
            Test_Evolution_CleanStatsAfterReversionNoPhantomStats(); passed++;

            // 4. TESTES DE COMBATE POSICIONAL
            Test_Positional_All16FacingCombinations(); passed++;
            Test_Positional_BackstabGuaranteesCritAndIgnoresGuard(); passed++;
            Test_Combat_FinalDamageNeverNegative(); passed++;
            Test_Combat_ElevationNonPositiveNeverGivesBonus(); passed++;

            // 5. TESTES DO TALK SYSTEM
            Test_Talk_SessionEndsExactlyAt3Questions(); passed++;
            Test_Talk_AffinityClamping0To100(); passed++;
            Test_Talk_RecruitedOnlyAboveThresholdAtResolution(); passed++;

            // 6. TESTES DE INTEGRAÇÃO DO TURNO
            Test_Integration_TurnPhasesAndBudgetValidation(); passed++;

            Debug.Log("=================================================");
            Debug.Log($"TODOS OS {passed} TESTES OBRIGATÓRIOS FORAM APROVADOS COM SUCESSO!");
            Debug.Log("=================================================");

            return passed;
        }

        // =========================================================================
        // 1. TESTES DE GRID
        // =========================================================================
        public static void Test_Grid_ReachableCells_MovementBudgetZero()
        {
            var grid = new GridState { width = 5, height = 5 };
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid.cells[GridState.Key(x, y)] = new GridCell(x, y, 0);

            var origin = new GridCoord(2, 2, 0);
            var reachable = PathfindingService.ComputeReachableCells(origin, 0, grid);

            Assert(reachable.Count == 1 && reachable[0] == origin,
                "Grid: computeReachableCells com budget 0 retorna apenas a célula de origem.");
        }

        public static void Test_Grid_CannotMoveToOccupiedCell()
        {
            var grid = new GridState { width = 5, height = 5 };
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid.cells[GridState.Key(x, y)] = new GridCell(x, y, 0);

            var origin = new GridCoord(1, 1, 0);
            var occupiedCoord = new GridCoord(1, 2, 0);

            // Ocupa célula adjacente
            grid.SetOccupant(occupiedCoord, "enemy_unit_1");

            var reachable = PathfindingService.ComputeReachableCells(origin, 2, grid, currentUnitId: "player_1");

            Assert(!reachable.Contains(occupiedCoord),
                "Grid: Unidade não pode ter como destino final uma célula ocupada por outra unidade.");
        }

        public static void Test_Grid_HeightDifferenceBlocksMovement()
        {
            var grid = new GridState { width = 5, height = 5 };
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid.cells[GridState.Key(x, y)] = new GridCell(x, y, 0);

            // Célula vizinha com altura z=3 (limite de escalada padrão = 1)
            grid.cells[GridState.Key(2, 1)].coord.z = 3;

            var origin = new GridCoord(1, 1, 0);
            var reachable = PathfindingService.ComputeReachableCells(origin, 1, grid, maxClimbHeight: 1);

            Assert(!reachable.Contains(new GridCoord(2, 1, 3)),
                "Grid: Diferença de altura acima do limite de escalada (z=3 vs z=0) bloqueia movimento.");
        }

        public static void Test_Terrain_MovementCostAndImpassability()
        {
            var grid = new GridState { width = 5, height = 5 };
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid.cells[GridState.Key(x, y)] = new GridCell(x, y, 0, terrainCost: 1, isWalkable: true);

            // Célula (1, 2) é Barricada (Custo MOV = 2)
            grid.cells[GridState.Key(1, 2)].terrainCost = 2;

            // Célula (2, 1) é Abismo/Chasm (isWalkable = false)
            grid.cells[GridState.Key(2, 1)].isWalkable = false;

            var origin = new GridCoord(1, 1, 0);

            // Com budget = 1, não pode alcançar a Barricada em (1, 2) que custa 2 nem o Abismo em (2, 1)
            var reachable1 = PathfindingService.ComputeReachableCells(origin, 1, grid);
            Assert(!reachable1.Contains(new GridCoord(1, 2, 0)), "Terreno: Barricada com custo 2 não é alcançável com budget 1.");
            Assert(!reachable1.Contains(new GridCoord(2, 1, 0)), "Terreno: Abismo (isWalkable=false) é intransponível.");

            // Com budget = 2, pode alcançar a Barricada em (1, 2)
            var reachable2 = PathfindingService.ComputeReachableCells(origin, 2, grid);
            Assert(reachable2.Contains(new GridCoord(1, 2, 0)), "Terreno: Barricada é alcançável com budget 2.");
        }

        public static void Test_Terrain_DefenseReduction()
        {
            var barricade = TerrainDatabase.Get(TerrainType.Barricade);
            Assert(barricade.defenseBonusPercent == 0.20f && barricade.movementCost == 2, "Terreno: Catálogo possui Barricada com +20% de defesa e custo 2.");

            int damageWithoutCover = DamageCalculator.ComputeFinalDamageDetailed(
                baseDamage: 100,
                attackerAttribute: TacticalAttribute.Free,
                defenderAttribute: TacticalAttribute.Free,
                position: RelativeCombatPosition.Front,
                attackerZ: 0,
                defenderZ: 0,
                isDefenderGuarding: false,
                terrainDefenseReduction: 0f
            );

            int damageWithCover = DamageCalculator.ComputeFinalDamageDetailed(
                baseDamage: 100,
                attackerAttribute: TacticalAttribute.Free,
                defenderAttribute: TacticalAttribute.Free,
                position: RelativeCombatPosition.Front,
                attackerZ: 0,
                defenderZ: 0,
                isDefenderGuarding: false,
                terrainDefenseReduction: barricade.defenseBonusPercent
            );

            Assert(damageWithoutCover == 100 && damageWithCover == 80,
                "Terreno: Redução de 20% de dano por cobertura de Barricada aplicada corretamente (100 -> 80).");
        }

        // =========================================================================
        // 2. TESTES DE ATRIBUTOS
        // =========================================================================
        public static void Test_Attributes_StrictAsymmetry()
        {
            // Vaccine > Virus > Data > Vaccine
            float vacVsVir = AttributeModule.GetAttributeMultiplier(TacticalAttribute.Vaccine, TacticalAttribute.Virus);
            float virVsVac = AttributeModule.GetAttributeMultiplier(TacticalAttribute.Virus, TacticalAttribute.Vaccine);

            float virVsDat = AttributeModule.GetAttributeMultiplier(TacticalAttribute.Virus, TacticalAttribute.Data);
            float datVsVir = AttributeModule.GetAttributeMultiplier(TacticalAttribute.Data, TacticalAttribute.Virus);

            float datVsVac = AttributeModule.GetAttributeMultiplier(TacticalAttribute.Data, TacticalAttribute.Vaccine);
            float vacVsDat = AttributeModule.GetAttributeMultiplier(TacticalAttribute.Vaccine, TacticalAttribute.Data);

            Assert(vacVsVir == 1.50f && virVsVac == 0.75f, "Atributos: Assimetria estrita Vaccine (+50%) vs Virus (-25%).");
            Assert(virVsDat == 1.50f && datVsVir == 0.75f, "Atributos: Assimetria estrita Virus (+50%) vs Data (-25%).");
            Assert(datVsVac == 1.50f && vacVsDat == 0.75f, "Atributos: Assimetria estrita Data (+50%) vs Vaccine (-25%).");
        }

        public static void Test_Attributes_FreeIsAlwaysNeutral()
        {
            var attributes = new[] { TacticalAttribute.Vaccine, TacticalAttribute.Data, TacticalAttribute.Virus, TacticalAttribute.Free };
            bool allNeutral = true;

            foreach (var attr in attributes)
            {
                if (AttributeModule.GetAttributeMultiplier(TacticalAttribute.Free, attr) != 1.0f) allNeutral = false;
                if (AttributeModule.GetAttributeMultiplier(attr, TacticalAttribute.Free) != 1.0f) allNeutral = false;
            }

            Assert(allNeutral, "Atributos: Atributo FREE nunca recebe nem concede multiplicador diferente de 1.0.");
        }

        // =========================================================================
        // 3. TESTES DE EVOLUÇÃO E SP
        // =========================================================================
        public static void Test_Evolution_RookieNeverConsumesSP()
        {
            var unit = new UnitState();
            unit.evolution.currentTier = EvolutionTier.Rookie;
            unit.evolution.currentSP = 50;

            EvolutionModule.OnUnitTurnStart(unit);

            Assert(unit.evolution.spCostPerTurn[EvolutionTier.Rookie] == 0 && unit.evolution.currentSP == 50,
                "Evolução: Invariante garantida — forma Rookie NUNCA consome SP (custo = 0).");
        }

        public static void Test_Evolution_SPNeverNegative()
        {
            var unit = new UnitState();
            unit.evolution.currentTier = EvolutionTier.Champion;
            unit.evolution.currentSP = 5; // Custo é 10

            EvolutionModule.OnUnitTurnStart(unit);

            Assert(unit.evolution.currentSP == 0,
                "Evolução: SP nunca fica negativo após consumo e devolução (SP = 0).");
        }

        public static void Test_Evolution_ForcedDevolutionAtZeroSP()
        {
            var unit = new UnitState();
            unit.evolution.currentTier = EvolutionTier.Champion;
            unit.evolution.currentSP = 10; // Custo exato de 10 -> SP fica 0

            EvolutionModule.OnUnitTurnStart(unit);

            Assert(unit.evolution.currentTier == EvolutionTier.Rookie && unit.evolution.currentSP == 0,
                "Evolução: Reversão automática para Rookie dispara exatamente quando currentSP <= 0.");
        }

        public static void Test_Evolution_DevolutionBeforeActions()
        {
            var controller = new TacticalBattleController();
            var unit = new UnitState { id = "u1", name = "Agumon" };
            unit.evolution.currentTier = EvolutionTier.Champion;
            unit.evolution.currentSP = 5; // Não aguenta o turno
            controller.State.units.Add(unit);

            controller.ExecuteUnitTurnStart("u1");

            Assert(unit.evolution.currentTier == EvolutionTier.Rookie,
                "Evolução: Reversão automática ocorre no início do turno, antes de qualquer ação ser liberada.");
        }

        public static void Test_Evolution_CannotEvolveTwiceInSameTurn()
        {
            var unit = new UnitState();
            unit.evolution.currentTier = EvolutionTier.Rookie;
            unit.evolution.currentSP = 50;
            unit.canEvolveThisTurn = true;

            bool firstEvolve = EvolutionModule.TryEvolve(unit, EvolutionTier.Champion);
            bool secondEvolve = EvolutionModule.TryEvolve(unit, EvolutionTier.Ultimate);

            Assert(firstEvolve == true && secondEvolve == false,
                "Evolução: Unidade não pode evoluir duas vezes no mesmo turno.");
        }

        public static void Test_Evolution_CleanStatsAfterReversionNoPhantomStats()
        {
            var unit = new UnitState { speciesId = "TestRookie" };
            EvolutionStatsTable.ApplyBaseStatsForTier(unit, EvolutionTier.Rookie);

            int baseRookieAtk = unit.attack;
            int baseRookieDef = unit.defense;

            // Evolui para Mega
            EvolutionStatsTable.ApplyBaseStatsForTier(unit, EvolutionTier.Mega);
            Assert(unit.attack > baseRookieAtk, "Stats aumentaram na evolução para Mega.");

            // Reverte para Rookie
            EvolutionModule.RevertToRookie(unit);

            Assert(unit.attack == baseRookieAtk && unit.defense == baseRookieDef,
                "Evolução: Stats pós-reversão correspondem exatamente à tabela base de Rookie sem stats fantasmas.");
        }

        // =========================================================================
        // 4. TESTES DE COMBATE POSICIONAL
        // =========================================================================
        public static void Test_Positional_All16FacingCombinations()
        {
            // Testar as 16 combinações (4 facings do defensor x 4 posições relativas do atacante)
            var defenderCoord = new GridCoord(2, 2, 0);

            var northAttacker = new GridCoord(2, 3, 0); // Atacante vindo do Norte (+Y)
            var southAttacker = new GridCoord(2, 1, 0); // Atacante vindo do Sul (-Y)
            var eastAttacker = new GridCoord(3, 2, 0);  // Atacante vindo do Leste (+X)
            var westAttacker = new GridCoord(1, 2, 0);  // Atacante vindo do Oeste (-X)

            // 1. Defensor olhando para o NORTE
            Assert(PositionalCombatModule.GetRelativePositionGridBased(northAttacker, GridFacing.North, defenderCoord) == RelativeCombatPosition.Front, "Posição: North vs North = Front");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(southAttacker, GridFacing.North, defenderCoord) == RelativeCombatPosition.Back, "Posição: North vs South = Back");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(eastAttacker, GridFacing.North, defenderCoord) == RelativeCombatPosition.Side, "Posição: North vs East = Side");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(westAttacker, GridFacing.North, defenderCoord) == RelativeCombatPosition.Side, "Posição: North vs West = Side");

            // 2. Defensor olhando para o SUL
            Assert(PositionalCombatModule.GetRelativePositionGridBased(southAttacker, GridFacing.South, defenderCoord) == RelativeCombatPosition.Front, "Posição: South vs South = Front");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(northAttacker, GridFacing.South, defenderCoord) == RelativeCombatPosition.Back, "Posição: South vs North = Back");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(eastAttacker, GridFacing.South, defenderCoord) == RelativeCombatPosition.Side, "Posição: South vs East = Side");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(westAttacker, GridFacing.South, defenderCoord) == RelativeCombatPosition.Side, "Posição: South vs West = Side");

            // 3. Defensor olhando para o LESTE
            Assert(PositionalCombatModule.GetRelativePositionGridBased(eastAttacker, GridFacing.East, defenderCoord) == RelativeCombatPosition.Front, "Posição: East vs East = Front");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(westAttacker, GridFacing.East, defenderCoord) == RelativeCombatPosition.Back, "Posição: East vs West = Back");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(northAttacker, GridFacing.East, defenderCoord) == RelativeCombatPosition.Side, "Posição: East vs North = Side");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(southAttacker, GridFacing.East, defenderCoord) == RelativeCombatPosition.Side, "Posição: East vs South = Side");

            // 4. Defensor olhando para o OESTE
            Assert(PositionalCombatModule.GetRelativePositionGridBased(westAttacker, GridFacing.West, defenderCoord) == RelativeCombatPosition.Front, "Posição: West vs West = Front");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(eastAttacker, GridFacing.West, defenderCoord) == RelativeCombatPosition.Back, "Posição: West vs East = Back");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(northAttacker, GridFacing.West, defenderCoord) == RelativeCombatPosition.Side, "Posição: West vs North = Side");
            Assert(PositionalCombatModule.GetRelativePositionGridBased(southAttacker, GridFacing.West, defenderCoord) == RelativeCombatPosition.Side, "Posição: West vs South = Side");
        }

        public static void Test_Positional_BackstabGuaranteesCritAndIgnoresGuard()
        {
            var res = PositionalCombatModule.ResolvePositionalDamage(RelativeCombatPosition.Back, 100);

            Assert(res.positionalMultiplier == 1.50f && res.isCriticalGuaranteed && res.ignoresGuardStance,
                "Combate Posicional: Multiplicador BACK (1.50x) sempre garante crítico e ignora guarda.");
        }

        public static void Test_Combat_FinalDamageNeverNegative()
        {
            int damage = DamageCalculator.ComputeFinalDamage(
                baseDamage: 0,
                attackerAttribute: TacticalAttribute.Virus,
                defenderAttribute: TacticalAttribute.Vaccine, // Desvantagem 0.75x
                position: RelativeCombatPosition.Front,
                attackerZ: 0,
                defenderZ: 2
            );

            Assert(damage >= 0, "Combate: computeFinalDamage nunca retorna valor negativo.");
        }

        public static void Test_Combat_ElevationNonPositiveNeverGivesBonus()
        {
            float sameHeightMult = PositionalCombatModule.GetElevationMultiplier(2, 2);
            float lowerHeightMult = PositionalCombatModule.GetElevationMultiplier(1, 3);

            Assert(sameHeightMult == 1.0f && lowerHeightMult <= 1.0f,
                "Combate: Altura atacante <= defensor nunca gera multiplicador maior que 1.0x.");
        }

        // =========================================================================
        // 5. TESTES DE TALK
        // =========================================================================
        public static void Test_Talk_SessionEndsExactlyAt3Questions()
        {
            var target = new TalkTarget
            {
                unitId = "enemy_1",
                personality = PersonalityTrait.Brave,
                affinity = 0f
            };

            var session = TalkModule.StartSession(target);
            Assert(session.phase == TalkPhase.Question1, "Talk: Inicia em Question1");

            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Brave);
            Assert(session.phase == TalkPhase.Question2 && target.questionsAnswered == 1, "Talk: Avança para Question2");

            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Brave);
            Assert(session.phase == TalkPhase.Question3 && target.questionsAnswered == 2, "Talk: Avança para Question3");

            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Brave);
            Assert(session.phase == TalkPhase.Resolution && target.questionsAnswered == 3,
                "Talk: Sessão termina exatamente após 3 perguntas na fase de Resolução.");
        }

        public static void Test_Talk_AffinityClamping0To100()
        {
            var target = new TalkTarget
            {
                unitId = "enemy_2",
                personality = PersonalityTrait.Kind,
                affinity = 90f
            };

            var session = TalkModule.StartSession(target);
            // Ganho de afinidade (+25) excederia 100
            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Kind);
            Assert(session.target.affinity == 100f, "Talk: Clamping no limite superior (100).");

            // Penalidade múltipla até zero
            session.target.affinity = 10f;
            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Aggressive); // -15
            Assert(session.target.affinity == 0f, "Talk: Clamping no limite inferior (0).");
        }

        public static void Test_Talk_RecruitedOnlyAboveThresholdAtResolution()
        {
            var target = new TalkTarget
            {
                unitId = "enemy_3",
                personality = PersonalityTrait.Logical,
                affinity = 20f,
                affinityThresholdToRecruit = 70f
            };

            var session = TalkModule.StartSession(target);
            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Logical); // +25 -> 45
            Assert(session.result == TalkResult.Pending, "Talk: Intermediário não resolve recrutamento");

            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Logical); // +25 -> 70
            session = TalkModule.AnswerQuestion(session, PersonalityTrait.Logical); // +25 -> 95

            Assert(session.result == TalkResult.Recruited,
                "Talk: RECRUITED ocorre com sucesso na resolução com afinidade >= threshold.");
        }

        // =========================================================================
        // 6. TESTES DE INTEGRAÇÃO DO TURNO
        // =========================================================================
        public static void Test_Integration_TurnPhasesAndBudgetValidation()
        {
            var controller = new TacticalBattleController();
            var unit = new UnitState
            {
                id = "hero",
                name = "HeroUnit",
                coord = new GridCoord(0, 0, 0),
                attribute = TacticalAttribute.Vaccine,
                movementBudget = 3
            };
            controller.State.units.Add(unit);

            // Inicializa turno
            controller.ExecuteUnitTurnStart("hero");
            Assert(controller.CurrentBudget.canMove && controller.CurrentBudget.canUseMainAction,
                "Integração: Orçamento completo liberado no início do turno.");

            // Cria inimigo
            var enemy = new UnitState
            {
                id = "enemy",
                name = "EnemyUnit",
                coord = new GridCoord(0, 1, 0),
                hp = 100,
                attribute = TacticalAttribute.Virus
            };
            controller.State.units.Add(enemy);

            // Executa ataque
            int dmg = controller.ExecuteAttack("hero", "enemy", 30);
            Assert(dmg > 0, "Integração: Ataque causou dano.");
            Assert(!controller.CurrentBudget.canUseMainAction, "Integração: canUseMainAction consumido.");

            // Tenta atacar novamente no mesmo turno (deve ser bloqueado pelo budget)
            int secondDmg = controller.ExecuteAttack("hero", "enemy", 30);
            Assert(secondDmg == 0, "Integração: Segundo ataque bloqueado no mesmo turno.");

            // Encerra turno
            controller.ExecuteUnitTurnEnd("hero");
        }
    }
}
