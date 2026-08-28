using System.Collections.Generic;
using UnityEngine;

public class GridHighlighter : MonoBehaviour
{
    public static GridHighlighter Instance;

    [Header("Cores dos Destaques (Estilo Digimon Survive)")]
    public Color reachableColor = new Color(0.15f, 0.75f, 1.0f, 0.60f);   // Azul / Ciano Neon
    public Color hoverValidColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);     // Branco Brilhante
    public Color invalidBlockedColor = new Color(1.0f, 0.20f, 0.20f, 0.85f);// Vermelho (Não pode andar)
    public Color occupiedColor = new Color(1.0f, 0.40f, 0.15f, 0.75f);     // Laranja / Alerta

    private List<GameObject> activeOverlayPool = new List<GameObject>();
    private Dictionary<Vector3Int, SpriteRenderer> activeOverlays = new Dictionary<Vector3Int, SpriteRenderer>();
    private Sprite highlightSprite;

    void Awake()
    {
        Instance = this;
    }

    Sprite GetHighlightSprite()
    {
        if (highlightSprite != null) return highlightSprite;

        if (Selector.Instance != null && Selector.Instance.spriteRenderer != null)
        {
            highlightSprite = Selector.Instance.spriteRenderer.sprite;
        }

        // Se ainda for nulo, tenta carregar dos Resources ou cria sprite padrão
        if (highlightSprite == null)
        {
            var loaded = Resources.Load<Sprite>("selector");
            if (loaded != null) highlightSprite = loaded;
        }

        return highlightSprite;
    }

    // Exibe a grade de alcance de movimento em Azul
    public void ShowMovementRange(HashSet<Vector3Int> reachableCoords, Vector3Int? currentHovered = null)
    {
        ClearHighlights();
        Sprite spr = GetHighlightSprite();

        foreach (var pos in reachableCoords)
        {
            TileLogic tile = Board.GetTile(pos);
            if (tile == null) continue;

            GameObject overlay = GetOrCreateOverlay();
            overlay.transform.position = tile.worldPos;

            SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spr;
                sr.color = reachableColor;
                sr.sortingOrder = tile.contentOrder + 1; // Fica acima do chão
            }

            overlay.SetActive(true);
            activeOverlays[pos] = sr;
        }

        if (currentHovered.HasValue)
        {
            UpdateHover(currentHovered.Value, reachableCoords.Contains(currentHovered.Value));
        }
    }

    // Atualiza a cor do cursor onde o jogador está selecionando
    public void UpdateHover(Vector3Int hoverPos, bool isReachableAndFree)
    {
        if (Selector.Instance != null && Selector.Instance.spriteRenderer != null)
        {
            TileLogic hoveredTile = Board.GetTile(hoverPos);

            if (isReachableAndFree)
            {
                // Branco brilhante para onde ele está escolhendo andar
                Selector.Instance.spriteRenderer.color = hoverValidColor;
            }
            else
            {
                // Vermelho onde ele NÃO pode andar
                Selector.Instance.spriteRenderer.color = invalidBlockedColor;
            }

            if (hoveredTile != null)
            {
                Selector.Instance.spriteRenderer.sortingOrder = hoveredTile.contentOrder + 2; // Acima dos destaques azuis
            }
        }
    }

    // Limpa todos os destaques do grid e restaura o seletor
    public void ClearHighlights()
    {
        foreach (var kvp in activeOverlays)
        {
            if (kvp.Value != null)
            {
                kvp.Value.gameObject.SetActive(false);
            }
        }
        activeOverlays.Clear();

        if (Selector.Instance != null && Selector.Instance.spriteRenderer != null)
        {
            Selector.Instance.spriteRenderer.color = Color.white;
        }
    }

    private GameObject GetOrCreateOverlay()
    {
        // Reutiliza objeto inativo do pool
        for (int i = 0; i < activeOverlayPool.Count; i++)
        {
            if (!activeOverlayPool[i].activeSelf)
            {
                return activeOverlayPool[i];
            }
        }

        // Cria novo objeto de overlay
        GameObject obj = new GameObject("TileHighlightOverlay");
        obj.transform.parent = transform;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetHighlightSprite();

        activeOverlayPool.Add(obj);
        return obj;
    }
}
