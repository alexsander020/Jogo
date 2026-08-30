using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public Dictionary<Vector3Int, TileLogic> tiles;
    public List<Floor> floors;
    public static Board instance;
    [HideInInspector]
    public Grid grid;

    [Header("Configuração de Arena / Mapa Tático")]
    [Tooltip("Se ativado, monta automaticamente a arena tática procedural nos andares. Se falso, usa o mapa desenhado na cena")]
    public bool autoAssembleTacticalMap = false;
    public MapBiome activeBiome = MapBiome.CyberRuins;

    void Awake()
    {
        tiles = new Dictionary<Vector3Int, TileLogic>();
        instance = this;
        grid = GetComponent<Grid>();
    }

    public IEnumerator InitSequence(LoadState loadState)
    {
        if (autoAssembleTacticalMap)
        {
            var mapDef = activeBiome == MapBiome.NaturalPlateau 
                ? TacticalMapDefinition.CreateNaturalPlateauForest()
                : TacticalMapDefinition.CreateCyberRuinsSectorCentral();
            MapAssembler.AssembleMap(this, mapDef);
        }

        yield return StartCoroutine(LoadFloors(loadState));
        yield return null;
        Debug.Log("Board InitSequence completed");
        ShadowOrdering();
        yield return null;
    }

    IEnumerator LoadFloors(LoadState loadState)
    {
        for (int i = 0; i < floors.Count; i++)
        {
            List<Vector3Int> floorTiles = floors[i].LoadTiles();
            yield return null;
            for (int j = 0; j < floorTiles.Count; j++)
            {
                if (!tiles.ContainsKey(floorTiles[j]))
                {
                    CreateTile(floorTiles[j], floors[i]);
                }
            }
        }
    }

    void CreateTile(Vector3Int pos, Floor floor)
    {
        Vector3 worldPos = grid.CellToWorld(pos);
        worldPos.y += (floor.tilemap.tileAnchor.y / 2) - 0.5f;
        TerrainType terrain = floor != null ? floor.GetTerrainAt(pos) : TerrainType.Standard;
        TileLogic tile = new TileLogic(pos, worldPos, floor, terrain);
        tiles.Add(pos, tile);
    }


    void ShadowOrdering()
    {
        foreach (TileLogic t in tiles.Values)
        {
            int floorIndex = floors.IndexOf(t.floor);
            floorIndex -= 2;

            if (floorIndex >= floors.Count || floorIndex < 0)
            {
                continue;
            }
            Floor floorToCheck = floors[floorIndex];

            Vector3Int pos = t.pos;
            IsNECheck(floorToCheck, t, pos + Vector3Int.right);
            IsNECheck(floorToCheck, t, pos + Vector3Int.up); 
            IsNECheck(floorToCheck, t, pos + Vector3Int.right + Vector3Int.up);
        }
    }

    void IsNECheck(Floor floor, TileLogic t, Vector3Int NEPosition)
    {
        if (floor.tilemap.HasTile(NEPosition))
        {
            t.contentOrder = floor.order;
        }
    }


    public static TileLogic GetTile(Vector3Int pos)
    {
        TileLogic tile = null;
        instance.tiles.TryGetValue(pos, out tile);


        return tile;
    }


}