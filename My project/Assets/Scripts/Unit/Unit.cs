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

    [Header("Combate & Habilidades")]
    [Tooltip("Alcance de ataque padrão em tiles")]
    public int attackRange = 2;
    [Tooltip("Lista de Habilidades e Golpes de Combate")]
    public List<SkillData> skills = new List<SkillData>();
    [Tooltip("Habilidade Passiva da Unidade")]
    public PassiveSkillData passiveSkill = new PassiveSkillData("Pernas Poderosas", "Aumenta VELOC em um nível.");

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
    public bool isDefending = false;

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

    public void ShowDefensePreview(FacingDirection dir)
    {
        SetFacing(dir);
    }

    public void SetDefenseStance(FacingDirection dir)
    {
        isDefending = true;
        SetFacing(dir);
        Debug.Log($"[Defesa] {unitName} assumiu posição de Defesa voltada para {DirectionUtils.GetDirectionName(dir)}.");
    }

    public void ClearDefenseStance()
    {
        isDefending = false;
    }

    // Inicia o turno desta unidade
    public virtual void StartTurn()
    {
        hasMoved = false;
        hasActed = false;
        isTurnCompleted = false;
        ClearDefenseStance();

        var appmon = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        if (appmon != null)
        {
            appmon.OnTurnStart();
        }
    }

    // Encerra o turno desta unidade
    public virtual void EndTurn()
    {
        isTurnCompleted = true;

        var appmon = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        if (appmon != null)
        {
            appmon.OnTurnEnd();
        }
    }

    public bool CanAct()
    {
        var appmon = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        if (appmon != null && !appmon.CanAct()) return false;
        return !isTurnCompleted && !hasActed && IsAlive;
    }

    public bool CanMove()
    {
        var appmon = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        if (appmon != null && !appmon.CanMove()) return false;
        return !isTurnCompleted && !hasMoved && IsAlive;
    }

    public bool IsAlive => stats != null ? stats.GetStat(StatEnum.HP) > 0 : true;

    public void ApplyAppmon(string nameOrId)
    {
        var data = TacticalBattle.Appmon.AppmonDatabase.Get(nameOrId);
        if (data != null) ApplyAppmon(data);
    }

    public void ApplyAppmon(TacticalBattle.Appmon.AppmonData data)
    {
        if (data == null) return;
        var comp = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        if (comp == null) comp = gameObject.AddComponent<TacticalBattle.Appmon.AppmonCharacter>();
        comp.InitializeFromData(data);
    }

    public void TakeDamage(int damage, AttackOrientation orientation = AttackOrientation.Frontal, bool isCritical = false, bool hasAdvantage = false)
    {
        if (stats != null)
        {
            var appmon = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
            if (appmon != null)
            {
                if (appmon.teamDamageImmunityTurns > 0)
                {
                    Debug.Log($"[Imunidade Celestial] {unitName} anulou 100% do dano recebido!");
                    DamagePopupService.ShowDamage(transform.position, 0, orientation, false, false);
                    return;
                }
            }

            // Se a unidade estiver em posição de Defesa
            if (isDefending)
            {
                if (orientation == AttackOrientation.Frontal)
                {
                    int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.70f)); // 30% de absorção de dano frontal
                    Debug.Log($"[Defesa] {unitName} defendeu o ataque frontal! Dano reduzido: {damage} -> {reducedDamage}");
                    damage = reducedDamage;
                }
                else if (orientation == AttackOrientation.Flank)
                {
                    int reducedDamage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.85f)); // 15% de absorção lateral
                    Debug.Log($"[Defesa] {unitName} amorteceu o ataque lateral! Dano reduzido: {damage} -> {reducedDamage}");
                    damage = reducedDamage;
                }
            }

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

    [Header("Configurações de Animação")]
    [Tooltip("Habilita ou desabilita a animação de avanço (lunge) no ataque")]
    public bool enableAttackAnimation = false;
    [Tooltip("Habilita ou desabilita a animação de tremor/piscar ao receber dano")]
    public bool enableDamageFeedbackAnimation = false;

    public IEnumerator DamageFeedbackRoutine()
    {
        if (!enableDamageFeedbackAnimation || spriteRenderer == null) yield break;

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
        if (!enableAttackAnimation)
        {
            yield return new WaitForSeconds(0.05f);
            yield break;
        }

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
        var appmon = GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        if (appmon != null && appmon.TryTriggerRebirth())
        {
            return; // Renasce das cinzas com 50% HP!
        }

        if (appmon != null)
        {
            appmon.OnDefeated(); // Dispara passivas como Combustão
        }

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

    public List<SkillData> GetSkills()
    {
        if (skills == null) skills = new List<SkillData>();

        if (skills.Count == 0)
        {
            // Habilidade 1: Ataque Básico (SP 0, Efeito 85) - Garra / Corte
            string basicIcon = "UI_Skill_Icon_Claw";
            if (category == FunctionalCategory.Game || category == FunctionalCategory.Tool)
            {
                basicIcon = "UI_Skill_Icon_Slash";
            }
            skills.Add(SkillData.CreateBasicAttack("Atacar", 85, attackRange > 0 ? attackRange : 2, category, basicIcon));

            // Habilidade 2: Habilidade Especial Elemental / Assinatura (SP 30, Efeito 120)
            string specName = "Rugido Destrutivo";
            string specDesc = "Causa dano de Vento aos alvos.";
            string specIcon = "🌪";
            string specAsset = "UI_Skill_Icon_Beam";

            if (unitName.Contains("Agumon") || unitName.Contains("Greymon"))
            {
                specName = "Rugido Destrutivo";
                specDesc = "Causa dano de Vento aos alvos.";
                specIcon = "🔥";
                specAsset = "UI_Skill_Icon_MeteorShower";
            }
            else if (unitName.Contains("Palmon"))
            {
                specName = "Espinho Venenoso";
                specDesc = "Dispara espinhos perfurantes de longo alcance.";
                specIcon = "🌿";
                specAsset = "UI_Skill_Icon_Arrow_Barrage";
            }
            else if (category == FunctionalCategory.System)
            {
                specName = "Pulso Quântico";
                specDesc = "Descarga de dados concentrada que atinge o alvo.";
                specIcon = "⚡";
                specAsset = "UI_Skill_Icon_PsycicAttack";
            }

            skills.Add(SkillData.CreateSpecialSkill(specName, 120, 30, category, specDesc, 3, TacticalBattle.Core.AttackShapeType.Single, 0, specIcon, specAsset));
        }

        return skills;
    }
}


