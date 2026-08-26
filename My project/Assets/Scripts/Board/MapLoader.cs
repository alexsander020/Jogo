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

        GameObject holder = new GameObject("Units Holder");
        holder.transform.parent = Board.instance != null ? Board.instance.transform : transform;

        // Lista de posições candidatas
        List<Vector3Int> candidatePositions = new List<Vector3Int>();
        if (Board.instance != null && Board.instance.tiles != null)
        {
            foreach (var pos in Board.instance.tiles.Keys)
            {
                candidatePositions.Add(pos);
            }
        }

        // Ordena posições por Y para colocar jogadores ao sul (baixo) e inimigos ao norte (cima)
        candidatePositions.Sort((a, b) => a.y.CompareTo(b.y));

        Vector3Int p1Pos = FindAvailablePos(candidatePositions, new Vector3Int(0, 0, 0));
        Vector3Int p2Pos = FindAvailablePos(candidatePositions, new Vector3Int(1, 0, 0));
        Vector3Int e1Pos = FindAvailablePos(candidatePositions, new Vector3Int(0, 3, 0), true);
        Vector3Int e2Pos = FindAvailablePos(candidatePositions, new Vector3Int(1, 3, 0), true);

        // 1. Jogador 1 (Aethel)
        SpawnUnit(holder.transform, "Aethel", Team.Player, FunctionalCategory.System, 
            ProtocolTrinity.Firewall, FacingDirection.North, p1Pos, 14, 120, 60, 25, 12);

        // 2. Jogador 2 (Aliado Suporte)
        SpawnUnit(holder.transform, "Bit-Fox", Team.Player, FunctionalCategory.Social, 
            ProtocolTrinity.Ping, FacingDirection.North, p2Pos, 12, 100, 50, 18, 10);

        // 3. Inimigo 1 (Corrupt-Bot Alpha)
        SpawnUnit(holder.transform, "Corrupt Alpha", Team.Enemy, FunctionalCategory.Navi, 
            ProtocolTrinity.Overclock, FacingDirection.South, e1Pos, 11, 90, 30, 20, 8, Color.red);

        // 4. Inimigo 2 (Corrupt-Bot Beta)
        SpawnUnit(holder.transform, "Corrupt Beta", Team.Enemy, FunctionalCategory.Tool, 
            ProtocolTrinity.Overclock, FacingDirection.South, e2Pos, 9, 140, 20, 22, 15, new Color(1f, 0.4f, 0.4f));
    }

    Vector3Int FindAvailablePos(List<Vector3Int> candidates, Vector3Int preferred, bool fromTop = false)
    {
        if (Board.GetTile(preferred) != null && Board.GetTile(preferred).content == null)
        {
            return preferred;
        }

        if (fromTop)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                TileLogic t = Board.GetTile(candidates[i]);
                if (t != null && t.content == null) return candidates[i];
            }
        }
        else
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                TileLogic t = Board.GetTile(candidates[i]);
                if (t != null && t.content == null) return candidates[i];
            }
        }

        return preferred;
    }

    Unit SpawnUnit(Transform parent, string name, Team team, FunctionalCategory cat, 
        ProtocolTrinity protocol, FacingDirection facing, Vector3Int gridPos, 
        int speed, int hp, int sp, int atk, int def, Color? tint = null)
    {
        TileLogic tile = Board.GetTile(gridPos);
        Vector3 spawnWorldPos = tile != null ? tile.worldPos : Vector3.zero;

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

        if (tile != null)
        {
            unit.PlaceAtTile(tile);
        }

        if (BattleController.Instance != null)
        {
            BattleController.Instance.RegisterUnit(unit);
        }

        return unit;
    }
}
