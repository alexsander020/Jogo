using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Identificação e Tipo")]
    public string unitName = "Entidade Digital";
    public Team team = Team.Player;
    public FunctionalCategory category = FunctionalCategory.System;
    public ProtocolTrinity protocol = ProtocolTrinity.Firewall;
    public EvolutionRank rank = EvolutionRank.Standard;
    public bool isEvolved => rank != EvolutionRank.Standard;

    [Header("Posicionamento e Orientação")]
    public FacingDirection facing = FacingDirection.South;
    public TileLogic currentTile;

    public Vector3Int gridPosition => currentTile != null ? currentTile.pos : Vector3Int.zero;

    [Header("Sprites Direcionais (4 Direções)")]
    [Tooltip("Sprite olhando para o Norte (+Y - Costas)")]
    public Sprite spriteNorth;
    [Tooltip("Sprite olhando para o Leste (+X - Frente-Direita)")]
    public Sprite spriteEast;
    [Tooltip("Sprite olhando para o Sul (-Y - Frente-Esquerda)")]
    public Sprite spriteSouth;
    [Tooltip("Sprite olhando para o Oeste (-X - Costas-Esquerda)")]
    public Sprite spriteWest;

    [Header("Estado no Turno")]
    public bool hasMoved = false;
    public bool hasActed = false;
    public bool isTurnCompleted = false;

    [HideInInspector]
    public Stats stats;
    [HideInInspector]
    public Movement movement;
    [HideInInspector]
    public SpriteRenderer spriteRenderer;

    private GameObject directionIndicatorObj;

    void Awake()
    {
        stats = GetComponentInChildren<Stats>();
        if (stats == null) stats = GetComponent<Stats>();

        movement = GetComponent<Movement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Tenta auto-carregar sprites direcionais se não estiverem configurados
        TryAutoPopulateDirectionalSprites();

        // Inicializa a orientação visual inicial
        SetFacing(facing);
    }

    void TryAutoPopulateDirectionalSprites()
    {
        if (spriteNorth == null || spriteEast == null || spriteSouth == null || spriteWest == null)
        {
            // Se spriteRenderer já tem um sprite, tenta mapear pelos recursos ou manter
            if (spriteEast == null && spriteRenderer != null) spriteEast = spriteRenderer.sprite;
        }
    }

    // Posiciona a unidade em um tile do grid
    public void PlaceAtTile(TileLogic tile)
    {
        if (currentTile != null && currentTile.content == this.gameObject)
        {
            currentTile.content = null;
        }

        currentTile = tile;
        if (tile != null)
        {
            tile.content = this.gameObject;
            transform.position = tile.worldPos;
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = tile.contentOrder;
            }
        }
    }

    // Altera a direção que a unidade está olhando e atualiza o visual em todas as 4 direções
    public void SetFacing(FacingDirection newFacing)
    {
        facing = newFacing;

        if (spriteRenderer != null)
        {
            switch (facing)
            {
                case FacingDirection.North:
                    if (spriteNorth != null)
                    {
                        spriteRenderer.sprite = spriteNorth;
                        spriteRenderer.flipX = false;
                    }
                    else if (spriteSouth != null)
                    {
                        // Fallback
                        spriteRenderer.sprite = spriteSouth;
                        spriteRenderer.flipX = false;
                    }
                    break;

                case FacingDirection.East:
                    if (spriteEast != null)
                    {
                        spriteRenderer.sprite = spriteEast;
                        spriteRenderer.flipX = false;
                    }
                    else
                    {
                        spriteRenderer.flipX = false;
                    }
                    break;

                case FacingDirection.South:
                    if (spriteSouth != null)
                    {
                        spriteRenderer.sprite = spriteSouth;
                        spriteRenderer.flipX = false;
                    }
                    else if (spriteEast != null)
                    {
                        spriteRenderer.sprite = spriteEast;
                        spriteRenderer.flipX = false;
                    }
                    break;

                case FacingDirection.West:
                    if (spriteWest != null)
                    {
                        spriteRenderer.sprite = spriteWest;
                        spriteRenderer.flipX = false;
                    }
                    else if (spriteEast != null)
                    {
                        spriteRenderer.sprite = spriteEast;
                        spriteRenderer.flipX = true;
                    }
                    else
                    {
                        spriteRenderer.flipX = true;
                    }
                    break;
            }
        }
    }

    // Inicia o turno desta unidade
    public virtual void StartTurn()
    {
        hasMoved = false;
        hasActed = false;
        isTurnCompleted = false;
    }

    // Encerra o turno desta unidade
    public virtual void EndTurn()
    {
        isTurnCompleted = true;
    }

    public bool CanAct()
    {
        return !isTurnCompleted && !hasActed;
    }

    public bool CanMove()
    {
        return !isTurnCompleted && !hasMoved;
    }
}

