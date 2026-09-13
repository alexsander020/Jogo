using System;
using System.Collections.Generic;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Appmon
{
    [Serializable]
    public class AppGappaiRecipe
    {
        public string parentA;
        public string parentB;
        public string resultId;
        public EvolutionRank resultRank;
        public string fusionName;
        public string description;

        public AppGappaiRecipe(string parentA, string parentB, string resultId, EvolutionRank rank, string fusionName, string description)
        {
            this.parentA = parentA;
            this.parentB = parentB;
            this.resultId = resultId;
            this.resultRank = rank;
            this.fusionName = fusionName;
            this.description = description;
        }

        public bool Matches(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            bool match1 = a.Equals(parentA, StringComparison.OrdinalIgnoreCase) && b.Equals(parentB, StringComparison.OrdinalIgnoreCase);
            bool match2 = a.Equals(parentB, StringComparison.OrdinalIgnoreCase) && b.Equals(parentA, StringComparison.OrdinalIgnoreCase);
            return match1 || match2;
        }
    }

    [Serializable]
    public class TemporaryFusionRecord
    {
        public Unit originalUnitA;
        public Unit originalUnitB;
        public TileLogic tileA;
        public TileLogic tileB;
        public Vector3Int posA;
        public Vector3Int posB;

        public int hpA, maxHpA, mpA, spA;
        public int hpB, maxHpB, mpB, spB;

        public Unit fusedUnit;
        public AppmonData fusedData;
        public DateTime fusionTimestamp;
    }

    public static class AppGappaiDatabase
    {
        private static readonly List<AppGappaiRecipe> recipes = new List<AppGappaiRecipe>();
        private static bool isInitialized = false;

        static AppGappaiDatabase()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (isInitialized) return;
            recipes.Clear();

            // =========================================================================
            // 1. RECEITAS DE APPMON SUPER (Standard + Standard = Super)
            // =========================================================================
            // Canônicas
            Register("Data-Viper", "Shitakumon", "Hydro-Vipermon", EvolutionRank.Super,
                "Fusão Aquática de Segurança", "Combina algoritmos de firewall e purificação digital.");

            Register("Glitch-Hound", "Sound-Beat", "Sonic-Debugger", EvolutionRank.Super,
                "Fusão Ressonante de Depuração", "Combina varredura de erros com frequência acústica purificadora.");

            Register("Craft-Craft", "Data-Viper", "Architectmon", EvolutionRank.Super,
                "Fusão Estrutural de Fortificação", "Ergue muralhas blindadas com código inquebrável.");

            Register("Flame-Log", "Craft-Craft", "Magma-Logmon", EvolutionRank.Super,
                "Fusão Ígnea de Construção", "Molda estruturas em código incandescente vulcânico.");

            Register("Volt-Plug", "Shadow-Cam", "Electro-Cammon", EvolutionRank.Super,
                "Fusão Elétrica de Vigilância", "Sentinela de alta voltagem com sensores infravermelhos.");

            Register("Bio-Patch", "Magnet-Core", "Bio-Magnetmon", EvolutionRank.Super,
                "Fusão Eletromagnética Vital", "União de pulso magnético com algoritmos de restauração celular.");

            // =========================================================================
            // 2. RECEITAS DE APPMON ULTIMATE (Super + Super = Ultimate)
            // =========================================================================
            Register("Hydro-Vipermon", "Architectmon", "Poseidon-Vipermon", EvolutionRank.Ultimate,
                "Fusão Abissal Suprema", "Soberano dos mares digitais capaz de submergir o campo de batalha.");

            Register("Sonic-Debugger", "Electro-Cammon", "Omega-Debugger", EvolutionRank.Ultimate,
                "Fusão Ótima de Sistema", "Ápice da depuração global capaz de purificar qualquer setor.");

            Register("Architectmon", "Magma-Logmon", "Dreadnoughtmon", EvolutionRank.Ultimate,
                "Fusão Baluarte Titânica", "Fortaleza ambulante imune a qualquer efeito de controle.");

            isInitialized = true;
        }

        public static void Register(string parentA, string parentB, string resultId, EvolutionRank rank, string fusionName, string description)
        {
            recipes.Add(new AppGappaiRecipe(parentA, parentB, resultId, rank, fusionName, description));
        }

        public static AppGappaiRecipe FindRecipe(string nameA, string nameB)
        {
            Initialize();
            foreach (var r in recipes)
            {
                if (r.Matches(nameA, nameB)) return r;
            }
            return null;
        }

        public static List<AppGappaiRecipe> GetAllRecipes()
        {
            Initialize();
            return new List<AppGappaiRecipe>(recipes);
        }
    }

    public static class AppGappaiService
    {
        private static readonly List<TemporaryFusionRecord> activeFusions = new List<TemporaryFusionRecord>();
        private static bool isListeningToBattleEnd = false;

        public static IReadOnlyList<TemporaryFusionRecord> ActiveFusions => activeFusions;

        static AppGappaiService()
        {
            EnsureBattleEndHook();
        }

        public static void EnsureBattleEndHook()
        {
            if (isListeningToBattleEnd) return;
            if (BattleController.Instance != null)
            {
                BattleController.Instance.OnBattleEnd += HandleBattleEnd;
                isListeningToBattleEnd = true;
            }
        }

        // =========================================================================
        // VALIDAÇÃO E COMPATIBILIDADE
        // =========================================================================

        public static bool CheckCompatibility(AppmonData mainAppmon, AppmonData partnerAppmon, out AppmonData fusionResult, out string reason)
        {
            fusionResult = null;
            if (mainAppmon == null || partnerAppmon == null)
            {
                reason = "Appmon inválido ou nulo.";
                return false;
            }

            if (mainAppmon.id.Equals(partnerAppmon.id, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Não é possível fundir uma criatura consigo mesma.";
                return false;
            }

            // 1. Consulta receita na Tabela de Compatibilidade dedicada
            AppGappaiRecipe recipe = AppGappaiDatabase.FindRecipe(mainAppmon.id, partnerAppmon.id) 
                                     ?? AppGappaiDatabase.FindRecipe(mainAppmon.name, partnerAppmon.name);

            if (recipe != null)
            {
                fusionResult = AppmonDatabase.Get(recipe.resultId);
                if (fusionResult != null)
                {
                    reason = $"Compatibilidade Total: {recipe.fusionName} ➔ {fusionResult.name} ({fusionResult.rank})";
                    return true;
                }
            }

            // 2. Fallback de consulta no Compêndio Central
            var fallback = AppmonDatabase.FindFusion(mainAppmon.id, partnerAppmon.id)
                           ?? AppmonDatabase.FindFusion(mainAppmon.name, partnerAppmon.name);

            if (fallback != null)
            {
                fusionResult = fallback;
                reason = $"Compatibilidade de Linhagem: ➔ {fusionResult.name} ({fusionResult.rank})";
                return true;
            }

            // 3. Incompatível para Fusão (Nenhuma ação permitida segundo o compêndio)
            reason = "Incompatível para Fusão. Nenhuma combinação cadastrada no Compêndio de Personagens.";
            return false;
        }

        // =========================================================================
        // EXECUÇÃO DA FUSÃO TEMPORÁRIA (APP GAPPAI)
        // =========================================================================

        public static Unit ExecuteFusion(Unit mainUnit, Unit partnerUnit, AppmonData fusedData)
        {
            if (mainUnit == null || partnerUnit == null || fusedData == null)
            {
                Debug.LogError("[AppGappaiService] Falha ao executar fusão: parâmetros nulos!");
                return null;
            }

            EnsureBattleEndHook();

            TileLogic tileA = mainUnit.currentTile;
            TileLogic tileB = partnerUnit.currentTile;

            if (tileA == null)
            {
                Debug.LogError($"[AppGappaiService] Monstro do turno {mainUnit.unitName} não possui tile associado!");
                return null;
            }

            // 1. Snapshot dos dois monstros originais antes de qualquer modificação
            var record = new TemporaryFusionRecord
            {
                originalUnitA = mainUnit,
                originalUnitB = partnerUnit,
                tileA = tileA,
                tileB = tileB,
                posA = tileA.pos,
                posB = tileB != null ? tileB.pos : tileA.pos,
                hpA = mainUnit.stats != null ? mainUnit.stats.GetStat(StatEnum.HP) : 100,
                maxHpA = mainUnit.stats != null ? mainUnit.stats.GetStat(StatEnum.MaxHp) : 100,
                mpA = mainUnit.stats != null ? mainUnit.stats.GetStat(StatEnum.MP) : 50,
                spA = mainUnit.stats != null ? mainUnit.stats.GetStat(StatEnum.SP) : 50,
                hpB = partnerUnit.stats != null ? partnerUnit.stats.GetStat(StatEnum.HP) : 100,
                maxHpB = partnerUnit.stats != null ? partnerUnit.stats.GetStat(StatEnum.MaxHp) : 100,
                mpB = partnerUnit.stats != null ? partnerUnit.stats.GetStat(StatEnum.MP) : 50,
                spB = partnerUnit.stats != null ? partnerUnit.stats.GetStat(StatEnum.SP) : 50,
                fusedData = fusedData,
                fusionTimestamp = DateTime.Now
            };

            // 2. Remoção dos dois monstros do campo (tiles e registros)
            if (tileA.content == mainUnit.gameObject)
            {
                tileA.content = null;
            }
            if (tileB != null && tileB.content == partnerUnit.gameObject)
            {
                tileB.content = null;
            }

            Team unitTeam = mainUnit.team;
            FacingDirection unitFacing = mainUnit.facing;
            Transform parentTransform = mainUnit.transform.parent;
            Vector3 spawnWorldPos = tileA.worldPos;
            bool wasMoved = mainUnit.hasMoved;

            Unit fusedUnit = null;

            // 3. Spawna a nova unidade fundida com modelo e componentes apropriados
            if (MapLoader.instance != null)
            {
                Transform holder = MapLoader.instance.GetUnitsHolder() != null ? MapLoader.instance.GetUnitsHolder().transform : parentTransform;
                fusedUnit = MapLoader.instance.SpawnAppmonUnit(holder, fusedData.name, unitTeam, unitFacing, tileA);
            }

            // Fallback para testes unitários ou ambientes sem MapLoader
            if (fusedUnit == null)
            {
                GameObject fusedObj = UnityEngine.Object.Instantiate(mainUnit.gameObject, spawnWorldPos, Quaternion.identity, parentTransform);
                fusedObj.name = $"{fusedData.name}_[GAPPAI]";
                fusedObj.SetActive(true);

                fusedUnit = fusedObj.GetComponent<Unit>();
                if (fusedUnit == null)
                {
                    fusedUnit = fusedObj.AddComponent<Unit>();
                }

                fusedData.ApplyToUnit(fusedUnit);
                fusedUnit.PlaceAtTile(tileA);
                fusedUnit.SetFacing(unitFacing);
            }

            // Desativa os GameObjects originais
            mainUnit.gameObject.SetActive(false);
            partnerUnit.gameObject.SetActive(false);

            if (BattleController.Instance != null)
            {
                BattleController.Instance.UnregisterUnit(mainUnit);
                BattleController.Instance.UnregisterUnit(partnerUnit);
            }

            // 4. Configuração de Estado e Flag Requerida
            fusedUnit.gameObject.name = $"{fusedData.name}_[GAPPAI]";
            fusedUnit.unitName = fusedData.name;
            fusedUnit.IsTemporaryFusion = true;
            fusedUnit.team = unitTeam;
            fusedUnit.facing = unitFacing;
            fusedUnit.linkedBagAppmon = null;
            fusedUnit.currentAppLink = null;
            fusedUnit.hasMoved = wasMoved;
            fusedUnit.hasActed = true; // Consome a ação do turno

            // Aplica os dados do compêndio
            fusedData.ApplyToUnit(fusedUnit);
            fusedUnit.PlaceAtTile(tileA);
            fusedUnit.SetFacing(unitFacing);

            // Vincula referência no record
            record.fusedUnit = fusedUnit;
            activeFusions.Add(record);

            // Registra a nova unidade na batalha
            if (BattleController.Instance != null)
            {
                BattleController.Instance.RegisterUnit(fusedUnit);
                BattleController.Instance.currentUnit = fusedUnit;
            }

            // Atualiza Câmera
            if (TacticalCameraController.Instance != null && fusedUnit != null)
            {
                TacticalCameraController.Instance.FocusOn(fusedUnit.transform);
            }

            // Atualiza HUD de combate
            if (BattleHUD.Instance != null)
            {
                BattleHUD.Instance.UpdateTurnBanner(fusedUnit);
                BattleHUD.Instance.UpdateControlsPrompt(
                    $"★ FUSÃO REALIZADA: {fusedData.name}! ★",
                    $"• Rank: {fusedData.rank}    • HP: {fusedData.hp}    • ATK: {fusedData.atk}    • DEF: {fusedData.def}\n• Escolha a direção defensiva para encerrar o turno."
                );
            }

            // Feedback visual flutuante com nome do monstro fundido
            DamagePopupService.ShowMessage(tileA.worldPos, $"★ GAPPAI: {fusedData.name}! ★", new Color(0.0f, 1.0f, 0.65f, 1.0f));

            Debug.Log($"[AppGappaiService] ★ FUSÃO CONCLUÍDA! {fusedData.name} ({fusedData.rank}) invocado na posição {tileA.pos}. Flag IsTemporaryFusion = true.");
            return fusedUnit;
        }

        // =========================================================================
        // EXECUÇÃO DE APP-LINK SIMPLES (INCOMPATÍVEL / APENAS BÔNUS)
        // =========================================================================

        public static bool ExecuteFieldLink(Unit mainUnit, Unit partnerUnit, out string summary)
        {
            summary = "";
            if (mainUnit == null || partnerUnit == null) return false;

            if (mainUnit.stats != null && partnerUnit.stats != null)
            {
                int bonusAtk = Mathf.Max(5, Mathf.RoundToInt(partnerUnit.stats.GetStat(StatEnum.ATK) * 0.15f));
                int bonusDef = Mathf.Max(5, Mathf.RoundToInt(partnerUnit.stats.GetStat(StatEnum.DEF) * 0.15f));

                mainUnit.stats.SetStat(StatEnum.ATK, mainUnit.stats.GetStat(StatEnum.ATK) + bonusAtk);
                mainUnit.stats.SetStat(StatEnum.DEF, mainUnit.stats.GetStat(StatEnum.DEF) + bonusDef);

                summary = $"+{bonusAtk} ATK, +{bonusDef} DEF (Sinergia com {partnerUnit.unitName})";
                mainUnit.hasActed = true;

                if (BattleHUD.Instance != null)
                {
                    BattleHUD.Instance.UpdateTurnBanner(mainUnit);
                    BattleHUD.Instance.UpdateControlsPrompt(
                        "● APP-LINK ATIVADO!",
                        $"• Bônus recebido: +{bonusAtk} ATK, +{bonusDef} DEF (Sinergia com {partnerUnit.unitName})\n• Escolha a direção defensiva para encerrar o turno."
                    );
                }

                DamagePopupService.ShowMessage(mainUnit.transform.position, $"● LINK: +{bonusAtk} ATK, +{bonusDef} DEF!", new Color(0.2f, 0.85f, 1.0f, 1.0f));

                Debug.Log($"[AppGappaiService] App-Link simples de campo ativado: {mainUnit.unitName} recebeu {summary}.");
                return true;
            }

            return false;
        }

        // =========================================================================
        // RESTAURAÇÃO AUTOMÁTICA AO FIM DO COMBATE
        // =========================================================================

        public static void HandleBattleEnd(Team winner)
        {
            Debug.Log($"[AppGappaiService] Fim de combate detectado (Vencedor: {winner}). Revertendo todas as fusões temporárias...");
            RevertAllFusions();
        }

        public static void RevertAllFusions()
        {
            if (activeFusions.Count == 0) return;

            Debug.Log($"[AppGappaiService] Revertendo {activeFusions.Count} fusão(ões) temporária(s)...");

            for (int i = activeFusions.Count - 1; i >= 0; i--)
            {
                var record = activeFusions[i];
                if (record == null) continue;

                // 1. Destrói a criatura fundida
                if (record.fusedUnit != null)
                {
                    if (BattleController.Instance != null)
                    {
                        BattleController.Instance.UnregisterUnit(record.fusedUnit);
                    }

                    if (record.fusedUnit.currentTile != null && record.fusedUnit.currentTile.content == record.fusedUnit.gameObject)
                    {
                        record.fusedUnit.currentTile.content = null;
                    }

                    UnityEngine.Object.Destroy(record.fusedUnit.gameObject);
                }

                // 2. Faz o respawn das duas criaturas originais com os status restaurados
                if (record.originalUnitA != null)
                {
                    record.originalUnitA.gameObject.SetActive(true);
                    if (record.originalUnitA.stats != null)
                    {
                        record.originalUnitA.stats.SetStat(StatEnum.MaxHp, record.maxHpA);
                        record.originalUnitA.stats.SetStat(StatEnum.HP, record.hpA);
                        record.originalUnitA.stats.SetStat(StatEnum.MP, record.mpA);
                        record.originalUnitA.stats.SetStat(StatEnum.SP, record.spA);
                    }

                    TileLogic targetTileA = record.tileA ?? Board.GetTile(record.posA);
                    if (targetTileA != null)
                    {
                        record.originalUnitA.PlaceAtTile(targetTileA);
                    }

                    if (BattleController.Instance != null)
                    {
                        BattleController.Instance.RegisterUnit(record.originalUnitA);
                    }
                }

                if (record.originalUnitB != null)
                {
                    record.originalUnitB.gameObject.SetActive(true);
                    if (record.originalUnitB.stats != null)
                    {
                        record.originalUnitB.stats.SetStat(StatEnum.MaxHp, record.maxHpB);
                        record.originalUnitB.stats.SetStat(StatEnum.HP, record.hpB);
                        record.originalUnitB.stats.SetStat(StatEnum.MP, record.mpB);
                        record.originalUnitB.stats.SetStat(StatEnum.SP, record.spB);
                    }

                    TileLogic targetTileB = record.tileB ?? Board.GetTile(record.posB);
                    if (targetTileB != null)
                    {
                        record.originalUnitB.PlaceAtTile(targetTileB);
                    }

                    if (BattleController.Instance != null)
                    {
                        BattleController.Instance.RegisterUnit(record.originalUnitB);
                    }
                }
            }

            activeFusions.Clear();
            Debug.Log("[AppGappaiService] Todas as criaturas originais foram restauradas com sucesso.");
        }
    }
}
