using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalBattle.Appmon
{
    [Serializable]
    public class StatusEffectInstance
    {
        public StatusEffectType type;
        public int remainingTurns;
        public int value;

        public StatusEffectInstance(StatusEffectType type, int turns, int value = 0)
        {
            this.type = type;
            this.remainingTurns = turns;
            this.value = value;
        }
    }

    [RequireComponent(typeof(Unit))]
    public class AppmonCharacter : MonoBehaviour
    {
        public AppmonData appmonData;
        public List<StatusEffectInstance> activeStatuses = new List<StatusEffectInstance>();

        [Header("Estado de Passivas")]
        public int turnsOnField = 0;
        public int insatiableHungerStax = 0;
        public bool hasRevivedFromAshes = false;
        public bool didActThisTurn = false;
        public bool wasInvisibleBeforeAttack = false;
        public int teamDamageImmunityTurns = 0; // Para Black Tortoise Citadel / Celestial Harmony

        private Unit unit;

        void Awake()
        {
            unit = GetComponent<Unit>();
        }

        public void InitializeFromAppmon(string appmonNameOrId)
        {
            appmonData = AppmonDatabase.Get(appmonNameOrId);
            if (appmonData != null && unit != null)
            {
                appmonData.ApplyToUnit(unit);
            }
        }

        public void InitializeFromData(AppmonData data)
        {
            appmonData = data;
            if (appmonData != null && unit != null)
            {
                appmonData.ApplyToUnit(unit);
            }
        }

        // =========================================================================
        // GERENCIAMENTO DE STATUS EFFECTS
        // =========================================================================
        public bool HasStatus(StatusEffectType type)
        {
            return activeStatuses.Exists(s => s.type == type && s.remainingTurns > 0);
        }

        public void ApplyStatus(StatusEffectType type, int turns, int value = 0)
        {
            if (type == StatusEffectType.None) return;

            // Verificação de Imunidades de Passivas
            if (appmonData != null)
            {
                // Cancelamento de Ruído (Sonic-Debugger): Imune a debuffs
                if (appmonData.passiveId == "noise_cancellation" && IsDebuff(type))
                {
                    Debug.Log($"[Passiva] {unit.unitName} (Cancelamento de Ruído) é imune ao debuff {type}!");
                    return;
                }

                // Soberano dos Mares (Poseidon-Vipermon): Imune a status em Água
                if (appmonData.passiveId == "sovereign_of_seas" && IsOnWaterTile() && IsDebuff(type))
                {
                    Debug.Log($"[Passiva] {unit.unitName} (Soberano dos Mares) é imune ao debuff {type} em águas digitais!");
                    return;
                }

                // Bastião Inabalável (Dreadnoughtmon): Imune a Immobilized e Stun
                if (appmonData.passiveId == "unshakable_bastion" && (type == StatusEffectType.Immobilized || type == StatusEffectType.Stun))
                {
                    Debug.Log($"[Passiva] {unit.unitName} (Bastião Inabalável) é imune a {type}!");
                    return;
                }

                // Casco Inabalável (Genbu-Architectmon): Imune a Paralysis
                if (appmonData.passiveId == "unshakable_shell" && type == StatusEffectType.Paralysis)
                {
                    Debug.Log($"[Passiva] {unit.unitName} (Casco Inabalável) é imune a Paralisia!");
                    return;
                }
            }

            var existing = activeStatuses.Find(s => s.type == type);
            if (existing != null)
            {
                existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
                existing.value = Mathf.Max(existing.value, value);
            }
            else
            {
                activeStatuses.Add(new StatusEffectInstance(type, turns, value));
            }

            Debug.Log($"[Status] {unit.unitName} recebeu status: {type} por {turns} turnos.");
        }

        public void RemoveStatus(StatusEffectType type)
        {
            activeStatuses.RemoveAll(s => s.type == type);
        }

        public void ClearAllNegativeStatuses()
        {
            activeStatuses.RemoveAll(s => IsDebuff(s.type));
        }

        public static bool IsDebuff(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Immobilized => true,
                StatusEffectType.Stun => true,
                StatusEffectType.Silence => true,
                StatusEffectType.Sleep => true,
                StatusEffectType.DeepSleep => true,
                StatusEffectType.Burn => true,
                StatusEffectType.ChaosBurn => true,
                StatusEffectType.Paralysis => true,
                StatusEffectType.Blind => true,
                StatusEffectType.Panic => true,
                StatusEffectType.Bleed => true,
                StatusEffectType.GoldPetrification => true,
                StatusEffectType.Frozen => true,
                StatusEffectType.Confused => true,
                _ => false
            };
        }

        public bool CanMove()
        {
            if (HasStatus(StatusEffectType.Immobilized) ||
                HasStatus(StatusEffectType.Stun) ||
                HasStatus(StatusEffectType.Sleep) ||
                HasStatus(StatusEffectType.DeepSleep) ||
                HasStatus(StatusEffectType.Frozen) ||
                HasStatus(StatusEffectType.GoldPetrification))
            {
                return false;
            }
            return true;
        }

        public bool CanAct()
        {
            if (HasStatus(StatusEffectType.Stun) ||
                HasStatus(StatusEffectType.Sleep) ||
                HasStatus(StatusEffectType.DeepSleep) ||
                HasStatus(StatusEffectType.Frozen) ||
                HasStatus(StatusEffectType.GoldPetrification))
            {
                return false;
            }
            return true;
        }

        public bool CanUseMagicSkills()
        {
            if (HasStatus(StatusEffectType.Silence)) return false;
            return true;
        }

        // =========================================================================
        // CICLO DE TURNOS & GATILHOS DE PASSIVAS
        // =========================================================================
        public void OnTurnStart()
        {
            turnsOnField++;
            didActThisTurn = false;
            wasInvisibleBeforeAttack = HasStatus(StatusEffectType.Invisible);

            if (teamDamageImmunityTurns > 0)
            {
                teamDamageImmunityTurns--;
            }

            // Reduz duração dos status e aplica efeitos contínuos
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                var st = activeStatuses[i];

                if (st.type == StatusEffectType.Burn)
                {
                    int burnDmg = Mathf.Max(5, (unit.stats != null ? unit.stats.GetStat(StatEnum.MaxHp) : 100) / 10);
                    unit.TakeDamage(burnDmg);
                    Debug.Log($"[Status Burn] {unit.unitName} sofreu {burnDmg} de Queimadura!");
                }
                else if (st.type == StatusEffectType.ChaosBurn)
                {
                    int chaosDmg = Mathf.Max(15, (unit.stats != null ? unit.stats.GetStat(StatEnum.MaxHp) : 100) / 5);
                    unit.TakeDamage(chaosDmg);
                    Debug.Log($"[Status Fogo do Caos] {unit.unitName} sofreu {chaosDmg} de Fogo do Caos!");
                }
                else if (st.type == StatusEffectType.Bleed)
                {
                    int bleedDmg = 15;
                    unit.TakeDamage(bleedDmg);
                    Debug.Log($"[Status Sangramento] {unit.unitName} sofreu {bleedDmg} de Sangramento!");
                }
                else if (st.type == StatusEffectType.GoldPetrification)
                {
                    int petrifyDmg = Mathf.RoundToInt((unit.stats != null ? unit.stats.GetStat(StatEnum.MaxHp) : 100) * 0.15f);
                    unit.TakeDamage(petrifyDmg);
                    Debug.Log($"[Status Petrificação de Ouro] {unit.unitName} perdeu 15% de HP ({petrifyDmg})!");
                }

                st.remainingTurns--;
                if (st.remainingTurns <= 0)
                {
                    activeStatuses.RemoveAt(i);
                    Debug.Log($"[Status] Status {st.type} expirou em {unit.unitName}.");
                }
            }

            // Passiva: Regeneração (Bio-Patch)
            if (appmonData != null && appmonData.passiveId == "regeneration" && unit.stats != null)
            {
                int maxHp = unit.stats.GetStat(StatEnum.MaxHp);
                int heal = Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.05f));
                int currentHp = unit.stats.GetStat(StatEnum.HP);
                unit.stats.SetStat(StatEnum.HP, Mathf.Min(maxHp, currentHp + heal));
                Debug.Log($"[Passiva Regeneração] {unit.unitName} recuperou {heal} HP no início do turno.");
            }

            // Passiva: Domínio Aquático (Hydro-Vipermon) em Terreno Alagado
            if (appmonData != null && appmonData.passiveId == "aquatic_dominion" && IsOnWaterTile() && unit.stats != null)
            {
                int maxHp = unit.stats.GetStat(StatEnum.MaxHp);
                int heal = Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.05f));
                int currentHp = unit.stats.GetStat(StatEnum.HP);
                unit.stats.SetStat(StatEnum.HP, Mathf.Min(maxHp, currentHp + heal));
                Debug.Log($"[Passiva Domínio Aquático] {unit.unitName} recuperou {heal} HP por estar em terreno alagado.");
            }

            // Terreno: Tile Energizado (Charge Tile)
            if (unit.currentTile != null && unit.currentTile.terrainType == TerrainType.ChargeTile && unit.stats != null)
            {
                int maxMp = unit.stats.GetStat(StatEnum.MaxMp);
                int mpGain = Mathf.Max(1, Mathf.RoundToInt(maxMp * 0.10f));
                int currentMp = unit.stats.GetStat(StatEnum.MP);
                unit.stats.SetStat(StatEnum.MP, Mathf.Min(maxMp, currentMp + mpGain));
                Debug.Log($"[Tile Energizado] {unit.unitName} recuperou {mpGain} MP.");
            }
        }

        public void OnTurnEnd()
        {
            // Passiva: Modo de Espera (Belphemon)
            if (appmonData != null && appmonData.passiveId == "standby_mode" && !didActThisTurn && unit.stats != null)
            {
                int maxHp = unit.stats.GetStat(StatEnum.MaxHp);
                int heal = Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.10f));
                int currentHp = unit.stats.GetStat(StatEnum.HP);
                unit.stats.SetStat(StatEnum.HP, Mathf.Min(maxHp, currentHp + heal));
                Debug.Log($"[Passiva Modo de Espera] {unit.unitName} descansou: curou {heal} HP e ganhou +40% de DEF!");
            }
        }

        public void OnActionExecuted()
        {
            didActThisTurn = true;
            // Se estava invisível e atacou, encerra a invisibilidade
            if (HasStatus(StatusEffectType.Invisible))
            {
                RemoveStatus(StatusEffectType.Invisible);
            }
        }

        public void OnEnemyDefeated()
        {
            // Passiva: Execução Perfeita (Omega-Debugger)
            if (appmonData != null && appmonData.passiveId == "perfect_execution")
            {
                Debug.Log($"[Passiva Execução Perfeita] {unit.unitName} eliminou um inimigo! Todas as recargas foram redefinidas.");
            }

            // Passiva: Fome Insaciável (Beelzebumon)
            if (appmonData != null && appmonData.passiveId == "insatiable_hunger" && unit.stats != null)
            {
                insatiableHungerStax++;
                int bonusAtk = Mathf.RoundToInt(appmonData.atk * 0.10f);
                unit.stats.ModifyStat(StatEnum.ATK, bonusAtk);
                Debug.Log($"[Passiva Fome Insaciável] {unit.unitName} abateu uma unidade! ATK aumentou em +{bonusAtk} permanentemente.");
            }
        }

        public bool TryTriggerRebirth()
        {
            // Passiva: Renascer das Cinzas (Suzaku-Beatmon)
            if (appmonData != null && appmonData.passiveId == "rebirth_from_ashes" && !hasRevivedFromAshes && unit.stats != null)
            {
                hasRevivedFromAshes = true;
                int maxHp = unit.stats.GetStat(StatEnum.MaxHp);
                int reviveHp = Mathf.Max(1, maxHp / 2);
                unit.stats.SetStat(StatEnum.HP, reviveHp);
                Debug.Log($"[Passiva Renascer das Cinzas] ★ {unit.unitName} renasceu imediatamente das cinzas com {reviveHp} HP!");
                return true;
            }
            return false;
        }

        public void OnDefeated()
        {
            // Passiva: Combustão (Flame-Log)
            if (appmonData != null && appmonData.passiveId == "combustion" && unit.currentTile != null && Board.instance != null)
            {
                Debug.Log($"[Passiva Combustão] {unit.unitName} caiu em combate e liberou explosão de fogo nos tiles adjacentes!");
                var neighbors = Board.instance.GetNeighborTiles(unit.currentTile);
                foreach (var tile in neighbors)
                {
                    if (tile != null)
                    {
                        tile.terrainType = TerrainType.Fire;
                        if (tile.content != null)
                        {
                            var targetUnit = tile.content.GetComponent<Unit>();
                            if (targetUnit != null && targetUnit != unit)
                            {
                                targetUnit.TakeDamage(30);
                            }
                        }
                    }
                }
            }
        }

        public bool IsOnWaterTile()
        {
            if (unit == null || unit.currentTile == null) return false;
            return unit.currentTile.terrainType == TerrainType.Flooded || unit.currentTile.terrainType == TerrainType.DigitalOcean;
        }

        public bool IsOnMagmaTile()
        {
            if (unit == null || unit.currentTile == null) return false;
            return unit.currentTile.terrainType == TerrainType.Magma;
        }

        public int GetEffectiveMovement()
        {
            int baseMov = unit.stats != null ? unit.stats.GetStat(StatEnum.MOV) : 3;

            // Piroclasto (Magma-Logmon): +2 MOV em Magma
            if (appmonData != null && appmonData.passiveId == "pyroclast" && IsOnMagmaTile())
            {
                baseMov += 2;
            }

            // Pressão Eletro-Aquática (Seiryu-Vipermon): +25% SPD em Alagado ou Eletrificado
            // Carga Polar: Inimigos adjacentes ao Magnet-Core perdem 2 MOV
            if (IsAdjacentToEnemyWithPolarCharge())
            {
                baseMov = Mathf.Max(1, baseMov - 2);
            }

            // Oceano Digital reduz MOV de inimigos em 50%
            if (unit.currentTile != null && unit.currentTile.terrainType == TerrainType.DigitalOcean)
            {
                if (appmonData == null || appmonData.passiveId != "sovereign_of_seas")
                {
                    baseMov = Mathf.Max(1, baseMov / 2);
                }
            }

            return baseMov;
        }

        private bool IsAdjacentToEnemyWithPolarCharge()
        {
            if (unit == null || unit.currentTile == null || Board.instance == null) return false;
            var neighbors = Board.instance.GetNeighborTiles(unit.currentTile);
            foreach (var tile in neighbors)
            {
                if (tile != null && tile.content != null)
                {
                    var other = tile.content.GetComponent<Unit>();
                    if (other != null && other.team != unit.team)
                    {
                        var appmon = other.GetComponent<AppmonCharacter>();
                        if (appmon != null && appmon.appmonData != null && appmon.appmonData.passiveId == "polar_charge")
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
