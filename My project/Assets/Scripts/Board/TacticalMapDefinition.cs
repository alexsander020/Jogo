using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapBiome
{
    CyberRuins,
    NaturalPlateau
}

[Serializable]
public class TilePlacementData
{
    public int floorIndex;
    public Vector3Int position;
    public TerrainType terrainType = TerrainType.Standard;
    public string tileAssetName; // Nome opcional do asset de Tile (ex: "Tile_Cyber_Barricade_1")

    public TilePlacementData(int floor, int x, int y, TerrainType terrain, string assetName = "")
    {
        floorIndex = floor;
        position = new Vector3Int(x, y, 0);
        terrainType = terrain;
        tileAssetName = assetName;
    }
}

[Serializable]
public class TacticalMapDefinition
{
    public string mapName;
    public MapBiome biome;
    public Vector2Int dimensions = new Vector2Int(7, 7);
    public List<TilePlacementData> tiles = new List<TilePlacementData>();
    public List<Vector3Int> playerSpawns = new List<Vector3Int>();
    public List<Vector3Int> enemySpawns = new List<Vector3Int>();

    /// <summary>
    /// Cria o mapa pré-configurado "Cyber-Ruínas: Setor Central" (3 andares de elevação, barricadas, painéis de dados, terminal de cura e zona corrompida).
    /// </summary>
    public static TacticalMapDefinition CreateCyberRuinsSectorCentral()
    {
        var map = new TacticalMapDefinition
        {
            mapName = "Cyber-Ruínas: Setor Central",
            biome = MapBiome.CyberRuins,
            dimensions = new Vector2Int(7, 7)
        };

        // --- ANDAR 0: Piso Base (Asfalto Urbano, Chão Corrompido no flanco oeste e Entulhos) ---
        for (int x = 0; x < 7; x++)
        {
            for (int y = 0; y < 7; y++)
            {
                // Flanco oeste corrompido
                if (x == 0 && y >= 2 && y <= 4)
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Corrupted, "Tile_Cyber_Corrupted_2"));
                }
                // Entulho / Latões industriais de cobertura leve
                else if ((x == 1 && y == 3) || (x == 5 && y == 1))
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Debris, "Tile_Cyber_Barrels_1"));
                }
                else
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Standard, "Tile_Cyber_Concrete_1"));
                }
            }
        }

        // --- ANDAR 1: Platô Médio Elevado (x: 2..5, y: 2..5) com Barricadas e Painéis de Dados ---
        for (int x = 2; x <= 5; x++)
        {
            for (int y = 2; y <= 5; y++)
            {
                // Barricada pesada de proteção no centro do platô
                if ((x == 3 && y == 3) || (x == 4 && y == 3))
                {
                    map.tiles.Add(new TilePlacementData(1, x, y, TerrainType.Barricade, "Tile_Cyber_Barricade_1"));
                }
                // Linhas de transmissão de dados (+5 SP)
                else if (x == 2 || y == 5)
                {
                    map.tiles.Add(new TilePlacementData(1, x, y, TerrainType.CyberPanel, "Tile_Cyber_Circuit_1"));
                }
                else
                {
                    map.tiles.Add(new TilePlacementData(1, x, y, TerrainType.Standard, "Tile_Cyber_Concrete_2"));
                }
            }
        }

        // --- ANDAR 2: Plataforma Superior / Ponto Estratégico (x: 3..4, y: 4..5) com Terminal de Cura ---
        map.tiles.Add(new TilePlacementData(2, 3, 4, TerrainType.HealingTerminal, "Tile_Cyber_HealingTerminal_1"));
        map.tiles.Add(new TilePlacementData(2, 4, 4, TerrainType.CyberPanel, "Tile_Cyber_Circuit_2"));
        map.tiles.Add(new TilePlacementData(2, 3, 5, TerrainType.Standard, "Tile_Cyber_Concrete_3"));
        map.tiles.Add(new TilePlacementData(2, 4, 5, TerrainType.Barricade, "Tile_Cyber_Barricade_2"));

        // Spawns dos Jogadores (Sul do mapa)
        map.playerSpawns.Add(new Vector3Int(2, 0, 0));
        map.playerSpawns.Add(new Vector3Int(4, 0, 0));
        map.playerSpawns.Add(new Vector3Int(3, 1, 0));

        // Spawns dos Inimigos (Norte e posições elevadas)
        map.enemySpawns.Add(new Vector3Int(3, 5, 0));
        map.enemySpawns.Add(new Vector3Int(5, 5, 0));
        map.enemySpawns.Add(new Vector3Int(1, 4, 0));

        return map;
    }

    /// <summary>
    /// Cria o mapa pré-configurado "Platô da Floresta Digital" (Grama, desníveis de falésia rochosa e rochas com musgo).
    /// </summary>
    public static TacticalMapDefinition CreateNaturalPlateauForest()
    {
        var map = new TacticalMapDefinition
        {
            mapName = "Platô da Floresta Digital",
            biome = MapBiome.NaturalPlateau,
            dimensions = new Vector2Int(8, 8)
        };

        // --- ANDAR 0: Piso Base de Grama e Caminho de Terra ---
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // Caminho de terra central
                if (x == 3 || x == 4)
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Standard, "Tile_Natural_Dirt_1"));
                }
                // Rochas e arbustos de cobertura
                else if ((x == 1 && y == 2) || (x == 6 && y == 5))
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Debris, "Tile_Natural_Boulder_1"));
                }
                else if ((x == 6 && y == 2) || (x == 1 && y == 5))
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Debris, "Tile_Natural_Bush_1"));
                }
                else
                {
                    map.tiles.Add(new TilePlacementData(0, x, y, TerrainType.Standard, "Tile_Natural_Grass_1"));
                }
            }
        }

        // --- ANDAR 1: Platô Rochoso Elevado (x: 2..6, y: 3..6) ---
        for (int x = 2; x <= 6; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                if (x == 4 && y == 5)
                {
                    map.tiles.Add(new TilePlacementData(1, x, y, TerrainType.ElevatedPlatform, "Tile_Natural_Cliff_1"));
                }
                else
                {
                    map.tiles.Add(new TilePlacementData(1, x, y, TerrainType.Standard, "Tile_Natural_Grass_2"));
                }
            }
        }

        // Spawns
        map.playerSpawns.Add(new Vector3Int(2, 0, 0));
        map.playerSpawns.Add(new Vector3Int(5, 0, 0));
        map.enemySpawns.Add(new Vector3Int(3, 6, 0));
        map.enemySpawns.Add(new Vector3Int(5, 6, 0));

        return map;
    }
}
