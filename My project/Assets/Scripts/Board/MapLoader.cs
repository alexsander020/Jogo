using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    public Unit unitPrefab;
    public static MapLoader instance;

    void Awake()
    {
        instance = this;
    }

    public void CriaUnidades()
    {
        if (unitPrefab == null)
        {
            Debug.LogWarning("[MapLoader] unitPrefab não atribuído no MapLoader!");
            return;
        }

        if (Board.instance == null || Board.instance.tiles == null || Board.instance.tiles.Count == 0)
        {
            Debug.LogError("[MapLoader] Tabuleiro vazio ou não inicializado ao criar unidades!");
            return;
        }

        GameObject holder = new GameObject("Units Holder");
        holder.transform.parent = Board.instance.transform;

        // Coleta todos os tiles válidos existentes no tabuleiro
        List<TileLogic> availableTiles = new List<TileLogic>();
        foreach (var tile in Board.instance.tiles.Values)
        {
            if (tile != null && tile.content == null)
            {
                availableTiles.Add(tile);
            }
        }

        if (availableTiles.Count == 0)
        {
            Debug.LogError("[MapLoader] Nenhum tile disponível no tabuleiro para posicionar unidades!");
            return;
        }

        // Ordena tiles por Y crescente (Sul = menor Y, Norte = maior Y)
        availableTiles.Sort((a, b) => a.pos.y != b.pos.y ? a.pos.y.CompareTo(b.pos.y) : a.pos.x.CompareTo(b.pos.x));

        // Seleção segura de tiles para os jogadores (Sul)
        TileLogic p1Tile = availableTiles[0];
        TileLogic p2Tile = availableTiles.Count > 1 ? availableTiles[1] : availableTiles[0];

        // Seleção segura de tiles para os inimigos (Norte / Platô)
        TileLogic e1Tile = availableTiles[availableTiles.Count - 1];
        TileLogic e2Tile = availableTiles.Count > 2 ? availableTiles[availableTiles.Count - 2] : e1Tile;

        // Se houver pontos de spawn preferenciais da arena que existam no tabuleiro, usa-os
        if (MapAssembler.ActiveMap != null)
        {
            if (MapAssembler.ActiveMap.playerSpawns.Count > 0)
            {
                TileLogic t = Board.GetTile(MapAssembler.ActiveMap.playerSpawns[0]);
                if (t != null && t.content == null) p1Tile = t;
            }
            if (MapAssembler.ActiveMap.playerSpawns.Count > 1)
            {
                TileLogic t = Board.GetTile(MapAssembler.ActiveMap.playerSpawns[1]);
                if (t != null && t.content == null && t != p1Tile) p2Tile = t;
            }
            if (MapAssembler.ActiveMap.enemySpawns.Count > 0)
            {
                TileLogic t = Board.GetTile(MapAssembler.ActiveMap.enemySpawns[0]);
                if (t != null && t.content == null && t != p1Tile && t != p2Tile) e1Tile = t;
            }
            if (MapAssembler.ActiveMap.enemySpawns.Count > 1)
            {
                TileLogic t = Board.GetTile(MapAssembler.ActiveMap.enemySpawns[1]);
                if (t != null && t.content == null && t != p1Tile && t != p2Tile && t != e1Tile) e2Tile = t;
            }
        }

        // 1. Jogador 1 (Aethel - Paladino Azul Tecnológico)
        SpawnUnit(holder.transform, "Aethel", Team.Player, FunctionalCategory.System, 
            ProtocolTrinity.Firewall, FacingDirection.North, p1Tile, 14, 120, 60, 25, 12, new Color(0.85f, 0.95f, 1f));

        // 2. Jogador 2 (Bit-Fox - Aliado Dourado)
        SpawnUnit(holder.transform, "Bit-Fox", Team.Player, FunctionalCategory.Social, 
            ProtocolTrinity.Ping, FacingDirection.North, p2Tile, 12, 100, 50, 18, 10, new Color(1f, 0.92f, 0.70f));

        // 3. Inimigo 1 (Corrupt Alpha - Carmesim)
        SpawnUnit(holder.transform, "Corrupt Alpha", Team.Enemy, FunctionalCategory.Navi, 
            ProtocolTrinity.Overclock, FacingDirection.South, e1Tile, 11, 90, 30, 20, 8, new Color(1f, 0.35f, 0.35f));

        // 4. Inimigo 2 (Corrupt Beta - Roxo Corrompido)
        SpawnUnit(holder.transform, "Corrupt Beta", Team.Enemy, FunctionalCategory.Tool, 
            ProtocolTrinity.Overclock, FacingDirection.South, e2Tile, 9, 140, 20, 22, 15, new Color(0.85f, 0.45f, 0.95f));
    }

    Unit SpawnUnit(Transform parent, string name, Team team, FunctionalCategory cat, 
        ProtocolTrinity protocol, FacingDirection facing, TileLogic tile, 
        int speed, int hp, int sp, int atk, int def, Color? tint = null)
    {
        if (tile == null)
        {
            Debug.LogError($"[MapLoader] Tentativa de spawnar unidade {name} em tile nulo!");
            return null;
        }

        Vector3 spawnWorldPos = tile.worldPos;

        Unit unit = Instantiate(unitPrefab, spawnWorldPos, Quaternion.identity, parent);
        unit.gameObject.name = name;
        unit.unitName = name;
        unit.team = team;
        unit.category = cat;
        unit.protocol = protocol;
        unit.SetFacing(facing);

        // Configuração de Stats
        if (unit.stats == null)
        {
            unit.stats = unit.GetComponentInChildren<Stats>();
        }
        if (unit.stats != null)
        {
            unit.stats.InitializeStatsIfEmpty();
            unit.stats.SetStat(StatEnum.SPEED, speed);
            unit.stats.SetStat(StatEnum.MaxHp, hp);
            unit.stats.SetStat(StatEnum.HP, hp);
            unit.stats.SetStat(StatEnum.MaxSp, sp);
            unit.stats.SetStat(StatEnum.SP, sp);
            unit.stats.SetStat(StatEnum.ATK, atk);
            unit.stats.SetStat(StatEnum.DEF, def);
            unit.stats.SetStat(StatEnum.MOV, 3);
        }

        // Tint visual para diferenciar inimigos
        if (tint.HasValue && unit.spriteRenderer != null)
        {
            unit.spriteRenderer.color = tint.Value;
        }

        unit.PlaceAtTile(tile);

        if (BattleController.Instance != null)
        {
            BattleController.Instance.RegisterUnit(unit);
        }

        return unit;
    }

    public Unit SpawnAppmonUnit(Transform parent, string appmonName, Team team, FacingDirection facing, TileLogic tile, Color? tint = null)
    {
        var appmonData = TacticalBattle.Appmon.AppmonDatabase.Get(appmonName);
        if (appmonData == null)
        {
            Debug.LogError($"[MapLoader] Appmon '{appmonName}' não encontrado no AppmonDatabase!");
            return null;
        }

        int speed = appmonData.spd;
        int hp = appmonData.hp;
        int sp = appmonData.mp;
        int atk = appmonData.atk;
        int def = appmonData.def;

        Unit u = SpawnUnit(parent, appmonData.name, team, appmonData.primaryCategory, appmonData.protocol, facing, tile, speed, hp, sp, atk, def, tint);
        if (u != null)
        {
            u.ApplyAppmon(appmonData);
        }
        return u;
    }

    private GameObject unitsHolder;

    public GameObject GetUnitsHolder()
    {
        if (unitsHolder == null)
        {
            unitsHolder = new GameObject("Units Holder");
            if (Board.instance != null)
            {
                unitsHolder.transform.parent = Board.instance.transform;
            }
        }
        return unitsHolder;
    }

    public List<TileLogic> GetPlayerSpawnTiles(int count = 4)
    {
        List<TileLogic> result = new List<TileLogic>();
        if (Board.instance == null || Board.instance.tiles == null) return result;

        // 1. Tenta pegar os spawns definidos no mapa
        if (MapAssembler.ActiveMap != null && MapAssembler.ActiveMap.playerSpawns != null)
        {
            foreach (var pos in MapAssembler.ActiveMap.playerSpawns)
            {
                TileLogic t = Board.GetTile(pos);
                if (t != null && !result.Contains(t) && t.content == null)
                {
                    result.Add(t);
                    if (result.Count >= count) return result;
                }
            }
        }

        // 2. Se faltar, preenche com tiles livres ao Sul (menor Y)
        List<TileLogic> availableTiles = new List<TileLogic>();
        foreach (var t in Board.instance.tiles.Values)
        {
            if (t != null && t.content == null && !result.Contains(t))
            {
                availableTiles.Add(t);
            }
        }
        availableTiles.Sort((a, b) => a.pos.y != b.pos.y ? a.pos.y.CompareTo(b.pos.y) : a.pos.x.CompareTo(b.pos.x));

        for (int i = 0; i < availableTiles.Count && result.Count < count; i++)
        {
            result.Add(availableTiles[i]);
        }

        return result;
    }

    public void SpawnEnemyUnits()
    {
        if (unitPrefab == null || Board.instance == null) return;
        Transform parent = GetUnitsHolder().transform;

        // Coleta tiles do Norte para os inimigos
        List<TileLogic> enemyTiles = new List<TileLogic>();
        if (MapAssembler.ActiveMap != null && MapAssembler.ActiveMap.enemySpawns != null)
        {
            foreach (var pos in MapAssembler.ActiveMap.enemySpawns)
            {
                TileLogic t = Board.GetTile(pos);
                if (t != null && t.content == null && !enemyTiles.Contains(t))
                {
                    enemyTiles.Add(t);
                }
            }
        }

        if (enemyTiles.Count == 0)
        {
            // Fallback: tiles de maior Y
            List<TileLogic> all = new List<TileLogic>(Board.instance.tiles.Values);
            all.Sort((a, b) => b.pos.y.CompareTo(a.pos.y));
            for (int i = 0; i < 2 && i < all.Count; i++) enemyTiles.Add(all[i]);
        }

        // Inimigo 1: Shitakumon (ou Corrupt Alpha)
        if (enemyTiles.Count > 0)
        {
            SpawnAppmonUnit(parent, "Shitakumon", Team.Enemy, FacingDirection.South, enemyTiles[0], new Color(1f, 0.45f, 0.45f));
        }

        // Inimigo 2: Flame-Log (ou Corrupt Beta)
        if (enemyTiles.Count > 1)
        {
            SpawnAppmonUnit(parent, "Flame-Log", Team.Enemy, FacingDirection.South, enemyTiles[1], new Color(1f, 0.55f, 0.35f));
        }
    }

    public Unit SpawnPlayerDeployUnit(string appmonName, TileLogic tile)
    {
        if (tile == null) return null;
        Transform parent = GetUnitsHolder().transform;
        Unit unit = SpawnAppmonUnit(parent, appmonName, Team.Player, FacingDirection.North, tile, new Color(0.95f, 1f, 1f));
        return unit;
    }

    public void RemovePlayerDeployUnit(Unit unit)
    {
        if (unit == null) return;
        if (unit.currentTile != null && unit.currentTile.content == unit.gameObject)
        {
            unit.currentTile.content = null;
        }
        if (BattleController.Instance != null)
        {
            BattleController.Instance.UnregisterUnit(unit);
        }
        Destroy(unit.gameObject);
    }
}
