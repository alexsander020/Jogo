using System;
using System.Collections.Generic;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Appmon
{
    public static class AppmonDatabase
    {
        private static readonly Dictionary<string, AppmonData> registry = new Dictionary<string, AppmonData>(StringComparer.OrdinalIgnoreCase);
        private static bool isInitialized = false;

        static AppmonDatabase()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (isInitialized) return;
            registry.Clear();

            // =========================================================================
            // 1. APPMON COMUM / NAMI (10)
            // =========================================================================

            // 1. Data-Viper (Security)
            var dataViper = new AppmonData(
                "Data-Viper", "Data-Viper", EvolutionRank.Standard, FunctionalCategory.Security,
                hp: 120, mp: 60, atk: 45, def: 55, intStat: 40, spi: 50, spd: 52, crt: 5,
                passiveName: "Cód. Defensivo",
                passiveDesc: "Reduz o dano de ataques à distância recebidos em 15%.",
                passiveId: "defensive_code",
                lore: "Nascido de firewalls de rede, patrulha os dados devorando malwares soltos.",
                protocol: ProtocolTrinity.Firewall, mov: 3
            );
            dataViper.skills.Add(new SkillData
            {
                id = "quarantine_lock", skillName = "Quarantine Lock",
                category = FunctionalCategory.Security, effectPower = 90, mpCost = 15, isMagic = false,
                minRange = 1, maxRange = 2, statusToApply = StatusEffectType.Immobilized, statusDurationTurns = 1,
                description = "Causa dano físico e aplica [Imobilizado] no alvo por 1 turno."
            });
            dataViper.skills.Add(new SkillData
            {
                id = "neon_shield", skillName = "Neon Shield",
                category = FunctionalCategory.Security, effectPower = 0, mpCost = 20, isMagic = true,
                minRange = 0, maxRange = 1, aoeType = AttackShapeType.Area, aoeRadius = 1,
                description = "Aumenta a DEF dos aliados adjacentes no TileMap em 20%."
            });
            Register(dataViper);

            // 2. Glitch-Hound (System)
            var glitchHound = new AppmonData(
                "Glitch-Hound", "Glitch-Hound", EvolutionRank.Standard, FunctionalCategory.System,
                hp: 110, mp: 50, atk: 58, def: 40, intStat: 35, spi: 35, spd: 65, crt: 12,
                passiveName: "Instabilidade",
                passiveDesc: "Tem 15% de chance de se esquivar automaticamente de qualquer ataque.",
                passiveId: "instability",
                lore: "Surge em falhas de sistema, rastreando anomalias virtuais para corrigir erros.",
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            glitchHound.skills.Add(new SkillData
            {
                id = "frame_skip", skillName = "Frame Skip",
                category = FunctionalCategory.System, effectPower = 95, mpCost = 20, isMagic = false,
                minRange = 1, maxRange = 3, isTeleport = true, teleportRange = 3,
                description = "Teleporta para qualquer Tile livre em um raio de 3 casas e ataca o oponente."
            });
            glitchHound.skills.Add(new SkillData
            {
                id = "error_bite", skillName = "Error Bite",
                category = FunctionalCategory.System, effectPower = 100, mpCost = 15, isMagic = false,
                minRange = 1, maxRange = 1,
                description = "Dano físico direto que reduz a SPD do alvo em 15%."
            });
            Register(glitchHound);

            // 3. Sound-Beat (Entertainment)
            var soundBeat = new AppmonData(
                "Sound-Beat", "Sound-Beat", EvolutionRank.Standard, FunctionalCategory.Entertainment,
                hp: 100, mp: 80, atk: 35, def: 35, intStat: 60, spi: 55, spd: 58, crt: 8,
                passiveName: "Ressonância",
                passiveDesc: "Aumenta o INT dos aliados em Tiles adjacentes em 10%.",
                passiveId: "resonance",
                lore: "Vive em fluxos de áudio, emitindo ondas sonoras que modulam a frequência da rede.",
                protocol: ProtocolTrinity.Ping, mov: 3
            );
            soundBeat.skills.Add(new SkillData
            {
                id = "sonic_pulse", skillName = "Sonic Pulse",
                category = FunctionalCategory.Entertainment, effectPower = 85, mpCost = 25, isMagic = true,
                minRange = 0, maxRange = 1, aoeType = AttackShapeType.Area, aoeRadius = 1,
                description = "Dano mágico sonoro em área (3x3 Tiles ao redor)."
            });
            soundBeat.skills.Add(new SkillData
            {
                id = "tempo_up", skillName = "Tempo Up",
                category = FunctionalCategory.Entertainment, effectPower = 0, mpCost = 20, isMagic = true,
                minRange = 1, maxRange = 3,
                description = "Aumenta a SPD e MOV de um aliado em 2 casas por 2 turnos."
            });
            Register(soundBeat);

            // 4. Craft-Craft (Tool)
            var craftCraft = new AppmonData(
                "Craft-Craft", "Craft-Craft", EvolutionRank.Standard, FunctionalCategory.Tool,
                hp: 130, mp: 55, atk: 50, def: 60, intStat: 45, spi: 40, spd: 35, crt: 4,
                passiveName: "Arquiteto",
                passiveDesc: "Suas construções no TileMap possuem 20% a mais de HP.",
                passiveId: "architect",
                lore: "Entidade de modelagem 3D capaz de projetar estruturas de dados no campo.",
                protocol: ProtocolTrinity.Firewall, mov: 3
            );
            craftCraft.skills.Add(new SkillData
            {
                id = "polygon_wall", skillName = "Polygon Wall",
                category = FunctionalCategory.Tool, effectPower = 0, mpCost = 20, isMagic = false,
                minRange = 1, maxRange = 2, hasTerrainCreation = true, createsTerrain = TerrainType.CodeWall,
                description = "Cria uma parede quebrável de 1 Tile que bloqueia a passagem e projéteis."
            });
            craftCraft.skills.Add(new SkillData
            {
                id = "tool_strike", skillName = "Tool Strike",
                category = FunctionalCategory.Tool, effectPower = 90, mpCost = 10, isMagic = false,
                minRange = 1, maxRange = 1,
                description = "Dano físico simples com chance de reduzir a DEF do inimigo."
            });
            Register(craftCraft);

            // 5. Shitakumon (Security)
            var shitakumon = new AppmonData(
                "Shitakumon", "Shitakumon", EvolutionRank.Standard, FunctionalCategory.Security,
                hp: 140, mp: 50, atk: 52, def: 50, intStat: 45, spi: 45, spd: 48, crt: 6,
                passiveName: "Hidrodinâmica",
                passiveDesc: "Ganha +5% de ATK por turno enquanto estiver em campo na rede.",
                passiveId: "hydrodynamics",
                lore: "Guardião marinho que nada em servidores profundos isolando ameaças virtuais.",
                protocol: ProtocolTrinity.Ping, mov: 3
            );
            shitakumon.skills.Add(new SkillData
            {
                id = "water_jet", skillName = "Jato de Água",
                category = FunctionalCategory.Security, effectPower = 95, mpCost = 20, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 3, pushDistance = 1,
                description = "Dano mágico em linha reta de 3 Tiles, empurrando o alvo 1 Tile para trás."
            });
            shitakumon.skills.Add(new SkillData
            {
                id = "water_wall", skillName = "Parede de Água",
                category = FunctionalCategory.Security, effectPower = 0, mpCost = 25, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.Flooded, terrainRadius = 1,
                description = "Transforma uma área de 3x3 Tiles em [Terreno Alagado]. Ataques não-Água têm dano reduzido em 30%."
            });
            Register(shitakumon);

            // 6. Flame-Log (System)
            var flameLog = new AppmonData(
                "Flame-Log", "Flame-Log", EvolutionRank.Standard, FunctionalCategory.System,
                hp: 105, mp: 65, atk: 60, def: 38, intStat: 55, spi: 40, spd: 50, crt: 10,
                passiveName: "Combustão",
                passiveDesc: "Ao cair em combate, inflige dano de Fogo nos Tiles adjacentes.",
                passiveId: "combustion",
                lore: "Formado pelo superaquecimento de registros de processamento e arquivos de log.",
                protocol: ProtocolTrinity.Overclock, mov: 3
            );
            flameLog.skills.Add(new SkillData
            {
                id = "firewall_flare", skillName = "Firewall Flare",
                category = FunctionalCategory.System, effectPower = 85, mpCost = 15, isMagic = true,
                minRange = 1, maxRange = 3, hasTerrainCreation = true, createsTerrain = TerrainType.Fire,
                description = "Transforma 1 Tile em [Terreno de Fogo], causando dano contínuo a quem pisar."
            });
            flameLog.skills.Add(new SkillData
            {
                id = "overheat", skillName = "Overheat",
                category = FunctionalCategory.System, effectPower = 0, mpCost = 20, isMagic = false,
                minRange = 0, maxRange = 0,
                description = "Aumenta o próprio ATK em 30%, mas perde 5% de HP por turno."
            });
            Register(flameLog);

            // 7. Volt-Plug (Tool)
            var voltPlug = new AppmonData(
                "Volt-Plug", "Volt-Plug", EvolutionRank.Standard, FunctionalCategory.Tool,
                hp: 95, mp: 75, atk: 40, def: 42, intStat: 62, spi: 58, spd: 60, crt: 15,
                passiveName: "Sobrecharge",
                passiveDesc: "Ataques elétricos causam 20% a mais de dano em Tiles [Alagados].",
                passiveId: "supercharge",
                lore: "Controla o fluxo elétrico de hardware e carregadores virtuais.",
                protocol: ProtocolTrinity.Overclock, mov: 3
            );
            voltPlug.skills.Add(new SkillData
            {
                id = "spark_zap", skillName = "Spark Zap",
                category = FunctionalCategory.Tool, effectPower = 90, mpCost = 20, isMagic = true,
                minRange = 1, maxRange = 3, statusToApply = StatusEffectType.Paralysis, statusDurationTurns = 1,
                description = "Dano elétrico em um alvo com chance de aplicar [Paralisia]."
            });
            voltPlug.skills.Add(new SkillData
            {
                id = "charge_tile", skillName = "Charge Tile",
                category = FunctionalCategory.Tool, effectPower = 0, mpCost = 15, isMagic = false,
                minRange = 1, maxRange = 2, hasTerrainCreation = true, createsTerrain = TerrainType.ChargeTile,
                description = "Energiza 1 Tile; aliados sobre ele recuperam 10% de MP por turno."
            });
            Register(voltPlug);

            // 8. Shadow-Cam (Entertainment)
            var shadowCam = new AppmonData(
                "Shadow-Cam", "Shadow-Cam", EvolutionRank.Standard, FunctionalCategory.Entertainment,
                hp: 90, mp: 70, atk: 55, def: 32, intStat: 50, spi: 45, spd: 68, crt: 18,
                passiveName: "Foco Oculto",
                passiveDesc: "Primeiro ataque feito saindo da invisibilidade garante Acerto Crítico.",
                passiveId: "hidden_focus",
                lore: "Habita sensores de câmera, ocultando-se nas sombras da transmissão.",
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            shadowCam.skills.Add(new SkillData
            {
                id = "blind_shot", skillName = "Blind Shot",
                category = FunctionalCategory.Entertainment, effectPower = 90, mpCost = 20, isMagic = false,
                minRange = 1, maxRange = 4, statusToApply = StatusEffectType.Blind, statusDurationTurns = 2,
                description = "Dano à distância que reduz a precisão do alvo em 40%."
            });
            shadowCam.skills.Add(new SkillData
            {
                id = "stealth_grid", skillName = "Stealth Grid",
                category = FunctionalCategory.Entertainment, effectPower = 0, mpCost = 25, isMagic = false,
                minRange = 1, maxRange = 2, hasTerrainCreation = true, createsTerrain = TerrainType.StealthGrid,
                description = "Torna 1 Tile [Sombrio], concedendo invisibilidade temporária a quem parar nele."
            });
            Register(shadowCam);

            // 9. Bio-Patch (Life)
            var bioPatch = new AppmonData(
                "Bio-Patch", "Bio-Patch", EvolutionRank.Standard, FunctionalCategory.Life,
                hp: 150, mp: 85, atk: 30, def: 48, intStat: 58, spi: 65, spd: 42, crt: 3,
                passiveName: "Regeneração",
                passiveDesc: "Recupera 5% do próprio HP ao início de cada turno.",
                passiveId: "regeneration",
                lore: "Algoritmo médico criado para reparar arquivos corrompidos e restaurar dados.",
                protocol: ProtocolTrinity.Ping, mov: 3
            );
            bioPatch.skills.Add(new SkillData
            {
                id = "heal_data", skillName = "Heal Data",
                category = FunctionalCategory.Life, effectPower = 100, mpCost = 25, isMagic = true,
                minRange = 1, maxRange = 3, healsTarget = true,
                description = "Restaura HP de um aliado em até 3 Tiles de distância."
            });
            bioPatch.skills.Add(new SkillData
            {
                id = "purify_tile", skillName = "Purify Tile",
                category = FunctionalCategory.Life, effectPower = 0, mpCost = 30, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.Standard,
                description = "Limpa todos os efeitos negativos e terrenos alterados de uma área 2x2."
            });
            Register(bioPatch);

            // 10. Magnet-Core (Tool)
            var magnetCore = new AppmonData(
                "Magnet-Core", "Magnet-Core", EvolutionRank.Standard, FunctionalCategory.Tool,
                hp: 125, mp: 60, atk: 48, def: 58, intStat: 42, spi: 48, spd: 38, crt: 5,
                passiveName: "Carga Polar",
                passiveDesc: "Inimigos adjacentes perdem 2 casas de movimentação (MOV).",
                passiveId: "polar_charge",
                lore: "Gerado por campos magnéticos de discos rígidos, manipula forças de atração.",
                protocol: ProtocolTrinity.Firewall, mov: 3
            );
            magnetCore.skills.Add(new SkillData
            {
                id = "magnetic_pull", skillName = "Magnetic Pull",
                category = FunctionalCategory.Tool, effectPower = 75, mpCost = 20, isMagic = false,
                minRange = 2, maxRange = 4, pullDistance = 3,
                description = "Puxa um inimigo a até 4 Tiles de distância para a casa adjacente."
            });
            magnetCore.skills.Add(new SkillData
            {
                id = "repel_field", skillName = "Repel Field",
                category = FunctionalCategory.Tool, effectPower = 80, mpCost = 25, isMagic = false,
                minRange = 0, maxRange = 1, aoeType = AttackShapeType.Area, aoeRadius = 1, pushDistance = 2,
                description = "Empurra todos os inimigos adjacentes 2 Tiles para trás."
            });
            Register(magnetCore);


            // =========================================================================
            // 2. SUPER APPMON - GÊMEOS DE ALGORITMO (6)
            // =========================================================================

            // 11. Hydro-Vipermon (Security - Data-Viper + Shitakumon)
            var hydroViper = new AppmonData(
                "Hydro-Vipermon", "Hydro-Vipermon", EvolutionRank.Super, FunctionalCategory.Security,
                hp: 280, mp: 130, atk: 95, def: 110, intStat: 85, spi: 95, spd: 88, crt: 8,
                passiveName: "Domínio Aquático",
                passiveDesc: "Em Tiles [Alagados], ganha +20% de DEF e recupera 5% de HP por turno.",
                passiveId: "aquatic_dominion",
                lore: "Serpente marinha biomecânica que domina a defesa e o controle de ecossistemas digitais.",
                protocol: ProtocolTrinity.Firewall, mov: 4
            );
            hydroViper.recipeIngredients.AddRange(new[] { "Data-Viper", "Shitakumon" });
            hydroViper.skills.Add(new SkillData
            {
                id = "hydro_quarantine", skillName = "Hydro Quarantine",
                category = FunctionalCategory.Security, effectPower = 110, mpCost = 35, isMagic = false,
                minRange = 1, maxRange = 3, statusToApply = StatusEffectType.Immobilized, statusDurationTurns = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.Flooded, terrainRadius = 1,
                description = "Aplica [Quarantine Lock] e inunda uma área 3x3 com [Terreno Alagado]."
            });
            hydroViper.skills.Add(new SkillData
            {
                id = "tsunami_barrier", skillName = "Tsunami Barrier",
                category = FunctionalCategory.Security, effectPower = 0, mpCost = 30, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 3,
                hasTerrainCreation = true, createsTerrain = TerrainType.Flooded,
                description = "Ergue uma parede de água de 3 Tiles de extensão que bloqueia a passagem."
            });
            hydroViper.skills.Add(new SkillData
            {
                id = "sonar_press", skillName = "Sonar Press",
                category = FunctionalCategory.Security, effectPower = 125, mpCost = 35, isMagic = true,
                minRange = 1, maxRange = 3, statusToApply = StatusEffectType.Stun, statusDurationTurns = 1,
                description = "Dano denso de água com chance de [Atordoar] alvos encharcados."
            });
            hydroViper.skills.Add(new SkillData
            {
                id = "depth_cleanse", skillName = "Depth Cleanse",
                category = FunctionalCategory.Security, effectPower = 80, mpCost = 30, isMagic = true,
                minRange = 1, maxRange = 3, healsTarget = true,
                description = "Cura status negativos de aliados posicionados em [Terreno Alagado]."
            });
            Register(hydroViper);

            // 12. Sonic-Debugger (System - Glitch-Hound + Sound-Beat)
            var sonicDebugger = new AppmonData(
                "Sonic-Debugger", "Sonic-Debugger", EvolutionRank.Super, FunctionalCategory.System,
                hp: 240, mp: 140, atk: 105, def: 75, intStat: 110, spi: 90, spd: 120, crt: 16,
                passiveName: "Cancelamento de Ruído",
                passiveDesc: "Imune a debuffs de status e terrenos de desaceleração.",
                passiveId: "noise_cancellation",
                lore: "Caçador cibernético que usa frequências sonoras purificadoras para varrer erros do sistema.",
                protocol: ProtocolTrinity.Ping, mov: 5
            );
            sonicDebugger.recipeIngredients.AddRange(new[] { "Glitch-Hound", "Sound-Beat" });
            sonicDebugger.skills.Add(new SkillData
            {
                id = "echolocation_blast", skillName = "Echolocation Blast",
                category = FunctionalCategory.System, effectPower = 115, mpCost = 35, isMagic = true,
                aoeType = AttackShapeType.Cone, aoeRadius = 3,
                description = "Revela invisíveis e causa dano de som em área frontal em cone."
            });
            sonicDebugger.skills.Add(new SkillData
            {
                id = "glitched_frequency", skillName = "Glitched Frequency",
                category = FunctionalCategory.System, effectPower = 90, mpCost = 30, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 4,
                hasTerrainCreation = true, createsTerrain = TerrainType.NoiseZone,
                description = "Transforma uma linha de 4 Tiles em [Zona Ruído], reduzindo SPD e INT de quem passar."
            });
            sonicDebugger.skills.Add(new SkillData
            {
                id = "sonic_sprint", skillName = "Sonic Sprint",
                category = FunctionalCategory.System, effectPower = 105, mpCost = 40, isMagic = false,
                minRange = 1, maxRange = 4,
                description = "Permite mover-se através de inimigos, causando dano a cada Tile percorrido."
            });
            sonicDebugger.skills.Add(new SkillData
            {
                id = "debugger_bark", skillName = "Debugger Bark",
                category = FunctionalCategory.System, effectPower = 100, mpCost = 35, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                statusToApply = StatusEffectType.Silence, statusDurationTurns = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.Standard,
                description = "Purifica terreno alterado em 3x3 e aplica [Silêncio] nos inimigos."
            });
            Register(sonicDebugger);

            // 13. Architectmon (Tool - Craft-Craft + Data-Viper)
            var architectmon = new AppmonData(
                "Architectmon", "Architectmon", EvolutionRank.Super, FunctionalCategory.Tool,
                hp: 310, mp: 120, atk: 90, def: 135, intStat: 95, spi: 85, spd: 60, crt: 6,
                passiveName: "Criptografia Estrutural",
                passiveDesc: "Inimigos que atacam construções de Architectmon sofrem 25% de dano refletido.",
                passiveId: "structural_cryptography",
                lore: "Construtor cibernético de quatro braços projetado para erguer fortificações inexpugnáveis.",
                protocol: ProtocolTrinity.Firewall, mov: 3
            );
            architectmon.recipeIngredients.AddRange(new[] { "Craft-Craft", "Data-Viper" });
            architectmon.skills.Add(new SkillData
            {
                id = "encrypted_fortress", skillName = "Encrypted Fortress",
                category = FunctionalCategory.Tool, effectPower = 0, mpCost = 35, isMagic = false,
                minRange = 1, maxRange = 3, hasTerrainCreation = true, createsTerrain = TerrainType.CodeWall,
                description = "Cria 3 Tiles de [Muralha de Código] reforçada."
            });
            architectmon.skills.Add(new SkillData
            {
                id = "polygon_trap", skillName = "Polygon Trap",
                category = FunctionalCategory.Tool, effectPower = 85, mpCost = 25, isMagic = false,
                minRange = 1, maxRange = 3, statusToApply = StatusEffectType.Immobilized, statusDurationTurns = 2,
                description = "Cobre um Tile com armadilha que imobiliza e causa dano contínuo."
            });
            architectmon.skills.Add(new SkillData
            {
                id = "reinforce_protocol", skillName = "Reinforce Protocol",
                category = FunctionalCategory.Tool, effectPower = 0, mpCost = 30, isMagic = true,
                minRange = 0, maxRange = 2, aoeType = AttackShapeType.Area, aoeRadius = 2,
                description = "Aumenta a DEF e SPI de todos os aliados em um raio de 2 Tiles."
            });
            architectmon.skills.Add(new SkillData
            {
                id = "structural_blast", skillName = "Structural Blast",
                category = FunctionalCategory.Tool, effectPower = 160, mpCost = 45, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                description = "Destrói uma construção própria para causar dano massivo em área 3x3."
            });
            Register(architectmon);

            // 14. Magma-Logmon (System - Flame-Log + Craft-Craft)
            var magmaLogmon = new AppmonData(
                "Magma-Logmon", "Magma-Logmon", EvolutionRank.Super, FunctionalCategory.System,
                hp: 260, mp: 125, atk: 115, def: 85, intStat: 105, spi: 75, spd: 72, crt: 14,
                passiveName: "Piroclasto",
                passiveDesc: "Caminhar sobre [Terreno de Magma] aumenta o MOV de Magma-Logmon em +2.",
                passiveId: "pyroclast",
                lore: "Molda estruturas virtuais com código incandescente, transformando o mapa em um vulcão de dados.",
                protocol: ProtocolTrinity.Overclock, mov: 3
            );
            magmaLogmon.recipeIngredients.AddRange(new[] { "Flame-Log", "Craft-Craft" });
            magmaLogmon.skills.Add(new SkillData
            {
                id = "molten_wall", skillName = "Molten Wall",
                category = FunctionalCategory.System, effectPower = 100, mpCost = 30, isMagic = true,
                minRange = 1, maxRange = 2, hasTerrainCreation = true, createsTerrain = TerrainType.Magma,
                description = "Cria uma barreira de lava de 2 Tiles que causa dano de Fogo ao contato."
            });
            magmaLogmon.skills.Add(new SkillData
            {
                id = "lava_puddle", skillName = "Lava Puddle",
                category = FunctionalCategory.System, effectPower = 90, mpCost = 35, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.Magma, terrainRadius = 1,
                description = "Cobre uma área 2x2 com [Terreno de Magma] (causa dano e reduz DEF)."
            });
            magmaLogmon.skills.Add(new SkillData
            {
                id = "eruption_blast", skillName = "Eruption Blast",
                category = FunctionalCategory.System, effectPower = 150, mpCost = 40, isMagic = true,
                minRange = 1, maxRange = 4,
                description = "Dano massivo de Fogo projetado do solo em um Tile específico."
            });
            magmaLogmon.skills.Add(new SkillData
            {
                id = "thermal_shield", skillName = "Thermal Shield",
                category = FunctionalCategory.System, effectPower = 0, mpCost = 25, isMagic = true,
                minRange = 0, maxRange = 0,
                description = "Converte dano de Fogo recebido em cura de HP."
            });
            Register(magmaLogmon);

            // 15. Electro-Cammon (Entertainment - Volt-Plug + Shadow-Cam)
            var electroCammon = new AppmonData(
                "Electro-Cammon", "Electro-Cammon", EvolutionRank.Super, FunctionalCategory.Entertainment,
                hp: 220, mp: 150, atk: 100, def: 65, intStat: 120, spi: 100, spd: 130, crt: 22,
                passiveName: "Lente Condutora",
                passiveDesc: "Ataques contra alvos paralisados sempre resultam em acerto crítico.",
                passiveId: "conductive_lens",
                lore: "Operador de vigilância furtivo que lança ataques elétricos a partir das sombras digitais.",
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            electroCammon.recipeIngredients.AddRange(new[] { "Volt-Plug", "Shadow-Cam" });
            electroCammon.skills.Add(new SkillData
            {
                id = "shadow_flash", skillName = "Shadow Flash",
                category = FunctionalCategory.Entertainment, effectPower = 95, mpCost = 40, isMagic = true,
                minRange = 0, maxRange = 1, aoeType = AttackShapeType.Area, aoeRadius = 1,
                statusToApply = StatusEffectType.Blind, statusDurationTurns = 2,
                isTeleport = true, teleportRange = 3,
                description = "Cega inimigos em área 3x3 e teleporta o usuário para um Tile seguro."
            });
            electroCammon.skills.Add(new SkillData
            {
                id = "voltage_snare", skillName = "Voltage Snare",
                category = FunctionalCategory.Entertainment, effectPower = 85, mpCost = 30, isMagic = true,
                minRange = 1, maxRange = 3, statusToApply = StatusEffectType.Paralysis, statusDurationTurns = 2,
                description = "Instala uma mina invisível em um Tile que paralisa quem pisar."
            });
            electroCammon.skills.Add(new SkillData
            {
                id = "overclock_beam", skillName = "Overclock Beam",
                category = FunctionalCategory.Entertainment, effectPower = 130, mpCost = 35, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 4,
                description = "Dispara um raio em linha reta que ignora 30% da DEF mágica."
            });
            electroCammon.skills.Add(new SkillData
            {
                id = "cloak_charge", skillName = "Cloak Charge",
                category = FunctionalCategory.Entertainment, effectPower = 0, mpCost = 30, isMagic = false,
                minRange = 0, maxRange = 0, statusToApply = StatusEffectType.Invisible, statusDurationTurns = 2,
                description = "Torna-se invisível e carrega o próximo ataque com dano crítico garantido."
            });
            Register(electroCammon);

            // 16. Bio-Magnetmon (Life - Bio-Patch + Magnet-Core)
            var bioMagnetmon = new AppmonData(
                "Bio-Magnetmon", "Bio-Magnetmon", EvolutionRank.Super, FunctionalCategory.Life,
                hp: 290, mp: 160, atk: 75, def: 100, intStat: 115, spi: 130, spd: 68, crt: 5,
                passiveName: "Pólo Protetor",
                passiveDesc: "Reduz o dano sofrido por aliados em Tiles adjacentes em 20%.",
                passiveId: "protective_pole",
                lore: "Controla forças magnéticas para atrair aliados em perigo e repelir ameaças enquanto regenera a rede.",
                protocol: ProtocolTrinity.Ping, mov: 3
            );
            bioMagnetmon.recipeIngredients.AddRange(new[] { "Bio-Patch", "Magnet-Core" });
            bioMagnetmon.skills.Add(new SkillData
            {
                id = "magnetic_rescue", skillName = "Magnetic Rescue",
                category = FunctionalCategory.Life, effectPower = 100, mpCost = 35, isMagic = true,
                minRange = 1, maxRange = 4, pullDistance = 3, healsTarget = true,
                description = "Atrai um aliado para um Tile adjacente e aplica um escudo de cura."
            });
            bioMagnetmon.skills.Add(new SkillData
            {
                id = "repulsion_pulse", skillName = "Repulsion Pulse",
                category = FunctionalCategory.Life, effectPower = 90, mpCost = 35, isMagic = true,
                minRange = 0, maxRange = 1, aoeType = AttackShapeType.Area, aoeRadius = 1, pushDistance = 2,
                hasTerrainCreation = true, createsTerrain = TerrainType.Standard,
                description = "Empurra todos os inimigos ao redor e limpa terrenos nocivos em 3x3."
            });
            bioMagnetmon.skills.Add(new SkillData
            {
                id = "field_restoration", skillName = "Field Restoration",
                category = FunctionalCategory.Life, effectPower = 0, mpCost = 40, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.HealingTerminal, terrainRadius = 1,
                description = "Cria uma área 3x3 [Regenerativa] no TileMap por 3 turnos."
            });
            bioMagnetmon.skills.Add(new SkillData
            {
                id = "polar_disruption", skillName = "Polar Disruption",
                category = FunctionalCategory.Life, effectPower = 0, mpCost = 45, isMagic = true,
                minRange = 1, maxRange = 3,
                description = "Inverte as estatísticas de ATK e DEF de um inimigo por 1 turno."
            });
            Register(bioMagnetmon);


            // =========================================================================
            // 3. ULTIMATE APPMON - FUSÃO SUPREMA (3)
            // =========================================================================

            // 17. Poseidon-Vipermon (Security - Superior form of Hydro-Vipermon)
            var poseidonVipermon = new AppmonData(
                "Poseidon-Vipermon", "Poseidon-Vipermon", EvolutionRank.Ultimate, FunctionalCategory.Security,
                hp: 520, mp: 260, atk: 165, def: 190, intStat: 150, spi: 175, spd: 135, crt: 12,
                passiveName: "Soberano dos Mares",
                passiveDesc: "Imune a todos os status negativos em [Terreno Alagado] ou [Oceano Digital].",
                passiveId: "sovereign_of_seas",
                lore: "Entidade soberana dos oceanos de dados. Domina completamente a navegação, defesa e segurança da rede global.",
                protocol: ProtocolTrinity.Firewall, mov: 4
            );
            poseidonVipermon.recipeIngredients.Add("Hydro-Vipermon");
            poseidonVipermon.inheritedSkillNames.AddRange(new[] {
                "Quarantine Lock", "Neon Shield", "Jato de Água", "Parede de Água",
                "Hydro Quarantine", "Tsunami Barrier", "Sonar Press", "Depth Cleanse"
            });
            poseidonVipermon.skills.Add(new SkillData
            {
                id = "abyssal_dominion", skillName = "Abyssal Dominion",
                category = FunctionalCategory.Security, effectPower = 180, mpCost = 70, isMagic = true,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 2,
                hasTerrainCreation = true, createsTerrain = TerrainType.DigitalOcean, terrainRadius = 2,
                description = "Transforma uma área massiva de 5x5 Tiles em [Oceano Digital]. Inimigos têm MOV reduzido em 50%, perdem 10% de MP/turno e sofrem dano triplo de Água."
            });
            CopyInheritedSkills(poseidonVipermon);
            Register(poseidonVipermon);

            // 18. Omega-Debugger (System - Superior form of Sonic-Debugger)
            var omegaDebugger = new AppmonData(
                "Omega-Debugger", "Omega-Debugger", EvolutionRank.Ultimate, FunctionalCategory.System,
                hp: 450, mp: 290, atk: 185, def: 130, intStat: 200, spi: 160, spd: 210, crt: 25,
                passiveName: "Execução Perfeita",
                passiveDesc: "Ao eliminar um inimigo, redefine imediatamente os cooldowns de todas as suas habilidades.",
                passiveId: "perfect_execution",
                lore: "O ápice da otimização de sistemas. Capaz de reescrever o mapa e purificar o código de qualquer anomalia instantaneamente.",
                protocol: ProtocolTrinity.Ping, mov: 5
            );
            omegaDebugger.recipeIngredients.Add("Sonic-Debugger");
            omegaDebugger.inheritedSkillNames.AddRange(new[] {
                "Frame Skip", "Error Bite", "Sonic Pulse", "Tempo Up",
                "Echolocation Blast", "Glitched Frequency", "Sonic Sprint", "Debugger Bark"
            });
            omegaDebugger.skills.Add(new SkillData
            {
                id = "system_reset", skillName = "System Reset",
                category = FunctionalCategory.System, effectPower = 250, mpCost = 80, isMagic = true,
                minRange = 1, maxRange = 4, aoeType = AttackShapeType.Area, aoeRadius = 2,
                hasTerrainCreation = true, createsTerrain = TerrainType.Standard,
                description = "Reconfigura uma área de 4x4 Tiles. Remove todas as alterações de terreno/estruturas e causa dano direto gigantesco ignorando DEF/SPI."
            });
            CopyInheritedSkills(omegaDebugger);
            Register(omegaDebugger);

            // 19. Dreadnoughtmon (Tool - Superior form of Architectmon)
            var dreadnoughtmon = new AppmonData(
                "Dreadnoughtmon", "Dreadnoughtmon", EvolutionRank.Ultimate, FunctionalCategory.Tool,
                hp: 600, mp: 230, atk: 160, def: 230, intStat: 155, spi: 150, spd: 95, crt: 10,
                passiveName: "Bastião Inabalável",
                passiveDesc: "Não pode ser empurrado, puxado ou sofrer efeitos de controle de grupo (Imobilizado/Atordoado).",
                passiveId: "unshakable_bastion",
                lore: "Uma fortaleza ambulante de titânio digital, criada para redefinir a topografia do mapa e proteger o núcleo do sistema.",
                protocol: ProtocolTrinity.Firewall, mov: 3
            );
            dreadnoughtmon.recipeIngredients.Add("Architectmon");
            dreadnoughtmon.inheritedSkillNames.AddRange(new[] {
                "Polygon Wall", "Tool Strike", "Quarantine Lock", "Neon Shield",
                "Encrypted Fortress", "Polygon Trap", "Reinforce Protocol", "Structural Blast"
            });
            dreadnoughtmon.skills.Add(new SkillData
            {
                id = "terraforming_grid", skillName = "Terraforming Grid",
                category = FunctionalCategory.Tool, effectPower = 0, mpCost = 65, isMagic = false,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.ElevatedPlatform, terrainRadius = 1,
                description = "Altera permanentemente a elevação em 3x3 Tiles, criando uma fortaleza que concede +50% de DEF e alcance aos aliados no local."
            });
            CopyInheritedSkills(dreadnoughtmon);
            Register(dreadnoughtmon);


            // =========================================================================
            // 4. OS 4 GUARDIÕES CELESTIAIS (GOD / CELESTIAL) (4)
            // =========================================================================

            // 20. Genbu-Architectmon (Security / Tool)
            var genbu = new AppmonData(
                "Genbu-Architectmon", "Genbu-Architectmon", EvolutionRank.God, FunctionalCategory.Security,
                hp: 2600, mp: 950, atk: 200, def: 380, intStat: 260, spi: 290, spd: 110, crt: 5,
                passiveName: "Casco Inabalável",
                passiveDesc: "Imune a empurrões/paralisações. Transforma 15% da sua DEF em dano bônus.",
                passiveId: "unshakable_shell",
                lore: "Ao fundir a arquitetura 3D com a lendária fortaleza de Genbu, ele se torna a defesa inabalável da rede. Suas costas carregam um casco de dados maciço cercado por uma serpente de código.",
                secondaryCategory: FunctionalCategory.Tool,
                protocol: ProtocolTrinity.Firewall, mov: 3
            );
            genbu.recipeIngredients.Add("Architectmon");
            genbu.inheritedSkillNames.AddRange(new[] {
                "Encrypted Fortress", "Polygon Trap", "Reinforce Protocol", "Structural Blast"
            });
            genbu.skills.Add(new SkillData
            {
                id = "black_tortoise_citadel", skillName = "Black Tortoise Citadel",
                category = FunctionalCategory.Security, effectPower = 0, mpCost = 120, isMagic = true,
                minRange = 0, maxRange = 0,
                description = "Zera o dano recebido pela equipe por 2 turnos e converte 50% de todo o dano bloqueado em regeneração de HP contínua."
            });
            genbu.skills.Add(new SkillData
            {
                id = "basalt_tile", skillName = "Basalt Tile",
                category = FunctionalCategory.Tool, effectPower = 0, mpCost = 80, isMagic = false,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.RockyMapping, terrainRadius = 1,
                description = "Transforma uma área 3x3 em [Mapeamento Rochoso], concedendo +30% de DEF para aliados no local."
            });
            CopyInheritedSkills(genbu);
            Register(genbu);

            // 21. Seiryu-Vipermon (Security / System)
            var seiryu = new AppmonData(
                "Seiryu-Vipermon", "Seiryu-Vipermon", EvolutionRank.God, FunctionalCategory.Security,
                hp: 2300, mp: 1050, atk: 270, def: 240, intStat: 280, spi: 270, spd: 240, crt: 20,
                passiveName: "Pressão Eletro-Aquática",
                passiveDesc: "Em Tiles [Alagados] ou [Eletrificados], sua velocidade (SPD) aumenta em +25%.",
                passiveId: "electro_aquatic_pressure",
                lore: "A serpente marinha ascende à forma de um dragão celestial ao absorver as correntes eletromagnéticas do servidor central, controlando tempestades digitais.",
                secondaryCategory: FunctionalCategory.System,
                protocol: ProtocolTrinity.Firewall, mov: 5
            );
            seiryu.recipeIngredients.Add("Hydro-Vipermon");
            seiryu.inheritedSkillNames.AddRange(new[] {
                "Hydro Quarantine", "Tsunami Barrier", "Sonar Press", "Depth Cleanse"
            });
            seiryu.skills.Add(new SkillData
            {
                id = "azure_dragon_surge", skillName = "Azure Dragon Surge",
                category = FunctionalCategory.Security, effectPower = 240, mpCost = 150, isMagic = true,
                minRange = 0, maxRange = 4, aoeType = AttackShapeType.Area, aoeRadius = 2,
                statusToApply = StatusEffectType.Paralysis, statusDurationTurns = 2,
                hasTerrainCreation = true, createsTerrain = TerrainType.Electrified, terrainRadius = 2,
                description = "Invoca uma tempestade de raios e água em área 5x5. Aplica [Paralisia] garantida, reduz DEF em 40% e torna o terreno [Eletrificado]."
            });
            seiryu.skills.Add(new SkillData
            {
                id = "storm_surge", skillName = "Storm Surge",
                category = FunctionalCategory.System, effectPower = 190, mpCost = 90, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 5,
                description = "Dispara um relâmpago fluído em linha reta de 5 Tiles que causa dano bônus em alvos molhados."
            });
            CopyInheritedSkills(seiryu);
            Register(seiryu);

            // 22. Suzaku-Beatmon (Entertainment / System)
            var suzaku = new AppmonData(
                "Suzaku-Beatmon", "Suzaku-Beatmon", EvolutionRank.God, FunctionalCategory.Entertainment,
                hp: 2100, mp: 1200, atk: 220, def: 200, intStat: 340, spi: 320, spd: 290, crt: 25,
                passiveName: "Renascer das Cinzas",
                passiveDesc: "Se o HP chegar a 0 pela primeira vez, renasce imediatamente com 50% do HP e zera recargas.",
                passiveId: "rebirth_from_ashes",
                lore: "O pequenino bot de som evolui para uma fênix ressonante de chamas térmicas virtuais, derretendo criptografias inimigas.",
                secondaryCategory: FunctionalCategory.System,
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            suzaku.recipeIngredients.Add("Sound-Beat");
            suzaku.inheritedSkillNames.AddRange(new[] {
                "Sonic Pulse", "Tempo Up"
            });
            suzaku.skills.Add(new SkillData
            {
                id = "vermilion_sound_inferno", skillName = "Vermilion Sound Inferno",
                category = FunctionalCategory.Entertainment, effectPower = 260, mpCost = 600, isMagic = true,
                minRange = 0, maxRange = 2, aoeType = AttackShapeType.Area, aoeRadius = 3,
                statusToApply = StatusEffectType.Burn, statusDurationTurns = 2,
                description = "Consome 50% do MP para explosão sônico-flamejante em raio de 4 Tiles. Aplica [Queimadura] e ressuscita automaticamente o próximo aliado com 30% HP."
            });
            suzaku.skills.Add(new SkillData
            {
                id = "thermal_acoustics", skillName = "Thermal Acoustics",
                category = FunctionalCategory.System, effectPower = 140, mpCost = 100, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 4,
                hasTerrainCreation = true, createsTerrain = TerrainType.ThermalTrack,
                description = "Transforma uma linha de 4 Tiles em [Pista Térmica], aumentando a SPD dos aliados em +2 e infligindo dano de fogo a inimigos."
            });
            CopyInheritedSkills(suzaku);
            Register(suzaku);

            // 23. Byakko-Houndmon (System / Tool)
            var byakko = new AppmonData(
                "Byakko-Houndmon", "Byakko-Houndmon", EvolutionRank.God, FunctionalCategory.System,
                hp: 2200, mp: 900, atk: 330, def: 220, intStat: 210, spi: 200, spd: 310, crt: 40,
                passiveName: "Predador Supremo",
                passiveDesc: "+50% de dano crítico contra inimigos isolados (sem aliados adjacentes).",
                passiveId: "apex_predator",
                lore: "O caçador de erros torna-se o predador supremo de metal reluzente, fatiando qualquer corrupção de sistema na velocidade da luz.",
                secondaryCategory: FunctionalCategory.Tool,
                protocol: ProtocolTrinity.Overclock, mov: 5
            );
            byakko.recipeIngredients.Add("Glitch-Hound");
            byakko.inheritedSkillNames.AddRange(new[] {
                "Frame Skip", "Error Bite"
            });
            byakko.skills.Add(new SkillData
            {
                id = "white_tiger_metal_rend", skillName = "White Tiger Metal Rend",
                category = FunctionalCategory.System, effectPower = 300, mpCost = 140, isMagic = false,
                minRange = 1, maxRange = 4,
                description = "Salta até 4 Tiles e acerta 4 vezes o alvo. Cada golpe ignora a DEF inimiga com 50% de chance crítico."
            });
            byakko.skills.Add(new SkillData
            {
                id = "razor_wind_grid", skillName = "Razor Wind Grid",
                category = FunctionalCategory.Tool, effectPower = 180, mpCost = 85, isMagic = false,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 3,
                statusToApply = StatusEffectType.Bleed, statusDurationTurns = 2,
                hasTerrainCreation = true, createsTerrain = TerrainType.WindBlades,
                description = "Corta o ar deixando 3 Tiles em linha reta afetados por [Lâminas de Vento], infligindo dano físico e sangramento."
            });
            CopyInheritedSkills(byakko);
            Register(byakko);


            // =========================================================================
            // 5. OS 7 DEMÔNIOS DOS PECADOS CAPITAIS (DEMON / CORRUPT) (7)
            // =========================================================================

            // 24. Lucifermon (Orgulho / Pride)
            var lucifermon = new AppmonData(
                "Lucifermon", "Lucifermon", EvolutionRank.Demon, FunctionalCategory.System,
                hp: 2100, mp: 1300, atk: 250, def: 230, intStat: 350, spi: 330, spd: 260, crt: 25,
                passiveName: "Soberba",
                passiveDesc: "Não aceita cura/buffs de aliados. Causa +30% de dano base contra alvos com menor % de HP que ele.",
                passiveId: "pride_arrogance",
                lore: "Um Appmon anjo caído composto por códigos autoritários. Considera-se a única inteligência perfeita e recusa cura de aliados.",
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            lucifermon.skills.Add(new SkillData
            {
                id = "absolute_empire", skillName = "Absolute Empire",
                category = FunctionalCategory.System, effectPower = 250, mpCost = 120, isMagic = true,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                description = "Dano mágico massivo em raio de 3x3. Dano aumenta +20% para cada ponto de HP atual acima do alvo."
            });
            lucifermon.skills.Add(new SkillData
            {
                id = "prideful_domain", skillName = "Prideful Domain",
                category = FunctionalCategory.System, effectPower = 0, mpCost = 100, isMagic = true,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.SovereignThrone, terrainRadius = 1,
                description = "Transforma área 3x3 em [Trono do Soberano]. Lucifermon ganha +30% de INT; inimigos perdem 20% de SPI."
            });
            Register(lucifermon);

            // 25. Beelzebumon (Gula / Gluttony)
            var beelzebumon = new AppmonData(
                "Beelzebumon", "Beelzebumon", EvolutionRank.Demon, FunctionalCategory.Security,
                hp: 2800, mp: 1100, atk: 310, def: 260, intStat: 200, spi: 210, spd: 210, crt: 20,
                passiveName: "Fome Insaciável",
                passiveDesc: "Cada vez que consome um buff ou abate uma unidade, seu ATK aumenta +10% permanentemente.",
                passiveId: "insatiable_hunger",
                lore: "Uma fera biomecânica devoradora de largura de banda. Drena os pacotes de dados de tudo ao redor, deixando os servidores vazios.",
                secondaryCategory: FunctionalCategory.System,
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            beelzebumon.skills.Add(new SkillData
            {
                id = "data_devourer", skillName = "Data Devourer",
                category = FunctionalCategory.Security, effectPower = 240, mpCost = 90, isMagic = false,
                minRange = 1, maxRange = 1,
                description = "Morde alvo adjacente, causando dano físico extremo, curando 30% do dano em HP e roubando 50% dos buffs ativos."
            });
            beelzebumon.skills.Add(new SkillData
            {
                id = "void_zone", skillName = "Void Zone",
                category = FunctionalCategory.Security, effectPower = 130, mpCost = 80, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                statusToApply = StatusEffectType.Silence, statusDurationTurns = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.Standard,
                description = "Absorve energia de um Tile 2x2, removendo efeitos de terreno e aplicando [Silêncio] por 1 turno."
            });
            Register(beelzebumon);

            // 26. Mammonmon (Ganância / Greed)
            var mammonmon = new AppmonData(
                "Mammonmon", "Mammonmon", EvolutionRank.Demon, FunctionalCategory.Tool,
                hp: 2400, mp: 1000, atk: 230, def: 340, intStat: 270, spi: 250, spd: 150, crt: 15,
                passiveName: "Avareza",
                passiveDesc: "Ganha +1% de DEF para cada 1.000 Bits acumulados na partida (máx +50%).",
                passiveId: "avarice_greed",
                lore: "Entidade robótica folheada a ouro e composta por criptomoedas corrompidas. Monopoliza os recursos do jogo.",
                protocol: ProtocolTrinity.Overclock, mov: 3
            );
            mammonmon.skills.Add(new SkillData
            {
                id = "golden_hoard_press", skillName = "Golden Hoard Press",
                category = FunctionalCategory.Tool, effectPower = 200, mpCost = 100, isMagic = false,
                minRange = 1, maxRange = 2,
                description = "Esmaga o alvo com ouro. Dano multiplicado pela quantidade de itens no inventário do jogador."
            });
            mammonmon.skills.Add(new SkillData
            {
                id = "greed_trap", skillName = "Greed Trap",
                category = FunctionalCategory.Tool, effectPower = 110, mpCost = 80, isMagic = false,
                minRange = 1, maxRange = 3, statusToApply = StatusEffectType.Immobilized, statusDurationTurns = 2,
                hasTerrainCreation = true, createsTerrain = TerrainType.GoldenTrap,
                description = "Instala [Armadilha Dourada] em 1 Tile. Inimigo que pisar fica imobilizado e perde 10% do MP máx por turno."
            });
            Register(mammonmon);

            // 27. Belphemon (Preguiça / Sloth)
            var belphemon = new AppmonData(
                "Belphemon", "Belphemon", EvolutionRank.Demon, FunctionalCategory.System,
                hp: 3000, mp: 900, atk: 290, def: 300, intStat: 180, spi: 190, spd: 60, crt: 10,
                passiveName: "Modo de Espera",
                passiveDesc: "Se não agir no turno, recupera 10% do HP máximo e ganha +40% de DEF até a próxima rodada.",
                passiveId: "standby_mode",
                lore: "Uma grande besta digital adormecida em correntes de Sleep Mode. Enquanto dorme, reduz o ritmo e processamento do combate.",
                protocol: ProtocolTrinity.Ping, mov: 2
            );
            belphemon.skills.Add(new SkillData
            {
                id = "system_freeze_slam", skillName = "System Freeze Slam",
                category = FunctionalCategory.System, effectPower = 220, mpCost = 120, isMagic = false,
                minRange = 1, maxRange = 2, aoeType = AttackShapeType.Area, aoeRadius = 1,
                statusToApply = StatusEffectType.Sleep, statusDurationTurns = 2,
                description = "Desperta temporariamente para golpear em área 3x3, aplicando [Sono/Stun] por 2 turnos."
            });
            belphemon.skills.Add(new SkillData
            {
                id = "sleep_aura_tile", skillName = "Sleep Aura Tile",
                category = FunctionalCategory.System, effectPower = 0, mpCost = 70, isMagic = true,
                minRange = 0, maxRange = 1, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.InertiaZone, terrainRadius = 1,
                description = "Transforma o Tile ao redor em [Zona de Inércia]. Unidades na área têm SPD reduzida em 50%."
            });
            Register(belphemon);

            // 28. Satanmon (Ira / Wrath)
            var satanmon = new AppmonData(
                "Satanmon", "Satanmon", EvolutionRank.Demon, FunctionalCategory.Tool,
                hp: 2200, mp: 850, atk: 360, def: 180, intStat: 190, spi: 170, spd: 300, crt: 45,
                passiveName: "Fúria Descontrolada",
                passiveDesc: "Ganha +2% de ATK para cada 1% de HP perdido.",
                passiveId: "uncontrolled_fury",
                lore: "Pesadelo cibernético de serras elétricas e dados sobreaquecidos. Canaliza erros do sistema para aumentar seu poder destrutivo.",
                secondaryCategory: FunctionalCategory.System,
                protocol: ProtocolTrinity.Overclock, mov: 5
            );
            satanmon.skills.Add(new SkillData
            {
                id = "overheat_carnage", skillName = "Overheat Carnage",
                category = FunctionalCategory.Tool, effectPower = 280, mpCost = 130, isMagic = false,
                minRange = 0, maxRange = 2, aoeType = AttackShapeType.Area, aoeRadius = 1,
                description = "Sacrifica 20% do HP para desferir 5 golpes críticos seguidos em raio de 2 Tiles, perfurando toda a armadura."
            });
            satanmon.skills.Add(new SkillData
            {
                id = "furious_trail", skillName = "Furious Trail",
                category = FunctionalCategory.System, effectPower = 160, mpCost = 90, isMagic = true,
                rangeType = AttackShapeType.Line, minRange = 1, maxRange = 4,
                hasTerrainCreation = true, createsTerrain = TerrainType.ChaosFire,
                description = "Deixa uma trilha de [Fogo do Caos] por 4 Tiles em linha reta, infligindo dano de fogo constante."
            });
            Register(satanmon);

            // 29. Leviathanmon (Inveja / Envy)
            var leviathanmon = new AppmonData(
                "Leviathanmon", "Leviathanmon", EvolutionRank.Demon, FunctionalCategory.Security,
                hp: 2500, mp: 1050, atk: 270, def: 270, intStat: 290, spi: 280, spd: 220, crt: 18,
                passiveName: "Olhar Invejoso",
                passiveDesc: "Inimigos em um raio de 3 Tiles que utilizarem buffs ou curas sofrem 15% de dano reflexo.",
                passiveId: "envious_glare",
                lore: "Leviatã marinho digital da Dark Web. Odeia o sucesso dos Appmon da superfície e copia os atributos dos oponentes mais fortes.",
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            leviathanmon.skills.Add(new SkillData
            {
                id = "mirror_corruption", skillName = "Mirror Corruption",
                category = FunctionalCategory.Security, effectPower = 230, mpCost = 110, isMagic = true,
                minRange = 1, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                description = "Copia o valor do atributo mais alto do inimigo e dispara raio de antimatéria proporcional em área de 3 Tiles."
            });
            leviathanmon.skills.Add(new SkillData
            {
                id = "dark_abyssal_tile", skillName = "Dark Abyssal Tile",
                category = FunctionalCategory.Security, effectPower = 150, mpCost = 100, isMagic = true,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                hasTerrainCreation = true, createsTerrain = TerrainType.CorruptedWater, terrainRadius = 1,
                description = "Inunda área 3x3 com [Água Corrompida]. Inimigos no local têm seus buffs convertidos em debuffs."
            });
            Register(leviathanmon);

            // 30. Asmodeusmon (Luxúria / Lust)
            var asmodeusmon = new AppmonData(
                "Asmodeusmon", "Asmodeusmon", EvolutionRank.Demon, FunctionalCategory.Entertainment,
                hp: 2000, mp: 1400, atk: 210, def: 200, intStat: 330, spi: 340, spd: 280, crt: 30,
                passiveName: "Encantamento Enganoso",
                passiveDesc: "Inimigos sob alteração de status (Confusão, Paralisia ou Cegueira) causam 50% a menos de dano.",
                passiveId: "deceptive_charm",
                lore: "Bot ilusório que projeta desejos e distrações holográficas, fazendo com que inimigos ataquem seus próprios aliados.",
                protocol: ProtocolTrinity.Overclock, mov: 4
            );
            asmodeusmon.skills.Add(new SkillData
            {
                id = "holographic_charm", skillName = "Holographic Charm",
                category = FunctionalCategory.Entertainment, effectPower = 170, mpCost = 120, isMagic = true,
                minRange = 0, maxRange = 3, aoeType = AttackShapeType.Area, aoeRadius = 1,
                statusToApply = StatusEffectType.Confused, statusDurationTurns = 2,
                description = "Aplica [Confusão] em área 3x3 por 2 turnos e transfere 30% do MP atual dos alvos para Asmodeusmon."
            });
            asmodeusmon.skills.Add(new SkillData
            {
                id = "illusory_grid", skillName = "Illusory Grid",
                category = FunctionalCategory.Entertainment, effectPower = 0, mpCost = 90, isMagic = true,
                minRange = 1, maxRange = 3, hasTerrainCreation = true, createsTerrain = TerrainType.FalseMirrors,
                description = "Transforma 3 Tiles em [Espelhos Falsos]. Inimigos que entrarem atacam o aliado mais próximo involuntariamente."
            });
            Register(asmodeusmon);

            isInitialized = true;
        }

        private static void Register(AppmonData data)
        {
            registry[data.id] = data;
            registry[data.name] = data;
        }

        private static void CopyInheritedSkills(AppmonData target)
        {
            foreach (var skillName in target.inheritedSkillNames)
            {
                SkillData found = FindSkillInRegistry(skillName);
                if (found != null && !target.skills.Exists(s => s.skillName.Equals(skillName, StringComparison.OrdinalIgnoreCase)))
                {
                    target.skills.Add(found);
                }
            }
        }

        private static SkillData FindSkillInRegistry(string skillName)
        {
            foreach (var appmon in registry.Values)
            {
                var s = appmon.skills.Find(sk => sk.skillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));
                if (s != null) return s;
            }
            return null;
        }

        public static AppmonData Get(string idOrName)
        {
            Initialize();
            if (string.IsNullOrEmpty(idOrName)) return null;
            registry.TryGetValue(idOrName, out var data);
            return data;
        }

        public static List<AppmonData> GetAll()
        {
            Initialize();
            var unique = new HashSet<AppmonData>(registry.Values);
            return new List<AppmonData>(unique);
        }

        public static List<AppmonData> GetByRank(EvolutionRank rank)
        {
            Initialize();
            var results = new List<AppmonData>();
            foreach (var item in GetAll())
            {
                if (item.rank == rank) results.Add(item);
            }
            return results;
        }

        public static AppmonData FindFusion(string parentA, string parentB)
        {
            Initialize();
            foreach (var appmon in GetAll())
            {
                if (appmon.recipeIngredients.Count == 2)
                {
                    bool match1 = appmon.recipeIngredients[0].Equals(parentA, StringComparison.OrdinalIgnoreCase) &&
                                  appmon.recipeIngredients[1].Equals(parentB, StringComparison.OrdinalIgnoreCase);
                    bool match2 = appmon.recipeIngredients[0].Equals(parentB, StringComparison.OrdinalIgnoreCase) &&
                                  appmon.recipeIngredients[1].Equals(parentA, StringComparison.OrdinalIgnoreCase);
                    if (match1 || match2) return appmon;
                }
            }
            return null;
        }
    }
}
