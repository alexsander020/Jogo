using System;
using System.Collections.Generic;
using TacticalBattle.Core;
using UnityEngine;

[Serializable]
public class SkillData
{
    [Header("Identificação & Ícone (Battle_Elements)")]
    public string id = "attack_basic";
    public string skillName = "Atacar";
    public string iconSymbol = "//";
    public string iconAssetName = "UI_Skill_Icon_Claw";
    public Sprite iconSprite;
    public FunctionalCategory category = FunctionalCategory.System;
    
    [Header("Poder e Custo")]
    public int effectPower = 85;
    public int spCost = 0;
    public int mpCost = 0;
    public bool isMagic = false; // false = Físico (ATK vs DEF), true = Mágico (INT vs SPI)
    
    [Header("Descrição do Efeito")]
    [TextArea(2, 3)]
    public string description = "Causa dano de Vento aos alvos.";

    [Header("Efeitos de Status e Terreno")]
    public StatusEffectType statusToApply = StatusEffectType.None;
    public int statusDurationTurns = 1;
    public float statusChance = 1f; // 1 = 100% de chance
    public TerrainType createsTerrain = TerrainType.Standard;
    public bool hasTerrainCreation = false;
    public int terrainRadius = 0; // 0 = 1 tile, 1 = 3x3, 2 = 5x5
    public int pushDistance = 0;  // Empurrão em tiles
    public int pullDistance = 0;  // Puxão em tiles
    public bool healsTarget = false;
    public bool isTeleport = false;
    public int teleportRange = 0;

    [Header("Alcance (ALCANCE)")]
    public int minRange = 1;
    public int maxRange = 2;
    public AttackShapeType rangeType = AttackShapeType.Single;
    public int heightToleranceUp = 1;
    public int heightToleranceDown = 1;

    [Header("Área de Efeito (ÁREA)")]
    public AttackShapeType aoeType = AttackShapeType.Single;
    public int aoeRadius = 0; // 0 = alvo único (1 quadrado), 1 = cruz/3x3, etc.
    public int aoeHeightToleranceUp = 1;
    public int aoeHeightToleranceDown = 1;

    public Sprite GetIconSprite()
    {
        if (iconSprite != null) return iconSprite;
        return SkillIconDatabase.GetSkillIcon(iconAssetName, category);
    }

    public static SkillData CreateBasicAttack(string name = "Atacar", int power = 85, int range = 2, FunctionalCategory cat = FunctionalCategory.System, string iconAsset = "UI_Skill_Icon_Claw")
    {
        return new SkillData
        {
            id = "attack_basic",
            skillName = name,
            iconSymbol = "//",
            iconAssetName = iconAsset,
            category = cat,
            effectPower = power,
            spCost = 0,
            description = "Ataque físico direto com dano concentrado.",
            minRange = 1,
            maxRange = range > 0 ? range : 2,
            rangeType = AttackShapeType.Single,
            heightToleranceUp = 1,
            heightToleranceDown = 1,
            aoeType = AttackShapeType.Single,
            aoeRadius = 0,
            aoeHeightToleranceUp = 1,
            aoeHeightToleranceDown = 1
        };
    }

    public static SkillData CreateSpecialSkill(string name, int power, int spCost, FunctionalCategory cat, string desc, int maxRange = 3, AttackShapeType aoe = AttackShapeType.Single, int aoeRadius = 0, string icon = "⚡", string iconAsset = "UI_Skill_Icon_MeteorShower")
    {
        return new SkillData
        {
            id = $"skill_{name.ToLower().Replace(" ", "_")}",
            skillName = name,
            iconSymbol = icon,
            iconAssetName = iconAsset,
            category = cat,
            effectPower = power,
            spCost = spCost,
            description = desc,
            minRange = 1,
            maxRange = maxRange,
            rangeType = AttackShapeType.Single,
            heightToleranceUp = 1,
            heightToleranceDown = 1,
            aoeType = aoe,
            aoeRadius = aoeRadius,
            aoeHeightToleranceUp = 1,
            aoeHeightToleranceDown = 1
        };
    }
}

[Serializable]
public class PassiveSkillData
{
    public string passiveName = "Pernas Poderosas";
    [TextArea(2, 3)]
    public string description = "Aumenta VELOC em um nível.";
    public string iconSymbol = "◈";

    public PassiveSkillData() { }

    public PassiveSkillData(string name, string desc, string icon = "◈")
    {
        passiveName = name;
        description = desc;
        iconSymbol = icon;
    }
}
