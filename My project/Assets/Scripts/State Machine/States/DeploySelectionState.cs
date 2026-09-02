using System.Collections.Generic;
using TacticalBattle.Appmon;
using UnityEngine;

public class DeploySelectionState : State
{
    private List<TileLogic> spawnTiles = new List<TileLogic>();
    private Dictionary<AppmonData, Unit> deployedUnits = new Dictionary<AppmonData, Unit>();
    private DeploymentMenuUI menuUI;

    public override void Enter()
    {
        Debug.Log("[DeployState] Iniciando fase de seleção e posicionamento de criaturas pré-batalha.");

        // Oculta a interface de combate padrão (BattleHUD) enquanto o jogador seleciona sua equipe
        if (BattleHUD.Instance != null && BattleHUD.Instance.canvas != null)
        {
            BattleHUD.Instance.canvas.gameObject.SetActive(false);
        }

        // 1. Coleta os tiles designados para o spawn do jogador (Sul)
        if (MapLoader.instance != null)
        {
            spawnTiles = MapLoader.instance.GetPlayerSpawnTiles(4);
        }

        // 2. Destaca os tiles de spawn com contorno amarelo neon brilhante (Digimon Survive)
        if (GridHighlighter.Instance != null && spawnTiles.Count > 0)
        {
            HashSet<Vector3Int> coords = new HashSet<Vector3Int>();
            foreach (var t in spawnTiles)
            {
                if (t != null) coords.Add(t.pos);
            }
            GridHighlighter.Instance.ShowDeployRange(coords);
        }

        // 3. Centraliza a câmera no campo de batalha
        if (TacticalCameraController.Instance != null)
        {
            TacticalCameraController.Instance.CenterOnBoard();
        }

        // 4. Inicializa e exibe o Smartphone de Desdobramento
        menuUI = FindFirstObjectByType<DeploymentMenuUI>();
        if (menuUI == null)
        {
            GameObject uiObj = new GameObject("DeploymentMenuUI", typeof(DeploymentMenuUI));
            menuUI = uiObj.GetComponent<DeploymentMenuUI>();
        }

        menuUI.OnUnitToggled = HandleUnitToggled;
        menuUI.OnBattleStartRequested = HandleBattleStart;
        menuUI.OnUnitHovered = HandleUnitHovered;

        // Começa sem nenhuma criatura selecionada: o jogador escolhe quem entra em campo
        deployedUnits.Clear();
        menuUI.Initialize(spawnTiles.Count > 0 ? spawnTiles.Count : 4, null);
        menuUI.Show();
    }

    public override void Exit()
    {
        if (menuUI != null)
        {
            menuUI.OnUnitToggled = null;
            menuUI.OnBattleStartRequested = null;
            menuUI.OnUnitHovered = null;
            menuUI.Hide();
        }

        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }

        // Garante que o BattleHUD esteja visível no início do combate
        if (BattleHUD.Instance != null && BattleHUD.Instance.canvas != null)
        {
            BattleHUD.Instance.canvas.gameObject.SetActive(true);
        }

        Debug.Log("[DeployState] Fase de posicionamento concluída.");
    }

    private void HandleUnitHovered(AppmonData appmon)
    {
        if (appmon == null || GridHighlighter.Instance == null || spawnTiles == null) return;

        HashSet<Vector3Int> coords = new HashSet<Vector3Int>();
        foreach (var t in spawnTiles) if (t != null) coords.Add(t.pos);

        if (deployedUnits.TryGetValue(appmon, out var unit) && unit != null && unit.currentTile != null)
        {
            GridHighlighter.Instance.ShowDeployRange(coords, unit.currentTile.pos);
        }
        else
        {
            GridHighlighter.Instance.ShowDeployRange(coords, null);
        }
    }

    private void HandleUnitToggled(AppmonData appmon)
    {
        if (appmon == null) return;

        // Se já está posicionado em campo, retira
        if (deployedUnits.TryGetValue(appmon, out var existingUnit))
        {
            if (MapLoader.instance != null)
            {
                MapLoader.instance.RemovePlayerDeployUnit(existingUnit);
            }
            deployedUnits.Remove(appmon);
            Debug.Log($"[Deploy] {appmon.name} retirado do campo.");
        }
        else
        {
            // Se foi adicionado à seleção, posiciona no próximo tile livre
            SpawnUnitOnNextFreeTile(appmon);
        }

        HandleUnitHovered(appmon);
    }

    private void SpawnUnitOnNextFreeTile(AppmonData appmon)
    {
        if (appmon == null || MapLoader.instance == null) return;

        TileLogic targetTile = null;
        foreach (var t in spawnTiles)
        {
            if (t != null && t.content == null)
            {
                targetTile = t;
                break;
            }
        }

        if (targetTile != null)
        {
            Unit u = MapLoader.instance.SpawnPlayerDeployUnit(appmon.name, targetTile);
            if (u != null)
            {
                deployedUnits[appmon] = u;
                Debug.Log($"[Deploy] {appmon.name} posicionado no tile {targetTile.pos}!");
            }
        }
        else
        {
            Debug.LogWarning("[Deploy] Nenhum tile de spawn livre encontrado para posicionar a criatura!");
        }
    }

    private void HandleBattleStart()
    {
        if (deployedUnits.Count == 0)
        {
            Debug.LogWarning("[Deploy] Você precisa colocar pelo menos 1 criatura em campo para iniciar a batalha!");
            return;
        }

        Debug.Log($"[Deploy] Batalha iniciada com {deployedUnits.Count} criaturas do jogador!");

        // 1. Limpa os destaques amarelos dos tiles
        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearHighlights();
        }

        // 2. Esconde o Smartphone
        if (menuUI != null)
        {
            menuUI.Hide();
        }

        // 3. Inicializa o controlador de batalha com as unidades presentes
        if (battle != null)
        {
            battle.InitBattle();
        }

        // 4. Inicia o primeiro turno da batalha!
        machine.ChangeTo<TurnStartState>();
    }
}
