using System;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainType
{
    Standard = 0,         // Piso digital padrão (Custo: 1, Def: 0%, Eva: 0%)
    CyberPanel = 1,       // Linha de dados (Custo: 1, +5 SP por turno)
    Barricade = 2,        // Barricada pesada (Custo: 2, Def: +20%, Eva: +15%)
    Debris = 3,           // Entulho leve (Custo: 2, Def: +10%, Eva: +10%)
    Corrupted = 4,        // Lodo corrompido (Custo: 2, -10 HP por turno, Eva: -10%)
    HealingTerminal = 5,  // Estação de cura (Custo: 1, +15 HP por turno, Def: +5%)
    Chasm = 6,            // Abismo/Vazio (Intransponível, Custo: 999)
    ElevatedPlatform = 7, // Plataforma elevada (Custo: 1, Def: +10%, Vantagem de Altura)

    // Terrenos Oficiais do Compêndio Appmon & Manual de Ataques Combinados
    Flooded = 8,          // [Terreno Alagado] (Shitakumon / Hydro-Vipermon)
    Fire = 9,             // [Terreno de Fogo] (Flame-Log / Overheat)
    Magma = 10,           // [Terreno de Magma] (Magma-Logmon / Lava Puddle)
    StealthGrid = 11,     // [Sombrio / Stealth Grid] (Shadow-Cam / Electro-Cammon)
    CodeWall = 12,        // [Muralha de Código / Polygon Wall] (Craft-Craft / Architectmon)
    NoiseZone = 13,       // [Zona Ruído] (Sonic-Debugger)
    DigitalOcean = 14,    // [Oceano Digital] (Poseidon-Vipermon)
    RockyMapping = 15,    // [Mapeamento Rochoso] (Genbu-Architectmon)
    Electrified = 16,     // [Terreno Eletrificado] (Seiryu-Vipermon)
    ThermalTrack = 17,    // [Pista Térmica] (Suzaku-Beatmon)
    WindBlades = 18,      // [Lâminas de Vento] (Byakko-Houndmon)
    SovereignThrone = 19, // [Trono do Soberano] (Lucifermon)
    InertiaZone = 20,     // [Zona de Inércia] (Belphemon)
    FalseMirrors = 21,    // [Espelhos Falsos] (Asmodeusmon)
    CorruptedWater = 22,  // [Água Corrompida] (Leviathanmon)
    GoldenTrap = 23,      // [Armadilha Dourada] (Mammonmon)
    ChaosFire = 24,       // [Fogo do Caos] (Satanmon / Cataclysmic Pandemonium)
    ChargeTile = 25       // [Tile Energizado] (Volt-Plug)
}

[Serializable]
public struct TerrainData
{
    public TerrainType type;
    public string displayName;
    public int movementCost;
    public bool isWalkable;
    public float defenseBonusPercent;  // Ex: 0.20f para +20% de defesa
    public float evasionBonusPercent;  // Ex: 0.15f para +15% de evasão
    public int hpTurnEffect;           // Positivo = Cura, Negativo = Dano por turno
    public int spTurnEffect;           // Positivo = Regeneração de SP
    public int mpTurnEffectPercent;    // % de MP recuperado ou drenado por turno
    public Color tintColor;            // Cor indicativa para feedback visual

    public TerrainData(
        TerrainType type,
        string displayName,
        int movementCost,
        bool isWalkable,
        float defenseBonusPercent = 0f,
        float evasionBonusPercent = 0f,
        int hpTurnEffect = 0,
        int spTurnEffect = 0,
        int mpTurnEffectPercent = 0,
        Color? tintColor = null)
    {
        this.type = type;
        this.displayName = displayName;
        this.movementCost = movementCost;
        this.isWalkable = isWalkable;
        this.defenseBonusPercent = defenseBonusPercent;
        this.evasionBonusPercent = evasionBonusPercent;
        this.hpTurnEffect = hpTurnEffect;
        this.spTurnEffect = spTurnEffect;
        this.mpTurnEffectPercent = mpTurnEffectPercent;
        this.tintColor = tintColor ?? Color.white;
    }

