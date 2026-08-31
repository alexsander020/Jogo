using System;
using System.Collections.Generic;
using UnityEngine;

public struct CombatForecast
{
    public Unit attacker;
    public Unit defender;
    public SkillData skill;
    public int baseDamage;
    public int finalDamage;
    public AttackOrientation orientation;
    public float orientationMultiplier;
    public bool isCritical;
    public bool hasCategoryAdvantage;
    public bool hasCategoryDisadvantage;
    public float categoryMultiplier;
    public bool hasProtocolAdvantage;
    public bool hasProtocolDisadvantage;
    public float protocolMultiplier;
    public int elevationDiff;
    public float elevationMultiplier;
    public bool isSynchronizedCombo;
    public int defenderCurrentHp;
    public int defenderRemainingHp;
}

public static class CombatService
{
    // Rastreia alvos atacados na rodada para suporte ao Combo Sincronizado
    private static Dictionary<Unit, List<FunctionalCategory>> roundAttacksOnTarget = new Dictionary<Unit, List<FunctionalCategory>>();
    private static int lastTrackedRound = -1;

    public static void ResetRoundTracking(int currentRound)
    {
        if (lastTrackedRound != currentRound)
        {
            lastTrackedRound = currentRound;
            roundAttacksOnTarget.Clear();
        }
    }

    public static void RegisterAttackOnTarget(Unit target, FunctionalCategory category)
    {
        if (target == null) return;
        if (!roundAttacksOnTarget.ContainsKey(target))
        {
            roundAttacksOnTarget[target] = new List<FunctionalCategory>();
        }
        roundAttacksOnTarget[target].Add(category);
    }

    public static bool CheckSynchronizedCombo(Unit target, FunctionalCategory attackerCategory)
    {
        if (target == null) return false;
        if (roundAttacksOnTarget.TryGetValue(target, out var list))
        {
            return list.Contains(attackerCategory);
        }
        return false;
    }

