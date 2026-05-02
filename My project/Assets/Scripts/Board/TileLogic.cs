using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileLogic
{
    public Vector3Int pos;
    public Vector3Int worldPos;
    public GameObject content;
    public Floor floor;
    public int contentOrder;

    // public TileType titleType;

    public TileLogic() { }

    public TileLogic(Vector3Int cellPos, Vector3 worldPosition, Floor tempFloor)
    {
        pos = cellPos;
        worldPos = Vector3Int.FloorToInt(worldPosition); // Corrigido: converte Vector3 para Vector3Int
        floor = tempFloor;
        contentOrder = tempFloor.contentOrder;
    }

    public static TileLogic Create(Vector3Int cellPos, Vector3 worldPosition, Floor floor)
    {
        TileLogic tileLogic = new TileLogic(cellPos, worldPosition, floor);
        return tileLogic;
    }
}