    public string GetSummary()
    {
        var traits = new List<string>();
        if (movementCost > 1 && isWalkable) traits.Add($"Custo MOV: {movementCost}");
        if (!isWalkable) traits.Add("INTRANSPONÍVEL");
        if (defenseBonusPercent > 0) traits.Add($"Def +{Mathf.RoundToInt(defenseBonusPercent * 100)}%");
        if (defenseBonusPercent < 0) traits.Add($"Def {Mathf.RoundToInt(defenseBonusPercent * 100)}%");
        if (evasionBonusPercent > 0) traits.Add($"Eva +{Mathf.RoundToInt(evasionBonusPercent * 100)}%");
        if (evasionBonusPercent < 0) traits.Add($"Eva {Mathf.RoundToInt(evasionBonusPercent * 100)}%");
        if (hpTurnEffect > 0) traits.Add($"Cura: +{hpTurnEffect} HP/turno");
        if (hpTurnEffect < 0) traits.Add($"Dano: {hpTurnEffect} HP/turno");
        if (spTurnEffect > 0) traits.Add($"SP: +{spTurnEffect} SP/turno");
        if (mpTurnEffectPercent != 0) traits.Add($"MP: {mpTurnEffectPercent:+0;-0}%/turno");

        return traits.Count > 0 ? string.Join(" | ", traits) : "Normal";
    }
}

