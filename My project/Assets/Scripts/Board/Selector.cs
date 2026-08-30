using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : MonoBehaviour
{
    public static Selector Instance;

    public Vector3Int position => tile != null ? tile.pos : Vector3Int.zero;
    public TileLogic tile;
    public SpriteRenderer spriteRenderer;

    [Header("Posicionamento e Deslizamento")]
    [Tooltip("Velocidade de deslizamento suave até o tile alvo")]
    public float glideSpeed = 28.0f;

    [Header("Marcador Chevron Flutuante (Digimon Survive)")]
    [Tooltip("Altura base do Chevron flutuante acima do centro do tile")]
    public float chevronHeight = 0.65f;
    [Tooltip("Velocidade de flutuação vertical do Chevron")]
    public float chevronBobSpeed = 4.0f;
    [Tooltip("Amplitude do salto/bobbing vertical do Chevron")]
    public float chevronBobAmount = 0.06f;

    private Vector3 targetWorldPos;
    private Transform visualChild;
    private Vector3 visualBaseLocalPos;

    private Transform chevronTransform;
    private SpriteRenderer chevronRenderer;

    void Awake()
    {
        Instance = this;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            visualChild = spriteRenderer.transform;
            visualBaseLocalPos = visualChild.localPosition;
            // Define o sprite procedural em alta resolução
            spriteRenderer.sprite = ProceduralGridTileFactory.TargetTile;
        }
        else
        {
            visualBaseLocalPos = Vector3.zero;
        }

        SetupChevronMarker();
        targetWorldPos = transform.position;
    }

    void SetupChevronMarker()
    {
        GameObject chevronObj = new GameObject("ChevronMarker");
        chevronObj.transform.parent = transform;
        chevronObj.transform.localPosition = new Vector3(0, chevronHeight, 0);
        chevronObj.transform.localScale = Vector3.one * 0.45f;

        chevronRenderer = chevronObj.AddComponent<SpriteRenderer>();
        chevronRenderer.sprite = ProceduralGridTileFactory.TargetChevron;
        chevronRenderer.sortingOrder = 999; // Sempre visível acima das unidades

        chevronTransform = chevronObj.transform;
    }

    void Update()
    {
        // 1. Interpolação suave até o tile alvo
        if (Vector3.Distance(transform.position, targetWorldPos) > 0.001f)
        {
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, glideSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetWorldPos;
        }

        // 2. Animação de respiração e flutuação suave do Chevron
        if (chevronTransform != null)
        {
            float bob = Mathf.Sin(Time.time * chevronBobSpeed) * chevronBobAmount;
            chevronTransform.localPosition = new Vector3(0, chevronHeight + bob, 0);
        }
    }

    /// <summary>
    /// Define o novo tile alvo do cursor com transição suave ou instantânea.
    /// </summary>
    public void SetTargetTile(TileLogic newTile, bool instant = false)
    {
        if (newTile == null) return;

        tile = newTile;
        targetWorldPos = newTile.worldPos;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = newTile.contentOrder + 2;
        }

        if (instant)
        {
            transform.position = targetWorldPos;
        }

        if (visualChild != null)
        {
            visualChild.localPosition = visualBaseLocalPos;
        }
    }

    /// <summary>
    /// Atualiza o visual do seletor conforme o estado da ação (Válido, Inválido, Ataque, Movimento).
    /// </summary>
    public void SetSelectionVisual(bool isValid, GridHighlightMode mode = GridHighlightMode.Movement)
    {
        if (spriteRenderer != null)
        {
            if (isValid)
            {
                spriteRenderer.sprite = ProceduralGridTileFactory.TargetTile;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.sprite = ProceduralGridTileFactory.InvalidTile;
                spriteRenderer.color = new Color(1.0f, 0.4f, 0.4f, 1.0f);
            }
        }

        if (chevronRenderer != null)
        {
            chevronRenderer.gameObject.SetActive(true);
            chevronRenderer.color = isValid 
                ? (mode == GridHighlightMode.Attack ? new Color(1.0f, 0.6f, 0.1f, 1.0f) : new Color(1.0f, 0.95f, 0.35f, 1.0f))
                : new Color(1.0f, 0.25f, 0.25f, 0.9f);
        }
    }

    /// <summary>
    /// Restaura o visual padrão do cursor.
    /// </summary>
    public void ResetSelectionVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = ProceduralGridTileFactory.TargetTile;
            spriteRenderer.color = Color.white;
        }

        if (chevronRenderer != null)
        {
            chevronRenderer.color = new Color(1.0f, 0.95f, 0.35f, 1.0f);
        }
    }

    /// <summary>
    /// Ativa ou desativa a exibição do Chevron marcador.
    /// </summary>
    public void SetChevronVisible(bool visible)
    {
        if (chevronRenderer != null)
        {
            chevronRenderer.gameObject.SetActive(visible);
        }
    }
}



