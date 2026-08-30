using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileLogic
{
    public Vector3Int pos;
    public Vector3 worldPos;
    public GameObject content;
    public Floor floor;
    public int contentOrder;
    public TerrainType terrainType = TerrainType.Standard;

    // Propriedades derivadas do catálogo de terrenos
    public TerrainData Terrain => TerrainDatabase.Get(terrainType);
    public int movementCost => Terrain.movementCost;
    public bool isWalkable => Terrain.isWalkable;
    public float defenseBonusPercent => Terrain.defenseBonusPercent;
    public float evasionBonusPercent => Terrain.evasionBonusPercent;
    public int hpTurnEffect => Terrain.hpTurnEffect;
    public int spTurnEffect => Terrain.spTurnEffect;

    public TileLogic() { }

    public TileLogic(Vector3Int cellPos, Vector3 worldPosition, Floor tempFloor, TerrainType type = TerrainType.Standard)
    {
        pos = cellPos;
        worldPos = worldPosition;
        floor = tempFloor;
        contentOrder = tempFloor.contentOrder;
        terrainType = type;
    }

    public static TileLogic Create(Vector3Int cellPos, Vector3 worldPosition, Floor floor, TerrainType type = TerrainType.Standard)
    {
        TileLogic tileLogic = new TileLogic(cellPos, worldPosition, floor, type);
        return tileLogic;
    }
}