public static class TerrainDatabase
{
    private static readonly Dictionary<TerrainType, TerrainData> catalog = new Dictionary<TerrainType, TerrainData>
    {
        {
            TerrainType.Standard,
            new TerrainData(TerrainType.Standard, "Piso Digital", 1, true, 0f, 0f, 0, 0, 0, new Color(0.9f, 0.9f, 0.9f))
        },
        {
            TerrainType.CyberPanel,
            new TerrainData(TerrainType.CyberPanel, "Painel de Dados", 1, true, 0f, 0f, 0, 5, 0, new Color(0.2f, 0.9f, 1.0f))
        },
        {
            TerrainType.Barricade,
            new TerrainData(TerrainType.Barricade, "Barricada (Cover Alto)", 2, true, 0.20f, 0.15f, 0, 0, 0, new Color(0.4f, 0.7f, 1.0f))
        },
        {
            TerrainType.Debris,
            new TerrainData(TerrainType.Debris, "Entulho (Cover Baixo)", 2, true, 0.10f, 0.10f, 0, 0, 0, new Color(0.8f, 0.8f, 0.5f))
        },
        {
            TerrainType.Corrupted,
            new TerrainData(TerrainType.Corrupted, "Chão Corrompido", 2, true, 0f, -0.10f, -10, 0, 0, new Color(0.9f, 0.2f, 0.3f))
        },
        {
            TerrainType.HealingTerminal,
            new TerrainData(TerrainType.HealingTerminal, "Terminal de Cura", 1, true, 0.05f, 0f, 15, 0, 0, new Color(0.3f, 1.0f, 0.4f))
        },
        {
            TerrainType.Chasm,
            new TerrainData(TerrainType.Chasm, "Vazio / Abismo", 999, false, 0f, 0f, 0, 0, 0, new Color(0.1f, 0.1f, 0.1f))
        },
        {
            TerrainType.ElevatedPlatform,
            new TerrainData(TerrainType.ElevatedPlatform, "Plataforma Elevada", 1, true, 0.10f, 0.05f, 0, 0, 0, new Color(1.0f, 0.85f, 0.3f))
        },

        // Compêndio Appmon & Ataques Combinados
        {
            TerrainType.Flooded,
            new TerrainData(TerrainType.Flooded, "Terreno Alagado", 2, true, 0f, 0f, 0, 0, 0, new Color(0.2f, 0.6f, 1.0f, 0.85f))
        },
        {
            TerrainType.Fire,
            new TerrainData(TerrainType.Fire, "Terreno de Fogo", 1, true, 0f, 0f, -15, 0, 0, new Color(1.0f, 0.35f, 0.1f, 0.9f))
        },
        {
            TerrainType.Magma,
            new TerrainData(TerrainType.Magma, "Terreno de Magma", 2, true, -0.20f, 0f, -20, 0, 0, new Color(0.9f, 0.15f, 0.05f, 0.95f))
        },
        {
            TerrainType.StealthGrid,
            new TerrainData(TerrainType.StealthGrid, "Zona Sombria (Stealth)", 1, true, 0f, 0.30f, 0, 0, 0, new Color(0.25f, 0.1f, 0.45f, 0.75f))
        },
        {
            TerrainType.CodeWall,
            new TerrainData(TerrainType.CodeWall, "Muralha de Código", 999, false, 0.40f, 0f, 0, 0, 0, new Color(0.1f, 0.9f, 0.9f, 0.95f))
        },
        {
            TerrainType.NoiseZone,
            new TerrainData(TerrainType.NoiseZone, "Zona Ruído", 2, true, 0f, -0.15f, 0, 0, 0, new Color(0.5f, 0.45f, 0.65f, 0.8f))
        },
        {
            TerrainType.DigitalOcean,
            new TerrainData(TerrainType.DigitalOcean, "Oceano Digital", 3, true, 0f, 0f, 0, 0, -10, new Color(0.05f, 0.25f, 0.85f, 0.9f))
        },
        {
            TerrainType.RockyMapping,
            new TerrainData(TerrainType.RockyMapping, "Mapeamento Rochoso", 1, true, 0.30f, 0f, 0, 0, 0, new Color(0.55f, 0.50f, 0.45f, 0.9f))
        },
        {
            TerrainType.Electrified,
            new TerrainData(TerrainType.Electrified, "Terreno Eletrificado", 1, true, -0.10f, 0f, -10, 0, 0, new Color(1.0f, 0.95f, 0.2f, 0.85f))
        },
        {
            TerrainType.ThermalTrack,
            new TerrainData(TerrainType.ThermalTrack, "Pista Térmica", 1, true, 0f, 0.10f, 0, 0, 0, new Color(1.0f, 0.55f, 0.1f, 0.85f))
        },
        {
            TerrainType.WindBlades,
            new TerrainData(TerrainType.WindBlades, "Lâminas de Vento", 1, true, 0f, 0f, -15, 0, 0, new Color(0.6f, 1.0f, 0.7f, 0.85f))
        },
        {
            TerrainType.SovereignThrone,
            new TerrainData(TerrainType.SovereignThrone, "Trono do Soberano", 1, true, 0.20f, 0f, 0, 0, 0, new Color(0.45f, 0.1f, 0.6f, 0.9f))
        },
        {
            TerrainType.InertiaZone,
            new TerrainData(TerrainType.InertiaZone, "Zona de Inércia", 2, true, 0f, -0.20f, 0, 0, 0, new Color(0.35f, 0.35f, 0.45f, 0.85f))
        },
        {
            TerrainType.FalseMirrors,
            new TerrainData(TerrainType.FalseMirrors, "Espelhos Falsos", 1, true, 0f, 0.25f, 0, 0, 0, new Color(0.85f, 0.85f, 1.0f, 0.8f))
        },
        {
            TerrainType.CorruptedWater,
            new TerrainData(TerrainType.CorruptedWater, "Água Corrompida", 2, true, -0.15f, -0.10f, -10, 0, 0, new Color(0.35f, 0.05f, 0.55f, 0.9f))
        },
        {
            TerrainType.GoldenTrap,
            new TerrainData(TerrainType.GoldenTrap, "Armadilha Dourada", 2, true, 0f, 0f, 0, 0, -10, new Color(1.0f, 0.84f, 0.0f, 0.9f))
        },
        {
            TerrainType.ChaosFire,
            new TerrainData(TerrainType.ChaosFire, "Fogo do Caos", 1, true, -0.25f, 0f, -30, 0, 0, new Color(0.85f, 0.1f, 0.25f, 0.95f))
        },
        {
            TerrainType.ChargeTile,
            new TerrainData(TerrainType.ChargeTile, "Tile Energizado", 1, true, 0f, 0f, 0, 0, 10, new Color(0.3f, 0.85f, 1.0f, 0.85f))
        }
    };

    public static TerrainData Get(TerrainType type)
    {
        if (catalog.TryGetValue(type, out var data))
        {
            return data;
        }
        return catalog[TerrainType.Standard];
    }
}