    /// <summary>
    /// Calcula a previsão completa de combate entre o atacante e o defensor de acordo com o GDD NetShift V3.
    /// </summary>
    public static CombatForecast CalculateForecast(Unit attacker, Unit defender, SkillData skill = null)
    {
        CombatForecast forecast = new CombatForecast
        {
            attacker = attacker,
            defender = defender,
            skill = skill
        };

        if (attacker == null || defender == null) return forecast;

        int attackerAtk = attacker.stats != null ? attacker.stats.GetStat(StatEnum.ATK) : 25;
        int defenderDef = defender.stats != null ? defender.stats.GetStat(StatEnum.DEF) : 10;
        int defenderHp = defender.stats != null ? defender.stats.GetStat(StatEnum.HP) : 100;

        forecast.defenderCurrentHp = defenderHp;

        // 1. Dano Base: escala com o efeito da habilidade (se informada) ou (ATK * 2) - DEF
        int effectPower = (skill != null && skill.effectPower > 0) ? skill.effectPower : 85;
        float powerFactor = effectPower / 85.0f;
        int baseDamage = Mathf.Max(1, Mathf.RoundToInt((attackerAtk * 2 * powerFactor) - defenderDef));
        forecast.baseDamage = baseDamage;

        // 2. Orientação Posicional (Frontal / Flanco / Backstab)
        forecast.orientation = DirectionUtils.GetAttackOrientation(defender.facing, defender.gridPosition, attacker.gridPosition);
        forecast.orientationMultiplier = DirectionUtils.GetOrientationDamageMultiplier(forecast.orientation, out bool guaranteedCrit);

        forecast.isCritical = guaranteedCrit;

        // Chance de crítico aleatório se não for Backstab (ex: Game tem maior taxa de crítico)
        FunctionalCategory attackCategory = (skill != null) ? skill.category : attacker.category;
        if (!forecast.isCritical && attackCategory == FunctionalCategory.Game)
        {
            // Categoria Game tem bônus de crítico
            forecast.isCritical = true;
        }

        // 3. Ciclo dos 7 Atributos Funcionais
        // Social > Navi > Tool > Game > Entertainment > Life > System > Social
        forecast.hasCategoryAdvantage = DirectionUtils.HasCategoryAdvantage(attackCategory, defender.category);
        forecast.hasCategoryDisadvantage = DirectionUtils.HasCategoryAdvantage(defender.category, attackCategory);

        if (forecast.hasCategoryAdvantage)
        {
            forecast.categoryMultiplier = 1.50f; // +50%
        }
        else if (forecast.hasCategoryDisadvantage)
        {
            forecast.categoryMultiplier = 0.75f; // -25%
        }
        else
        {
            forecast.categoryMultiplier = 1.00f; // Neutro
        }

        // 4. Trindade de Protocolo (Karma)
        // Firewall (Vacina) > Overclock (Vírus) > Ping (Dados) > Firewall (Vacina)
        forecast.hasProtocolAdvantage = DirectionUtils.HasProtocolAdvantage(attacker.protocol, defender.protocol);
        forecast.hasProtocolDisadvantage = DirectionUtils.HasProtocolAdvantage(defender.protocol, attacker.protocol);

        if (forecast.hasProtocolAdvantage)
        {
            forecast.protocolMultiplier = 1.20f; // +20%
        }
        else if (forecast.hasProtocolDisadvantage)
        {
            forecast.protocolMultiplier = 0.85f; // -15%
        }
        else
        {
            forecast.protocolMultiplier = 1.00f; // Neutro
        }

        // 5. Elevação (Z)
        int attackerZ = attacker.currentTile != null && attacker.currentTile.floor != null && Board.instance != null
            ? Board.instance.floors.IndexOf(attacker.currentTile.floor)
            : 0;

        int defenderZ = defender.currentTile != null && defender.currentTile.floor != null && Board.instance != null
            ? Board.instance.floors.IndexOf(defender.currentTile.floor)
            : 0;

        forecast.elevationDiff = attackerZ - defenderZ;
        // Bônus de +10% por nível acima, -10% por nível abaixo
        forecast.elevationMultiplier = Mathf.Clamp(1.0f + (forecast.elevationDiff * 0.10f), 0.70f, 1.50f);

        // 6. Combo Sincronizado (2v2)
        forecast.isSynchronizedCombo = CheckSynchronizedCombo(defender, attacker.category);
        float comboMult = forecast.isSynchronizedCombo ? 1.30f : 1.00f;

        // 7. Modificador de Crítico
        float critMult = forecast.isCritical ? 1.30f : 1.00f;

        // 8. Cálculo Final
        float totalMultiplier = forecast.orientationMultiplier 
                              * forecast.categoryMultiplier 
                              * forecast.protocolMultiplier 
                              * forecast.elevationMultiplier 
                              * comboMult 
                              * critMult;

        forecast.finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * totalMultiplier));
        forecast.defenderRemainingHp = Mathf.Max(0, defenderHp - forecast.finalDamage);

        return forecast;
    }

    /// <summary>
    /// Retorna descrição legível das vantagens/desvantagens para exibição na UI.
    /// </summary>
    public static string GetForecastSummaryText(CombatForecast forecast)
    {
        string orientationStr = forecast.orientation switch
        {
            AttackOrientation.Backstab => "<color=#FFD700>TRASEIRO (x1.50 Crítico!)</color>",
            AttackOrientation.Flank => "<color=#FFA500>FLANCO (x1.25)</color>",
            _ => "FRONTAL (x1.00)"
        };

        string categoryStr = "";
        if (forecast.hasCategoryAdvantage)
        {
            categoryStr = $"<color=#00E5FF>Vantagem: {forecast.attacker.category} > {forecast.defender.category} (+50%)</color>";
        }
        else if (forecast.hasCategoryDisadvantage)
        {
            categoryStr = $"<color=#FF5252>Desvantagem: {forecast.attacker.category} < {forecast.defender.category} (-25%)</color>";
        }
        else
        {
            categoryStr = "Neutro (x1.00)";
        }

        string protocolStr = "";
        if (forecast.hasProtocolAdvantage)
        {
            protocolStr = $" • <color=#00E5FF>Karma +20%</color>";
        }
        else if (forecast.hasProtocolDisadvantage)
        {
            protocolStr = $" • <color=#FF5252>Karma -15%</color>";
        }

        string elevationStr = "";
        if (forecast.elevationDiff > 0)
        {
            elevationStr = $" • <color=#00E5FF>Elevação +{forecast.elevationDiff * 10}%</color>";
        }
        else if (forecast.elevationDiff < 0)
        {
            elevationStr = $" • <color=#FF5252>Elevação {forecast.elevationDiff * 10}%</color>";
        }

        string comboStr = forecast.isSynchronizedCombo ? "\n• <color=#FFD700>★ COMBO SINCRONIZADO ATIVADO! (Guard Break +30%)</color>" : "";

        return $"• ALVO: <b>{forecast.defender.unitName}</b> ({forecast.defender.category} / {forecast.defender.protocol})\n" +
               $"• DANO ESTIMADO: <b><size=15>{forecast.finalDamage}</size></b> (HP: {forecast.defenderCurrentHp} → <color=#00E5FF>{forecast.defenderRemainingHp}</color>)\n" +
               $"• POSIÇÃO: {orientationStr} • ELEMENTO: {categoryStr}{protocolStr}{elevationStr}{comboStr}\n" +
               $"• [ESPAÇO / ENTER / Z] : Executar Ataque    [X / ESC] : Cancelar";
    }
}
