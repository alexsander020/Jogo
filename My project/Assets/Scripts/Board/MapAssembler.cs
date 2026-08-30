using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Montador de mapas e arenas táticas. Popula os andares do tabuleiro,
/// aplica os tipos de terreno correspondentes e configura os pontos de spawn.
/// </summary>
public static class MapAssembler
{
    public static TacticalMapDefinition ActiveMap;

    /// <summary>
    /// Monta o mapa tático fornecido no tabuleiro e configura todos os andares.
    /// </summary>
    public static void AssembleMap(Board board, TacticalMapDefinition mapDef = null)
    {
        if (board == null) return;

        mapDef ??= TacticalMapDefinition.CreateCyberRuinsSectorCentral();
        ActiveMap = mapDef;

        Debug.Log($"[MapAssembler] Montando arena de batalha: '{mapDef.mapName}' (Bioma: {mapDef.biome}, Dimensões: {mapDef.dimensions.x}x{mapDef.dimensions.y})");

        if (board.floors == null || board.floors.Count == 0)
        {
            Debug.LogWarning("[MapAssembler] O tabuleiro não possui andares (floors) configurados!");
            return;
        }

        // Limpa overrides anteriores nos andares
        for (int i = 0; i < board.floors.Count; i++)
        {
            Floor floor = board.floors[i];
            if (floor == null) continue;

            floor.customTiles = new List<Floor.CustomTileTerrain>();
            floor.minXY = new Vector3Int(0, 0, 0);
            floor.maxXY = new Vector3Int(mapDef.dimensions.x - 1, mapDef.dimensions.y - 1, 0);
        }

        // Popula as posições e terrenos de cada andar
        for (int i = 0; i < mapDef.tiles.Count; i++)
        {
            TilePlacementData placement = mapDef.tiles[i];
            if (placement.floorIndex < 0 || placement.floorIndex >= board.floors.Count) continue;

            Floor targetFloor = board.floors[placement.floorIndex];
            if (targetFloor == null) continue;

            // Registra o tipo de terreno no Floor
            targetFloor.customTiles.Add(new Floor.CustomTileTerrain
            {
                pos = placement.position,
                terrain = placement.terrainType
            });

            // Se o Tilemap existir, garante que ele possui o tile registrado
            if (targetFloor.tilemap != null && !targetFloor.tilemap.HasTile(placement.position))
            {
                // Se o tilemap não tiver tile desenhado naquela posição, define um tile base para ser carregado pelo Board
                Tile defaultTile = ScriptableObject.CreateInstance<Tile>();
                defaultTile.name = !string.IsNullOrEmpty(placement.tileAssetName) ? placement.tileAssetName : placement.terrainType.ToString();
                targetFloor.tilemap.SetTile(placement.position, defaultTile);
            }
        }

        Debug.Log($"[MapAssembler] Arena '{mapDef.mapName}' montada com sucesso! Total de tiles: {mapDef.tiles.Count}");
    }
}
