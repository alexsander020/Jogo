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
        if (instance != null && instance.tiles != null)
        {
            instance.tiles.TryGetValue(pos, out tile);
        }
        return tile;
    }

    public List<TileLogic> GetNeighborTiles(TileLogic tile)
    {
        var list = new List<TileLogic>();
        if (tile == null || tiles == null) return list;

        Vector3Int[] deltas = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        foreach (var delta in deltas)
        {
            if (tiles.TryGetValue(tile.pos + delta, out var neighbor))
            {
                list.Add(neighbor);
            }
        }
        return list;
    }

    [Header("Exibição da Grade no Editor")]
    [Tooltip("Desenha as linhas da grade no Editor APENAS sobre a área desenhada com tiles")]
    public bool showGridOnlyOnDrawnArea = true;
    public Color drawnGridColor = new Color(0.0f, 0.95f, 1.0f, 0.55f);

    [ContextMenu("Comprimir Limites dos Tilemaps (Compress Bounds)")]
    public void CompressBoundsToDrawnArea()
    {
        var tilemaps = GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>();
        int count = 0;
        foreach (var tm in tilemaps)
        {
            tm.CompressBounds();
            count++;
            Debug.Log($"[Board] Tilemap '{tm.name}' comprimido para limites reais: {tm.cellBounds}");
        }
        Debug.Log($"[Board] {count} Tilemaps comprimidos com sucesso para a área desenhada!");
    }

    void OnDrawGizmos()
    {
        if (!showGridOnlyOnDrawnArea) return;

        Grid g = grid != null ? grid : GetComponent<Grid>();
        if (g == null) return;

        Gizmos.color = drawnGridColor;

        Vector3 cellSize = g.cellSize;
        float hw = cellSize.x * 0.5f;
        float hh = cellSize.y * 0.5f;

        // 1. Se em execução e o dicionário de tiles estiver populado
        if (tiles != null && tiles.Count > 0)
        {
            foreach (var t in tiles.Values)
            {
                Vector3 center = t.worldPos;
                center.y += 0.5f;
                DrawIsometricCellGizmo(center, hw, hh);
            }
        }
        else
        {
            // 2. Em modo de edição no Unity, busca todas as células preenchidas nos Tilemaps filhos
            var tilemaps = GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>();
            if (tilemaps == null || tilemaps.Length == 0) return;

            HashSet<Vector3Int> drawnCells = new HashSet<Vector3Int>();
            foreach (var tm in tilemaps)
            {
                BoundsInt bounds = tm.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (tm.HasTile(pos))
                    {
                        drawnCells.Add(pos);
                    }
                }
            }

            foreach (var pos in drawnCells)
            {
                Vector3 center = g.GetCellCenterWorld(pos);
                DrawIsometricCellGizmo(center, hw, hh);
            }
        }
    }

    private void DrawIsometricCellGizmo(Vector3 center, float hw, float hh)
    {
        Vector3 top = center + new Vector3(0, hh, 0);
        Vector3 right = center + new Vector3(hw, 0, 0);
        Vector3 bottom = center + new Vector3(0, -hh, 0);
        Vector3 left = center + new Vector3(-hw, 0, 0);

        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);
    }
}