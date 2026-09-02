using System.Collections.Generic;
using UnityEngine;

public enum GridHighlightMode
{
    Movement,
    Attack,
    Skill,
    Deploy
}

public class GridHighlighter : MonoBehaviour
{
    public static GridHighlighter Instance;

    [Header("Configurações Visuais")]
    [Tooltip("Intensidade do efeito de pulso / respiração neon")]
    public float pulseIntensity = 0.08f;
    [Tooltip("Velocidade da respiração do brilho dos tiles")]
    public float pulseSpeed = 3.5f;

    private List<GameObject> activeOverlayPool = new List<GameObject>();
    private Dictionary<Vector3Int, SpriteRenderer> activeOverlays = new Dictionary<Vector3Int, SpriteRenderer>();
    private HashSet<Vector3Int> currentRangeCoords = new HashSet<Vector3Int>();
    private List<Vector3Int> currentPathTrail = new List<Vector3Int>();
    private GridHighlightMode currentMode = GridHighlightMode.Movement;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Efeito de pulso e respiração suave no brilho dos tiles ativos
        if (activeOverlays.Count > 0)
        {
            float pulse = 1.0f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            Color baseColor = Color.white;

            foreach (var kvp in activeOverlays)
            {
                if (kvp.Value != null && kvp.Value.gameObject.activeSelf)
                {
                    // Não altera os tiles da rota que já estão super iluminados
                    if (!currentPathTrail.Contains(kvp.Key))
                    {
                        kvp.Value.color = new Color(baseColor.r, baseColor.g, baseColor.b, pulse);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Exibe a grade de alcance de movimento em Ciano Neon com cantos arredondados e centro translúcido.
    /// </summary>
    public void ShowMovementRange(HashSet<Vector3Int> reachableCoords, Vector3Int? currentHovered = null)
    {
        ShowRange(reachableCoords, GridHighlightMode.Movement, currentHovered);
    }

    /// <summary>
    /// Exibe a grade de alcance de ataque em Laranja / Âmbar Incandescente (Digimon Survive Imagem 4).
    /// </summary>
    public void ShowAttackRange(HashSet<Vector3Int> attackCoords, Vector3Int? currentHovered = null)
    {
        ShowRange(attackCoords, GridHighlightMode.Attack, currentHovered);
    }

    /// <summary>
    /// Exibe a grade de alcance de habilidades de suporte / cura em Verde Esmeralda.
    /// </summary>
    public void ShowSkillRange(HashSet<Vector3Int> skillCoords, Vector3Int? currentHovered = null)
    {
        ShowRange(skillCoords, GridHighlightMode.Skill, currentHovered);
    }

    /// <summary>
    /// Exibe a grade de posições de spawn disponíveis para posicionamento pré-batalha em Amarelo Neon (Digimon Survive).
    /// </summary>
    public void ShowDeployRange(HashSet<Vector3Int> deployCoords, Vector3Int? currentHovered = null)
    {
        ShowRange(deployCoords, GridHighlightMode.Deploy, currentHovered);
    }

    private void ShowRange(HashSet<Vector3Int> coords, GridHighlightMode mode, Vector3Int? currentHovered)
    {
        ClearHighlights();
        currentMode = mode;
        currentRangeCoords = new HashSet<Vector3Int>(coords);

        Sprite rangeSprite = mode switch
        {
            GridHighlightMode.Attack => ProceduralGridTileFactory.AttackTile,
            GridHighlightMode.Skill => ProceduralGridTileFactory.SkillTile,
            GridHighlightMode.Deploy => ProceduralGridTileFactory.DeployTile,
            _ => ProceduralGridTileFactory.MovementTile
        };

        foreach (var pos in coords)
        {
            TileLogic tile = Board.GetTile(pos);
            if (tile == null) continue;

            GameObject overlay = GetOrCreateOverlay();
            overlay.transform.position = tile.worldPos;

            SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = rangeSprite;
                sr.color = Color.white;
                sr.sortingOrder = tile.contentOrder + 1; // Camada logo acima do chão
            }

            overlay.SetActive(true);
            activeOverlays[pos] = sr;
        }

        if (currentHovered.HasValue)
        {
            UpdateHover(currentHovered.Value, coords.Contains(currentHovered.Value));
        }
    }

    /// <summary>
    /// Destaca a rota projetada do caminho que a unidade irá percorrer até o cursor.
    /// </summary>
    public void UpdatePathPreview(List<Vector3Int> path)
    {
        // 1. Restaura as cores originais da área alcançável
        if (currentPathTrail.Count > 0)
        {
            foreach (var pos in currentPathTrail)
            {
                if (activeOverlays.TryGetValue(pos, out var sr) && sr != null)
                {
                    sr.color = Color.white;
                }
            }
            currentPathTrail.Clear();
        }

        if (path == null || path.Count <= 1) return;

        // 2. Aplica destaque luminoso na trilha da rota
        for (int i = 0; i < path.Count; i++)
        {
            Vector3Int p = path[i];
            currentPathTrail.Add(p);

            if (activeOverlays.TryGetValue(p, out var sr) && sr != null)
            {
                // Realça a rota com brilho extra (1.35x de intensidade visual)
                sr.color = new Color(1.2f, 1.2f, 1.2f, 1.0f);
            }
        }
    }

    /// <summary>
    /// Atualiza o visual do seletor e feedback de célula válida/inválida.
    /// </summary>
    public void UpdateHover(Vector3Int hoverPos, bool isReachableAndFree)
    {
        if (Selector.Instance != null)
        {
            TileLogic hoveredTile = Board.GetTile(hoverPos);
            Selector.Instance.SetSelectionVisual(isReachableAndFree, currentMode);

            if (hoveredTile != null && Selector.Instance.spriteRenderer != null)
            {
                Selector.Instance.spriteRenderer.sortingOrder = hoveredTile.contentOrder + 2;
            }
        }
    }

    /// <summary>
    /// Limpa todos os destaques do grid e restaura o seletor.
    /// </summary>
    public void ClearHighlights()
    {
        currentPathTrail.Clear();
        currentRangeCoords.Clear();

        foreach (var kvp in activeOverlays)
        {
            if (kvp.Value != null)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }
        activeOverlays.Clear();

        if (Selector.Instance != null)
        {
            Selector.Instance.ResetSelectionVisual();
        }
    }

    private GameObject GetOrCreateOverlay()
    {
        for (int i = 0; i < activeOverlayPool.Count; i++)
        {
            if (!activeOverlayPool[i].activeSelf)
            {
                return activeOverlayPool[i];
            }
        }

        GameObject obj = new GameObject("TileHighlightOverlay");
        obj.transform.parent = transform;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralGridTileFactory.MovementTile;

        activeOverlayPool.Add(obj);
        return obj;
    }
}


