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

    [Header("Combate")]
    [Tooltip("Alcance de ataque em tiles (padrão: 2)")]
    public int attackRange = 2;

    public Vector3Int gridPosition => currentTile != null ? currentTile.pos : Vector3Int.zero;

    [Header("Retrato / Ícone de Turno (HUD)")]
    [Tooltip("Ícone de retrato dedicado para a HUD de turnos. Se nulo, usará automaticamente os sprites direcionais ou o spriteRenderer")]
    public Sprite portraitIcon;

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
        return !isTurnCompleted && !hasActed && IsAlive;
    }

    public bool CanMove()
    {
        return !isTurnCompleted && !hasMoved && IsAlive;
    }

    public bool IsAlive => stats != null ? stats.GetStat(StatEnum.HP) > 0 : true;

    public void TakeDamage(int damage, AttackOrientation orientation = AttackOrientation.Frontal, bool isCritical = false, bool hasAdvantage = false)
    {
        if (stats != null)
        {
            int currentHp = stats.GetStat(StatEnum.HP);
            int newHp = Mathf.Max(0, currentHp - damage);
            stats.SetStat(StatEnum.HP, newHp);

            Debug.Log($"[Combate] {unitName} recebeu {damage} de dano! HP: {currentHp} -> {newHp}");

            // Exibe o popup de dano flutuante
            DamagePopupService.ShowDamage(transform.position, damage, orientation, isCritical, hasAdvantage);

            // Efeito visual de piscar em vermelho e tremor
            StartCoroutine(DamageFeedbackRoutine());

            if (newHp <= 0)
            {
                Die();
            }
        }
    }

    public IEnumerator DamageFeedbackRoutine()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        Vector3 originalPos = transform.position;

        // Pisca em vermelho e treme
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(1f, 0.2f, 0.2f, 1f);
            transform.position = originalPos + new Vector3(UnityEngine.Random.Range(-0.06f, 0.06f), 0, 0);
            yield return new WaitForSeconds(0.05f);

            spriteRenderer.color = Color.white;
            transform.position = originalPos;
            yield return new WaitForSeconds(0.05f);
        }

        spriteRenderer.color = originalColor;
        transform.position = originalPos;
    }

    public IEnumerator PlayAttackAnimation(Vector3 targetWorldPos)
    {
        Vector3 originPos = transform.position;
        Vector3 lungePos = Vector3.Lerp(originPos, targetWorldPos, 0.35f);

        float duration = 0.12f;
        float elapsed = 0f;

        // Avanço rápido (Lunge)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(originPos, lungePos, elapsed / duration);
            yield return null;
        }

        yield return new WaitForSeconds(0.06f);

        // Retorno para a posição de origem
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(lungePos, originPos, elapsed / duration);
            yield return null;
        }

        transform.position = originPos;
    }

    public void Die()
    {
        Debug.Log($"[Combate] {unitName} foi derrotado em combate!");
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (currentTile != null && currentTile.content == this.gameObject)
        {
            currentTile.content = null;
        }

        if (BattleController.Instance != null)
        {
            BattleController.Instance.UnregisterUnit(this);
        }

        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Color startColor = spriteRenderer.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        gameObject.SetActive(false);
    }

    public Sprite GetPortraitSprite()
    {
        if (portraitIcon != null) return portraitIcon;
        if (spriteSouth != null) return spriteSouth;
        if (spriteEast != null) return spriteEast;
        if (spriteNorth != null) return spriteNorth;
        if (spriteWest != null) return spriteWest;
        if (spriteRenderer != null && spriteRenderer.sprite != null) return spriteRenderer.sprite;
        return null;
    }
}


