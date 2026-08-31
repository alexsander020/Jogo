using System;
using UnityEngine;

public enum ItemEffectType
{
    HealHP,
    RestoreSP,
    HealAll, // HP + SP
    BuffStat
}

[Serializable]
public class ItemData
{
    [Header("Identificação do Item")]
    public string id = "item_curativo";
    public string itemName = "Curativo";
    [TextArea(2, 4)]
    public string description = "Restaura levemente o HP do alvo. Uma bandagem de velha escola, pra ser correto. Alguém deixou cair aqui?";
    public string iconAssetName = "UI_Skill_Icon_Heal";
    public Sprite iconSprite;
    public string effectSymbol = "✦";

    [Header("Efeito de Uso")]
    public ItemEffectType effectType = ItemEffectType.HealHP;
    public int healAmount = 150;
    public int spAmount = 0;
    public int quantity = 1;

    [Header("Alcance (ALCANCE)")]
    public int minRange = 0; // 0 = pode usar em si mesmo
    public int maxRange = 4;
    public TacticalBattle.Core.AttackShapeType rangeType = TacticalBattle.Core.AttackShapeType.Single;
    public int heightToleranceUp = 3;
    public int heightToleranceDown = 3;

    [Header("Área de Efeito (ÁREA)")]
    public TacticalBattle.Core.AttackShapeType aoeType = TacticalBattle.Core.AttackShapeType.Single;
    public int aoeRadius = 0; // 0 = alvo único
    public int aoeHeightToleranceUp = 1;
    public int aoeHeightToleranceDown = 1;

    public Sprite GetIconSprite()
    {
        if (iconSprite != null) return iconSprite;
        return SkillIconDatabase.GetSkillIcon(iconAssetName, FunctionalCategory.Life);
    }
}
