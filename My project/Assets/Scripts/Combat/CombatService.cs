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

        var attackerAppmon = attacker.GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        var defenderAppmon = defender.GetComponent<TacticalBattle.Appmon.AppmonCharacter>();

        bool isMagic = skill != null && skill.isMagic;

        // 1. Determina atributos ofensivos e defensivos (Físico: ATK vs DEF / Mágico: INT vs SPI)
        int attackerOffense = 25;
        int defenderDefense = 10;

        if (attacker.stats != null)
        {
            attackerOffense = isMagic ? attacker.stats.GetStat(StatEnum.INT) : attacker.stats.GetStat(StatEnum.ATK);
        }

        if (defender.stats != null)
        {
            defenderDefense = isMagic ? defender.stats.GetStat(StatEnum.SPI) : defender.stats.GetStat(StatEnum.DEF);
        }

        int defenderHp = defender.stats != null ? defender.stats.GetStat(StatEnum.HP) : 100;
        forecast.defenderCurrentHp = defenderHp;

        // Passiva: Fúria Descontrolada (Satanmon) -> +2% ATK para cada 1% de HP perdido
        if (attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "uncontrolled_fury" && attacker.stats != null)
        {
            int maxHp = attacker.stats.GetStat(StatEnum.MaxHp);
            int curHp = attacker.stats.GetStat(StatEnum.HP);
            float lostHpPercent = Mathf.Clamp01(1f - ((float)curHp / Mathf.Max(1, maxHp)));
            float furyBonus = lostHpPercent * 2.0f; // ex: 50% HP perdido = +100% ATK
            attackerOffense = Mathf.RoundToInt(attackerOffense * (1.0f + furyBonus));
        }

        // Passiva: Hidrodinâmica (Shitakumon) -> +5% ATK por turno
        if (attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "hydrodynamics")
        {
            float bonus = 0.05f * attackerAppmon.turnsOnField;
            attackerOffense = Mathf.RoundToInt(attackerOffense * (1.0f + bonus));
        }

        // Passiva: Casco Inabalável (Genbu-Architectmon) -> Transforma 15% da DEF em bônus
        if (attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "unshakable_shell" && attacker.stats != null)
        {
            int defVal = attacker.stats.GetStat(StatEnum.DEF);
            attackerOffense += Mathf.RoundToInt(defVal * 0.15f);
        }

        // 2. Dano Base: escala com o efeito da habilidade (se informada) ou (Offense * 2) - Defense
        int effectPower = (skill != null && skill.effectPower > 0) ? skill.effectPower : 85;
        float powerFactor = effectPower / 85.0f;
        int baseDamage = Mathf.Max(1, Mathf.RoundToInt((attackerOffense * 2 * powerFactor) - defenderDefense));

        // Passiva: Soberba (Lucifermon) -> +30% de dano base se alvo tem menor % de HP
        if (attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "pride_arrogance" && attacker.stats != null && defender.stats != null)
        {
            float atkHpPct = (float)attacker.stats.GetStat(StatEnum.HP) / Mathf.Max(1, attacker.stats.GetStat(StatEnum.MaxHp));
            float defHpPct = (float)defender.stats.GetStat(StatEnum.HP) / Mathf.Max(1, defender.stats.GetStat(StatEnum.MaxHp));
            if (atkHpPct > defHpPct)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * 1.30f);
            }
        }

        forecast.baseDamage = baseDamage;

        // 3. Orientação Posicional (Frontal / Flanco / Backstab)
        forecast.orientation = DirectionUtils.GetAttackOrientation(defender.facing, defender.gridPosition, attacker.gridPosition);
        forecast.orientationMultiplier = DirectionUtils.GetOrientationDamageMultiplier(forecast.orientation, out bool guaranteedCrit);

        forecast.isCritical = guaranteedCrit;

        // Passiva: Lente Condutora (Electro-Cammon) -> Crítico garantido se alvo paralisado
        if (attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "conductive_lens")
        {
            if (defenderAppmon != null && defenderAppmon.HasStatus(StatusEffectType.Paralysis))
            {
                forecast.isCritical = true;
            }
        }

        // Passiva: Foco Oculto (Shadow-Cam) -> Crítico garantido saindo da invisibilidade
        if (attackerAppmon != null && attackerAppmon.wasInvisibleBeforeAttack)
        {
            forecast.isCritical = true;
        }

        // Chance de Crítico Base (CRT %)
        if (!forecast.isCritical && attacker.stats != null)
        {
            int crtVal = attacker.stats.GetStat(StatEnum.CRT);
            if (UnityEngine.Random.Range(0, 100) < crtVal)
            {
                forecast.isCritical = true;
            }
        }

        // 4. Ciclo dos Atributos Funcionais
        FunctionalCategory attackCategory = (skill != null) ? skill.category : attacker.category;
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

        // 5. Trindade de Protocolo (Karma)
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

        // 6. Elevação (Z)
        int attackerZ = attacker.currentTile != null && attacker.currentTile.floor != null && Board.instance != null
            ? Board.instance.floors.IndexOf(attacker.currentTile.floor)
            : 0;

        int defenderZ = defender.currentTile != null && defender.currentTile.floor != null && Board.instance != null
            ? Board.instance.floors.IndexOf(defender.currentTile.floor)
            : 0;

        forecast.elevationDiff = attackerZ - defenderZ;
        forecast.elevationMultiplier = Mathf.Clamp(1.0f + (forecast.elevationDiff * 0.10f), 0.70f, 1.50f);

        // 7. Combo Sincronizado (2v2)
        forecast.isSynchronizedCombo = CheckSynchronizedCombo(defender, attacker.category);
        float comboMult = forecast.isSynchronizedCombo ? 1.30f : 1.00f;

        // 8. Modificador de Crítico (Predador Supremo de Byakko: 1.80x em vez de 1.30x)
        float critMult = 1.00f;
        if (forecast.isCritical)
        {
            bool isApex = attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "apex_predator";
            critMult = isApex ? 1.80f : 1.30f;
        }

        // 9. Modificadores de Terreno e Passivas Defensivas
        float specialMod = 1.00f;

        // Oceano Digital: Habilidades de Água causam dano triplo (3x)
        if (defender.currentTile != null && defender.currentTile.terrainType == TerrainType.DigitalOcean && attackCategory == FunctionalCategory.Security)
        {
            specialMod *= 3.0f;
        }
        // Terreno Alagado: Ataques não-Água têm dano reduzido em 30%
        else if (defender.currentTile != null && defender.currentTile.terrainType == TerrainType.Flooded && attackCategory != FunctionalCategory.Security)
        {
            specialMod *= 0.70f;
        }

        // Passiva Sobrecharge (Volt-Plug): Ataques elétricos causam +20% em Alagados
        if (attackerAppmon != null && attackerAppmon.appmonData != null && attackerAppmon.appmonData.passiveId == "supercharge")
        {
            if (defender.currentTile != null && defender.currentTile.terrainType == TerrainType.Flooded)
            {
                specialMod *= 1.20f;
            }
        }

        // Passiva Cód. Defensivo (Data-Viper): Reduz dano de ataques à distância em 15%
        int combatDist = Mathf.Abs(attacker.gridPosition.x - defender.gridPosition.x) + Mathf.Abs(attacker.gridPosition.y - defender.gridPosition.y);
        if (combatDist > 1 && defenderAppmon != null && defenderAppmon.appmonData != null && defenderAppmon.appmonData.passiveId == "defensive_code")
        {
            specialMod *= 0.85f;
        }

        // Passiva Encantamento Enganoso (Asmodeusmon): Atacantes sob status causam 50% menos dano
        if (defenderAppmon != null && defenderAppmon.appmonData != null && defenderAppmon.appmonData.passiveId == "deceptive_charm")
        {
            if (attackerAppmon != null && (attackerAppmon.HasStatus(StatusEffectType.Confused) ||
                                           attackerAppmon.HasStatus(StatusEffectType.Paralysis) ||
                                           attackerAppmon.HasStatus(StatusEffectType.Blind)))
            {
                specialMod *= 0.50f;
            }
        }

        // 10. Cálculo Final
        float totalMultiplier = forecast.orientationMultiplier 
                              * forecast.categoryMultiplier 
                              * forecast.protocolMultiplier 
                              * forecast.elevationMultiplier 
                              * comboMult 
                              * critMult
                              * specialMod;

        forecast.finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * totalMultiplier));
        forecast.defenderRemainingHp = Mathf.Max(0, defenderHp - forecast.finalDamage);

        return forecast;
    }

    public static void ApplyCombatEffects(Unit attacker, Unit defender, SkillData skill, int damageDealt)
    {
        if (attacker == null || defender == null) return;

        var attackerAppmon = attacker.GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
        var defenderAppmon = defender.GetComponent<TacticalBattle.Appmon.AppmonCharacter>();

        if (attackerAppmon != null) attackerAppmon.OnActionExecuted();

        // Se o defensor foi abatido
        if (!defender.IsAlive)
        {
            if (attackerAppmon != null)
            {
                attackerAppmon.OnEnemyDefeated();
            }
        }

        // Aplica efeitos de status da habilidade
        if (skill != null && skill.statusToApply != StatusEffectType.None && defenderAppmon != null)
        {
            if (UnityEngine.Random.value <= skill.statusChance)
            {
                defenderAppmon.ApplyStatus(skill.statusToApply, skill.statusDurationTurns);
            }
        }

        // Aplica criação ou modificação de terreno
        if (skill != null && skill.hasTerrainCreation && Board.instance != null)
        {
            TileLogic targetTile = defender.currentTile ?? attacker.currentTile;
            if (targetTile != null)
            {
                targetTile.terrainType = skill.createsTerrain;
                if (skill.terrainRadius > 0)
                {
                    foreach (var tile in Board.instance.tiles.Values)
                    {
                        if (tile != null)
                        {
                            int d = Mathf.Abs(tile.pos.x - targetTile.pos.x) + Mathf.Abs(tile.pos.y - targetTile.pos.y);
                            if (d <= skill.terrainRadius)
                            {
                                tile.terrainType = skill.createsTerrain;
                            }
                        }
                    }
                }
            }
        }

        // Passiva Olhar Invejoso (Leviathanmon): Inimigos em até 3 tiles que usarem curas/buffs sofrem 15% reflexo
        if (skill != null && skill.healsTarget && Board.instance != null)
        {
            foreach (var tile in Board.instance.tiles.Values)
            {
                if (tile != null && tile.content != null)
                {
                    var other = tile.content.GetComponent<Unit>();
                    if (other != null && other.team != attacker.team)
                    {
                        var appmon = other.GetComponent<TacticalBattle.Appmon.AppmonCharacter>();
                        if (appmon != null && appmon.appmonData != null && appmon.appmonData.passiveId == "envious_glare")
                        {
                            int dist = Mathf.Abs(attacker.gridPosition.x - other.gridPosition.x) + Mathf.Abs(attacker.gridPosition.y - other.gridPosition.y);
                            if (dist <= 3)
                            {
                                int reflectDmg = Mathf.Max(1, Mathf.RoundToInt(skill.effectPower * 0.15f));
                                attacker.TakeDamage(reflectDmg);
                                Debug.Log($"[Olhar Invejoso] {attacker.unitName} curou e sofreu {reflectDmg} de dano reflexo de {other.unitName}!");
                            }
                        }
                    }
                }
            }
        }
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
