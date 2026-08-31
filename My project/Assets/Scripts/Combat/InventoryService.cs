using System;
using System.Collections.Generic;
using UnityEngine;

public static class InventoryService
{
    private static List<ItemData> partyInventory;

    public static List<ItemData> GetInventory()
    {
        if (partyInventory == null || partyInventory.Count == 0)
        {
            InitializeDefaultInventory();
        }
        return partyInventory;
    }

    public static void InitializeDefaultInventory()
    {
        partyInventory = new List<ItemData>
        {
            new ItemData
            {
                id = "item_curativo",
                itemName = "Curativo",
                description = "Restaura levemente o HP do alvo. Uma bandagem de velha escola, pra ser correto. Alguém deixou cair aqui?",
                iconAssetName = "UI_Skill_Icon_Heal",
                effectType = ItemEffectType.HealHP,
                healAmount = 150,
                spAmount = 0,
                quantity = 7,
                minRange = 0,
                maxRange = 4,
                heightToleranceUp = 3,
                heightToleranceDown = 3,
                aoeType = TacticalBattle.Core.AttackShapeType.Single,
                aoeRadius = 0
            },
            new ItemData
            {
                id = "item_remedio",
                itemName = "Pacote de Remédio",
                description = "Restaura uma quantidade moderada de HP do alvo. Contém medicamentos de primeiros socorros essenciais.",
                iconAssetName = "UI_Skill_Icon_Buff",
                effectType = ItemEffectType.HealHP,
                healAmount = 400,
                spAmount = 0,
                quantity = 2,
                minRange = 0,
                maxRange = 4,
                heightToleranceUp = 3,
                heightToleranceDown = 3,
                aoeType = TacticalBattle.Core.AttackShapeType.Single,
                aoeRadius = 0
            },
            new ItemData
            {
                id = "item_coxa_frango",
                itemName = "Coxa de Frango",
                description = "Um alimento delicioso e nutritivo que restaura completamente o HP e recupera 50 SP de um aliado.",
                iconAssetName = "UI_Skill_Icon_Buff",
                effectType = ItemEffectType.HealAll,
                healAmount = 9999,
                spAmount = 50,
                quantity = 1,
                minRange = 0,
                maxRange = 3,
                heightToleranceUp = 2,
                heightToleranceDown = 2,
                aoeType = TacticalBattle.Core.AttackShapeType.Single,
                aoeRadius = 0
            }
        };
    }

    public static bool HasItem(string itemId)
    {
        var item = GetItem(itemId);
        return item != null && item.quantity > 0;
    }

    public static ItemData GetItem(string itemId)
    {
        var inv = GetInventory();
        return inv.Find(i => i.id == itemId);
    }

    public static bool ConsumeItem(ItemData item, Unit target)
    {
        if (item == null || item.quantity <= 0 || target == null || target.stats == null)
        {
            return false;
        }

        // Aplica o efeito no alvo
        switch (item.effectType)
        {
            case ItemEffectType.HealHP:
                int currentHp = target.stats.GetStat(StatEnum.HP);
                int maxHp = target.stats.GetStat(StatEnum.MaxHp);
                if (maxHp <= 0) maxHp = 500;
                int newHp = Mathf.Min(maxHp, currentHp + item.healAmount);
                int actualHealed = newHp - currentHp;
                target.stats.SetStat(StatEnum.HP, newHp);
                DamagePopupService.ShowDamage(target.transform.position, actualHealed, AttackOrientation.Frontal, false, false);
                Debug.Log($"[Inventário] {target.unitName} usou {item.itemName} e recuperou {actualHealed} HP! ({currentHp} -> {newHp})");
                break;

            case ItemEffectType.RestoreSP:
                int currentSp = target.stats.GetStat(StatEnum.SP);
                int maxSp = target.stats.GetStat(StatEnum.MaxSp);
                if (maxSp <= 0) maxSp = 100;
                int newSp = Mathf.Min(maxSp, currentSp + item.spAmount);
                target.stats.SetStat(StatEnum.SP, newSp);
                Debug.Log($"[Inventário] {target.unitName} usou {item.itemName} e recuperou {item.spAmount} SP!");
                break;

            case ItemEffectType.HealAll:
                int curHp = target.stats.GetStat(StatEnum.HP);
                int mHp = target.stats.GetStat(StatEnum.MaxHp);
                if (mHp <= 0) mHp = 500;
                target.stats.SetStat(StatEnum.HP, mHp);
                int curSp = target.stats.GetStat(StatEnum.SP);
                int mSp = target.stats.GetStat(StatEnum.MaxSp);
                if (mSp <= 0) mSp = 100;
                target.stats.SetStat(StatEnum.SP, Mathf.Min(mSp, curSp + item.spAmount));
                DamagePopupService.ShowDamage(target.transform.position, mHp - curHp, AttackOrientation.Frontal, false, false);
                Debug.Log($"[Inventário] {target.unitName} consumiu {item.itemName}! HP e SP restaurados.");
                break;
        }

        item.quantity--;
        return true;
    }
}
