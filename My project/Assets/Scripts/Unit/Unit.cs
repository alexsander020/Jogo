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

    [Header("Posicionamento e Orientação")]
    public FacingDirection facing = FacingDirection.South;
    public TileLogic currentTile;

    public Vector3Int gridPosition => currentTile != null ? currentTile.pos : Vector3Int.zero;

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

    void Awake()
    {
        stats = GetComponentInChildren<Stats>();
        if (stats == null) stats = GetComponent<Stats>();

        movement = GetComponent<Movement>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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

    // Altera a direção que a unidade está olhando e atualiza o visual
    public void SetFacing(FacingDirection newFacing)
    {
        facing = newFacing;

        // Feedback visual da direção (Flip horizontal para East/West)
        if (spriteRenderer != null)
        {
            if (facing == FacingDirection.West)
            {
                spriteRenderer.flipX = true;
            }
            else if (facing == FacingDirection.East)
            {
                spriteRenderer.flipX = false;
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
