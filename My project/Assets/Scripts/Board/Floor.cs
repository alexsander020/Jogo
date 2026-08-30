using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Floor : MonoBehaviour
{
    [HideInInspector]
    public TilemapRenderer tilemapRenderer;

    public int order => tilemapRenderer != null ? tilemapRenderer.sortingOrder : 0;
    public int contentOrder;

    [Header("Configuração de Terreno do Andar")]
    public TerrainType defaultTerrain = TerrainType.Standard;

    [Serializable]
    public struct CustomTileTerrain
    {
        public Vector3Int pos;
        public TerrainType terrain;
    }

    [Tooltip("Overrides manuais de terreno para posições específicas deste andar")]
    public List<CustomTileTerrain> customTiles = new List<CustomTileTerrain>();

    public Vector3Int minXY;
    public Vector3Int maxXY;

    [HideInInspector]
    public Tilemap tilemap;

    void Awake()
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemap = GetComponent<Tilemap>();
    }

    public List<Vector3Int> LoadTiles()
    {
        List<Vector3Int> tiles = new List<Vector3Int>();
        if (tilemap == null) return tiles;

        // 1. Tenta carregar automaticamente através dos limites do Tilemap
        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x > 0 && bounds.size.y > 0)
        {
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                {
                    tiles.Add(pos);
                }
            }
        }

        // 2. Se nenhum tile foi encontrado via bounds, usa a varredura por minXY/maxXY
        if (tiles.Count == 0)
        {
            for (int i = minXY.x; i <= maxXY.x; i++)
            {
                for (int j = minXY.y; j <= maxXY.y; j++)
                {
                    Vector3Int currenPos = new Vector3Int(i, j, 0);
                    if (tilemap.HasTile(currenPos))
                    {
                        tiles.Add(currenPos);
                    }
                }
            }
        }

        return tiles;
    }

    public TerrainType GetTerrainAt(Vector3Int pos)
    {
        // 1. Verifica se há override manual configurado
        if (customTiles != null)
        {
            for (int i = 0; i < customTiles.Count; i++)
            {
                if (customTiles[i].pos == pos)
                {
                    return customTiles[i].terrain;
                }
            }
        }

        // 2. Tenta inferir pelo nome do sprite / Tile no Tilemap
        if (tilemap != null)
        {
            TileBase tb = tilemap.GetTile(pos);
            if (tb != null)
            {
                string tileName = tb.name.ToLowerInvariant();
                if (tileName.Contains("barricade") || tileName.Contains("heavycover")) return TerrainType.Barricade;
                if (tileName.Contains("debris") || tileName.Contains("barrel") || tileName.Contains("boulder") || tileName.Contains("bush") || tileName.Contains("lightcover")) return TerrainType.Debris;
                if (tileName.Contains("corrupt") || tileName.Contains("hazard") || tileName.Contains("acid")) return TerrainType.Corrupted;
                if (tileName.Contains("heal") || tileName.Contains("terminal")) return TerrainType.HealingTerminal;
                if (tileName.Contains("circuit") || tileName.Contains("cyber") || tileName.Contains("panel") || tileName.Contains("data")) return TerrainType.CyberPanel;
                if (tileName.Contains("water") || tileName.Contains("chasm") || tileName.Contains("void")) return TerrainType.Chasm;
                if (tileName.Contains("cliff") || tileName.Contains("elevated") || tileName.Contains("high")) return TerrainType.ElevatedPlatform;
            }
        }

        // 3. Fallback para o padrão do andar
        return defaultTerrain;
    }
}