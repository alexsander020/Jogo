using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Combat
{
    public static class CombinedAttackService
    {
        public static readonly Dictionary<CombinedAttackId, CombinedAttackDefinition> Catalog = 
            new Dictionary<CombinedAttackId, CombinedAttackDefinition>
        {
            {
                CombinedAttackId.HydroSonicShockwave,
                new CombinedAttackDefinition(
                    CombinedAttackId.HydroSonicShockwave,
                    "Hydro-Sonic Shockwave",
                    "Hydro-Vipermon", "Sonic-Debugger",
                    "Hydro-Vipermon ter criado [Terreno Alagado] e Sonic-Debugger estar adjacente.",
                    "Sonic-Debugger lança uma onda de som que viaja através da água, atingindo todos os inimigos em Tiles [Alagados]. Causa dano massivo de Água/Som e aplica [Atordoado] garantido por 1 turno."
                )
            },
            {
                CombinedAttackId.AegisThermalBarricade,
                new CombinedAttackDefinition(
                    CombinedAttackId.AegisThermalBarricade,
                    "Aegis Thermal Barricade",
                    "Architectmon", "Magma-Logmon",
                    "Architectmon ergue uma [Muralha de Código] e Magma-Logmon posiciona-se no Tile adjacente.",
                    "Magma-Logmon funde sua lava à muralha. A barreira inflige dano de Fogo contínuo aos inimigos adjacentes e reflete 50% do dano físico/mágico recebido de volta aos atacantes."
                )
            },
            {
                CombinedAttackId.OverclockedStealthAmbush,
                new CombinedAttackDefinition(
                    CombinedAttackId.OverclockedStealthAmbush,
                    "Overclocked Stealth Ambush",
                    "Electro-Cammon", "Glitch-Hound",
                    "Electro-Cammon posicionado em [Stealth Grid] e Glitch-Hound em Tile adjacente.",
                    "Electro-Cammon energiza Glitch-Hound com camuflagem. Glitch-Hound salta 5 Tiles usando Frame Skip sem ser detectado e desfere um ataque crítico elétrico em cadeia que atinge até 3 inimigos adjacentes."
                )
            },
            {
                CombinedAttackId.PolarMagneticVortex,
                new CombinedAttackDefinition(
                    CombinedAttackId.PolarMagneticVortex,
                    "Polar Magnetic Vortex",
                    "Bio-Magnetmon", "Shitakumon",
                    "Área 3x3 afetada por [Parede de Água] de Shitakumon.",
                    "Bio-Magnetmon ativa um pulso magnético no centro da água. Todos os inimigos do mapa são puxados para a [Parede de Água], recebem dano esmagador de pressão e têm sua SPD reduzida a 0 por 1 turno."
                )
            },
            {
                CombinedAttackId.CelestialHarmonyArray,
                new CombinedAttackDefinition(
                    CombinedAttackId.CelestialHarmonyArray,
                    "Celestial Harmony Array",
                    "Genbu-Architectmon", "Seiryu-Vipermon",
                    "Genbu em [Mapeamento Rochoso] e Seiryu em terreno [Alagado] ou [Eletrificado].",
                    "Genbu cria uma cúpula inexpugnável enquanto Seiryu canaliza uma tempestade. A equipe fica 100% imune a dano por 1 turno e todos os inimigos sofrem dano massivo de Água/Trovão, ficando com SPD reduzida a 0 no próximo turno."
                )
            },
            {
                CombinedAttackId.VermilionThunderStorm,
                new CombinedAttackDefinition(
                    CombinedAttackId.VermilionThunderStorm,
                    "Vermilion Thunder Storm",
                    "Suzaku-Beatmon", "Seiryu-Vipermon",
                    "Campo afetado por [Pista Térmica] e [Terreno Eletrificado].",
                    "Suzaku aquece o ar amplificando os raios de Seiryu. Causa explosões térmico-elétricas em todos os Tiles ocupados por inimigos, aplicando [Queimadura] e [Paralisia] simultaneamente por 2 turnos."
                )
            },
            {
                CombinedAttackId.WhiteTigerFortressShred,
                new CombinedAttackDefinition(
                    CombinedAttackId.WhiteTigerFortressShred,
                    "White Tiger Fortress Shred",
                    "Byakko-Houndmon", "Genbu-Architectmon",
                    "Byakko posicionado sobre construções ativas de Genbu.",
                    "Byakko usa a altura das muralhas como plataforma de impulso, saltando sobre o mapa inteiro. Desfere um corte devastador que destrói todas as estruturas inimigas e causa 100% de dano crítico a todas as unidades inimigas."
                )
            },
            {
                CombinedAttackId.OmegaTsunamiOverdrive,
                new CombinedAttackDefinition(
                    CombinedAttackId.OmegaTsunamiOverdrive,
                    "Omega Tsunami Overdrive",
                    "Omega-Debugger", "Poseidon-Vipermon",
                    "Executado dentro da área do [Oceano Digital] de Poseidon.",
                    "Omega-Debugger injeta código de otimização pura nas águas de Poseidon. O Oceano Digital entra em ebulição binária, causando dano supremo que deleta buffs de status e reseta as habilidades de todos os aliados no campo."
                )
            },
            {
                CombinedAttackId.CataclysmicPandemonium,
                new CombinedAttackDefinition(
                    CombinedAttackId.CataclysmicPandemonium,
                    "Cataclysmic Pandemonium",
                    "Satanmon", "Lucifermon",
                    "Satanmon com menos de 30% de HP e adjacente a Lucifermon.",
                    "Satanmon sacrifica metade do seu HP restante para energizar a lâmina de Lucifermon. Lucifermon dispara uma onda de choque de antimatéria incinerando terrenos para [Fogo do Caos] e reduzindo DEF e SPI de todos os oponentes em 50% por 3 turnos."
                )
            },
            {
                CombinedAttackId.AbyssalDevourer,
                new CombinedAttackDefinition(
                    CombinedAttackId.AbyssalDevourer,
                    "Abyssal Devourer",
                    "Beelzebumon", "Leviathanmon",
                    "Inimigo localizado em [Água Corrompida].",
                    "Leviathanmon prende o inimigo sob águas corrompidas enquanto Beelzebumon salta e desfere mordida fatal. Causa dano físico e mágico massivo, transferindo 100% do HP e MP drenados para dividi-los entre ambos os atacantes."
                )
            },
            {
                CombinedAttackId.GoldenSlothTrap,
                new CombinedAttackDefinition(
                    CombinedAttackId.GoldenSlothTrap,
                    "Golden Sloth Trap",
                    "Mammonmon", "Belphemon",
                    "Inimigo sobre uma [Armadilha Dourada] dentro da [Zona de Inércia].",
                    "A armadilha se funde à aura de sono. O alvo entra em petrificação de ouro e [Sono Profundo] inquebrável por 3 turnos, perdendo 15% do seu HP total a cada turno que permanece petrificado."
                )
            },
            {
                CombinedAttackId.PhantasmagoricLustFire,
                new CombinedAttackDefinition(
                    CombinedAttackId.PhantasmagoricLustFire,
                    "Phantasmagoric Lust Fire",
                    "Asmodeusmon", "Flame-Log",
                    "Executado em Tiles afetados por [Espelhos Falsos] e [Terreno de Fogo].",
                    "Asmodeusmon projeta ilusões flamejantes. Inimigos confundidos caminham para Tiles de Fogo recebendo dano triplicado e repassando fogo para aliados próximos."
                )
            },
            {
                CombinedAttackId.ToxicSystemCorruption,
                new CombinedAttackDefinition(
                    CombinedAttackId.ToxicSystemCorruption,
                    "Toxic System Corruption",
                    "Data-Viper", "Satanmon",
                    "Alvo aprisionado em [Quarantine Lock].",
                    "Data-Viper tranca o oponente na jaula de código enquanto Satanmon injeta fúria sobreaquecida. A jaula explode, destruindo a proteção do alvo e aplicando [Pânico] e [Queimadura contínua] por 3 turnos."
                )
            },
            {
                CombinedAttackId.AbsoluteZeroOverclock,
                new CombinedAttackDefinition(
                    CombinedAttackId.AbsoluteZeroOverclock,
                    "Absolute Zero Overclock",
                    "Volt-Plug", "Belphemon",
                    "Volt-Plug posicionado em um [Charge Tile] energizado.",
                    "Volt-Plug canaliza a energia diretamente para Belphemon. Belphemon dispara um raio estático em congelamento absoluto atingindo 4 Tiles em cruz, paralisando e congelando todos os inimigos por 2 turnos."
                )
            }
        };

        // =========================================================================
        // VALIDAÇÃO DE CONDIÇÃO DOS COMBOS
        // =========================================================================
        public static bool CanPerformCombo(
            CombinedAttackId id,
            Unit initiator,
            Unit partner,
            Unit target = null)
        {
            if (initiator == null || partner == null || !initiator.IsAlive || !partner.IsAlive)
            {
                return false;
            }

            if (!Catalog.TryGetValue(id, out var def)) return false;

            // Valida se os participantes batem com os nomes requeridos
            bool participantsMatch =
                (initiator.unitName.Contains(def.participantA) && partner.unitName.Contains(def.participantB)) ||
                (initiator.unitName.Contains(def.participantB) && partner.unitName.Contains(def.participantA));

            if (!participantsMatch) return false;

            int dist = GetManhattanDistance(initiator.gridPosition, partner.gridPosition);

            switch (id)
            {
                case CombinedAttackId.HydroSonicShockwave:
                    // Sonic-Debugger adjacente e Terreno Alagado presente
                    return dist <= 1 && (IsTileType(initiator.currentTile, TerrainType.Flooded) || IsTileType(partner.currentTile, TerrainType.Flooded) || HasAnyTileOfType(TerrainType.Flooded));

                case CombinedAttackId.AegisThermalBarricade:
                    // Adjacente e Architectmon com Muralha de Código no local ou adjacente
                    return dist <= 1 && (IsAdjacentToTileType(initiator.gridPosition, TerrainType.CodeWall) || IsAdjacentToTileType(partner.gridPosition, TerrainType.CodeWall));

                case CombinedAttackId.OverclockedStealthAmbush:
                    // Electro-Cammon em StealthGrid e Glitch-Hound adjacente
                    Unit electro = initiator.unitName.Contains("Electro-Cammon") ? initiator : partner;
                    return dist <= 1 && IsTileType(electro.currentTile, TerrainType.StealthGrid);

                case CombinedAttackId.PolarMagneticVortex:
                    // Área afetada por Parede de Água (Flooded)
                    return HasAnyTileOfType(TerrainType.Flooded);

                case CombinedAttackId.CelestialHarmonyArray:
                    // Genbu em Mapeamento Rochoso e Seiryu em Alagado ou Eletrificado
                    Unit genbu = initiator.unitName.Contains("Genbu") ? initiator : partner;
                    Unit seiryu = initiator.unitName.Contains("Seiryu") ? initiator : partner;
                    return IsTileType(genbu.currentTile, TerrainType.RockyMapping) &&
                           (IsTileType(seiryu.currentTile, TerrainType.Flooded) || IsTileType(seiryu.currentTile, TerrainType.Electrified));

                case CombinedAttackId.VermilionThunderStorm:
                    // Campo com Pista Térmica e Terreno Eletrificado
                    return HasAnyTileOfType(TerrainType.ThermalTrack) && HasAnyTileOfType(TerrainType.Electrified);

                case CombinedAttackId.WhiteTigerFortressShred:
                    // Byakko posicionado sobre construções ativas (CodeWall ou RockyMapping ou ElevatedPlatform)
                    Unit byakko = initiator.unitName.Contains("Byakko") ? initiator : partner;
                    return IsTileType(byakko.currentTile, TerrainType.RockyMapping) ||
                           IsTileType(byakko.currentTile, TerrainType.ElevatedPlatform) ||
                           IsAdjacentToTileType(byakko.gridPosition, TerrainType.CodeWall);

                case CombinedAttackId.OmegaTsunamiOverdrive:
                    // Executado dentro do Oceano Digital
                    return IsTileType(initiator.currentTile, TerrainType.DigitalOcean) || IsTileType(partner.currentTile, TerrainType.DigitalOcean);

                case CombinedAttackId.CataclysmicPandemonium:
                    // Satanmon < 30% HP e adjacente a Lucifermon
                    Unit satan = initiator.unitName.Contains("Satanmon") ? initiator : partner;
                    int maxHp = satan.stats != null ? satan.stats.GetStat(StatEnum.MaxHp) : 2200;
                    int curHp = satan.stats != null ? satan.stats.GetStat(StatEnum.HP) : 2200;
                    return dist <= 1 && ((float)curHp / maxHp) < 0.30f;

                case CombinedAttackId.AbyssalDevourer:
                    // Inimigo localizado em Água Corrompida
                    return target != null && IsTileType(target.currentTile, TerrainType.CorruptedWater);

                case CombinedAttackId.GoldenSlothTrap:
                    // Inimigo sobre Armadilha Dourada dentro de Zona de Inércia (ou tile Armadilha Dourada)
                    return target != null && (IsTileType(target.currentTile, TerrainType.GoldenTrap) || IsTileType(target.currentTile, TerrainType.InertiaZone));

                case CombinedAttackId.PhantasmagoricLustFire:
                    // Campo com Espelhos Falsos e Terreno de Fogo
                    return HasAnyTileOfType(TerrainType.FalseMirrors) && HasAnyTileOfType(TerrainType.Fire);

                case CombinedAttackId.ToxicSystemCorruption:
                    // Alvo aprisionado em Quarantine Lock (Immobilized)
                    if (target == null) return false;
                    var appmonTarget = target.GetComponent<AppmonCharacter>();
                    return appmonTarget != null && appmonTarget.HasStatus(StatusEffectType.Immobilized);

                case CombinedAttackId.AbsoluteZeroOverclock:
                    // Volt-Plug em Charge Tile
                    Unit volt = initiator.unitName.Contains("Volt-Plug") ? initiator : partner;
                    return IsTileType(volt.currentTile, TerrainType.ChargeTile);

                default:
                    return false;
            }
        }

        // =========================================================================
        // EXECUÇÃO DOS COMBOS
        // =========================================================================
        public static bool ExecuteCombo(
            CombinedAttackId id,
            Unit initiator,
            Unit partner,
            List<Unit> allUnits,
            Unit target = null)
        {
            if (!CanPerformCombo(id, initiator, partner, target))
            {
                Debug.LogWarning($"[Ataque Combinado] Condições não atendidas para {id}!");
                return false;
            }

            Debug.Log($"[ATAQUE COMBINADO ATIVADO] ★★★ {Catalog[id].name} executado por {initiator.unitName} & {partner.unitName}!");

            switch (id)
            {
                case CombinedAttackId.HydroSonicShockwave:
                    // Atinge todos os inimigos em Tiles [Alagados]
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team && IsTileType(u.currentTile, TerrainType.Flooded))
                        {
                            u.TakeDamage(220, isCritical: true);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.ApplyStatus(StatusEffectType.Stun, 1);
                        }
                    }
                    break;

                case CombinedAttackId.AegisThermalBarricade:
                    // Transforma muralhas adjacentes em barricadas térmicas refletivas
                    var initChar = initiator.GetComponent<AppmonCharacter>();
                    var partChar = partner.GetComponent<AppmonCharacter>();
                    if (initChar != null) initChar.teamDamageImmunityTurns = 1;
                    if (partChar != null) partChar.teamDamageImmunityTurns = 1;
                    Debug.Log("[Aegis Thermal Barricade] Barreira de lava ativa refletindo dano!");
                    break;

                case CombinedAttackId.OverclockedStealthAmbush:
                    // Salto de 5 tiles e ataque crítico elétrico em cadeia
                    Unit hound = initiator.unitName.Contains("Glitch-Hound") ? initiator : partner;
                    int hitCount = 0;
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team && hitCount < 3)
                        {
                            u.TakeDamage(180, isCritical: true);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.ApplyStatus(StatusEffectType.Paralysis, 1);
                            hitCount++;
                        }
                    }
                    break;

                case CombinedAttackId.PolarMagneticVortex:
                    // Puxa todos os inimigos do mapa para o centro da água, dano e SPD a 0
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team)
                        {
                            u.TakeDamage(190);
                            if (u.stats != null) u.stats.SetStat(StatEnum.SPD, 0);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.ApplyStatus(StatusEffectType.Immobilized, 1);
                        }
                    }
                    break;

                case CombinedAttackId.CelestialHarmonyArray:
                    // Equipe 100% imune por 1 turno; inimigos dano massivo Água/Trovão e SPD=0
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team == initiator.team)
                        {
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.teamDamageImmunityTurns = 1;
                        }
                        else if (u != null && u.team != initiator.team)
                        {
                            u.TakeDamage(350, isCritical: true);
                            if (u.stats != null) u.stats.SetStat(StatEnum.SPD, 0);
                        }
                    }
                    break;

                case CombinedAttackId.VermilionThunderStorm:
                    // Explosões térmico-elétricas em todos inimigos, Queimadura e Paralisia por 2 turnos
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team)
                        {
                            u.TakeDamage(260);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null)
                            {
                                ch.ApplyStatus(StatusEffectType.Burn, 2);
                                ch.ApplyStatus(StatusEffectType.Paralysis, 2);
                            }
                        }
                    }
                    break;

                case CombinedAttackId.WhiteTigerFortressShred:
                    // 100% dano crítico a todas as unidades inimigas
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team)
                        {
                            u.TakeDamage(320, isCritical: true);
                        }
                    }
                    break;

                case CombinedAttackId.OmegaTsunamiOverdrive:
                    // Dano supremo, deleta buffs de status e reseta habilidades dos aliados
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team)
                        {
                            u.TakeDamage(400, isCritical: true);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.activeStatuses.Clear();
                        }
                        else if (u != null && u.team == initiator.team)
                        {
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.OnEnemyDefeated(); // Reseta cooldowns
                        }
                    }
                    break;

                case CombinedAttackId.CataclysmicPandemonium:
                    // Satanmon sacrifica 50% HP; antimatéria incinera terrenos para Fogo do Caos e reduz DEF/SPI 50%
                    Unit satan = initiator.unitName.Contains("Satanmon") ? initiator : partner;
                    int hpSatan = satan.stats != null ? satan.stats.GetStat(StatEnum.HP) : 100;
                    if (satan.stats != null) satan.stats.SetStat(StatEnum.HP, Mathf.Max(1, hpSatan / 2));

                    if (Board.instance != null && Board.instance.tiles != null)
                    {
                        foreach (var tile in Board.instance.tiles.Values)
                        {
                            if (tile != null) tile.terrainType = TerrainType.ChaosFire;
                        }
                    }

                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team && u.stats != null)
                        {
                            int def = u.stats.GetStat(StatEnum.DEF);
                            int spi = u.stats.GetStat(StatEnum.SPI);
                            u.stats.SetStat(StatEnum.DEF, def / 2);
                            u.stats.SetStat(StatEnum.SPI, spi / 2);
                            u.TakeDamage(250);
                        }
                    }
                    break;

                case CombinedAttackId.AbyssalDevourer:
                    // Dano fatal e drena 100% HP/MP divididos entre os atacantes
                    if (target != null)
                    {
                        int targetHp = target.stats != null ? target.stats.GetStat(StatEnum.HP) : 100;
                        int targetMp = target.stats != null ? target.stats.GetStat(StatEnum.MP) : 50;
                        target.TakeDamage(300, isCritical: true);

                        int hpShare = targetHp / 2;
                        int mpShare = targetMp / 2;

                        if (initiator.stats != null)
                        {
                            initiator.stats.ModifyStat(StatEnum.HP, hpShare);
                            initiator.stats.ModifyStat(StatEnum.MP, mpShare);
                        }
                        if (partner.stats != null)
                        {
                            partner.stats.ModifyStat(StatEnum.HP, hpShare);
                            partner.stats.ModifyStat(StatEnum.MP, mpShare);
                        }
                    }
                    break;

                case CombinedAttackId.GoldenSlothTrap:
                    // Petrificação de ouro e Sono Profundo por 3 turnos
                    if (target != null)
                    {
                        var ch = target.GetComponent<AppmonCharacter>();
                        if (ch != null)
                        {
                            ch.ApplyStatus(StatusEffectType.GoldPetrification, 3);
                            ch.ApplyStatus(StatusEffectType.DeepSleep, 3);
                        }
                    }
                    break;

                case CombinedAttackId.PhantasmagoricLustFire:
                    // Inimigos recebem dano de Fogo triplicado e espalham fogo
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team && IsTileType(u.currentTile, TerrainType.Fire))
                        {
                            u.TakeDamage(250 * 3, isCritical: true);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null) ch.ApplyStatus(StatusEffectType.Burn, 3);
                        }
                    }
                    break;

                case CombinedAttackId.ToxicSystemCorruption:
                    // Explode jaula, reduz carcaça (-30% DEF), Pânico e Queimadura por 3 turnos
                    if (target != null)
                    {
                        if (target.stats != null)
                        {
                            int def = target.stats.GetStat(StatEnum.DEF);
                            target.stats.SetStat(StatEnum.DEF, Mathf.Max(1, Mathf.RoundToInt(def * 0.70f)));
                        }
                        target.TakeDamage(220);
                        var ch = target.GetComponent<AppmonCharacter>();
                        if (ch != null)
                        {
                            ch.ApplyStatus(StatusEffectType.Panic, 3);
                            ch.ApplyStatus(StatusEffectType.Burn, 3);
                        }
                    }
                    break;

                case CombinedAttackId.AbsoluteZeroOverclock:
                    // Raio estático em cruz 4 tiles, paralisando e congelando inimigos por 2 turnos
                    foreach (var u in allUnits)
                    {
                        if (u != null && u.team != initiator.team)
                        {
                            u.TakeDamage(200);
                            var ch = u.GetComponent<AppmonCharacter>();
                            if (ch != null)
                            {
                                ch.ApplyStatus(StatusEffectType.Paralysis, 2);
                                ch.ApplyStatus(StatusEffectType.Frozen, 2);
                            }
                        }
                    }
                    break;
            }

            return true;
        }

        private static int GetManhattanDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static bool IsTileType(TileLogic tile, TerrainType type)
        {
            return tile != null && tile.terrainType == type;
        }

        private static bool IsAdjacentToTileType(Vector3Int pos, TerrainType type)
        {
            if (Board.instance == null || Board.instance.tiles == null) return false;
            Vector3Int[] deltas = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            foreach (var d in deltas)
            {
                if (Board.instance.tiles.TryGetValue(pos + d, out var t) && t.terrainType == type)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasAnyTileOfType(TerrainType type)
        {
            if (Board.instance == null || Board.instance.tiles == null) return false;
            foreach (var tile in Board.instance.tiles.Values)
            {
                if (tile != null && tile.terrainType == type) return true;
            }
            return false;
        }
    }
}
