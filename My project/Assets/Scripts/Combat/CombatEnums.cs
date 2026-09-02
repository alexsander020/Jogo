using System;
using UnityEngine;

public enum FacingDirection
{
    North = 0, // +Y no grid
    East = 1,  // +X no grid
    South = 2, // -Y no grid
    West = 3   // -X no grid
}

public enum AttackOrientation
{
    Frontal,  // Dano padrão: 100%
    Flank,    // Dano lateral: 125%
    Backstab  // Dano traseiro: 150% + Crítico garantido
}

// Atributos Funcionais de NetShift & Appmon
public enum FunctionalCategory
{
    Social,        // Buffs de Grupo / CC (vence Navi)
    Navi,          // Precisão, Evasão e Velocidade (vence Tool)
    Tool,          // Tanque / Dano Físico (vence Game)
    Game,          // Crítico / Multi-hit (vence Entertainment)
    Entertainment, // Debuffs / Provocação (vence Life)
    Life,          // Cura / Regeneração (vence System)
    System,        // Negação / Dano Puro / Hack (vence Social)
    Security       // Defesa Cibernética / Firewalls / Isolamento (vence System/Malware)
}

// Trindade de Protocolo (Alinhamento Moral / Afinidade)
public enum ProtocolTrinity
{
    Firewall,  // Moral / Proteção (Ordem, retidão)
    Ping,      // Harmonia / Equilíbrio (Pragmatismo, diplomacia)
    Overclock  // Cólera / Ruptura (Impulso, revolta contra o sistema)
}

// Estágios de Evolução / NetFusion / Compêndio Appmon
public enum EvolutionRank
{
    Standard,  // Nami / Comum
    Super,     // Gêmeos de Algoritmo
    Ultimate,  // Fusão Suprema
    God,       // Guardiões Celestiais (Celestial)
    Celestial = God,
    Demon      // Demônios dos Pecados Capitais (Corrupt)
}

// Facção / Time em campo
public enum Team
{
    Player,
    Enemy,
    Neutral
}

// Efeitos de Status do Sistema Tático
public enum StatusEffectType
{
    None = 0,
    Immobilized,       // Não pode se mover (Quarantine Lock, Polygon Trap, Greed Trap)
    Stun,              // Não pode agir nem se mover por 1 turno
    Silence,           // Não pode usar habilidades mágicas ou especiais
    Sleep,             // Adormecido, perde a vez até receber dano ou expirar
    DeepSleep,         // Sono profundo inquebrável (Golden Sloth)
    Burn,              // Dano contínuo de fogo a cada turno
    ChaosBurn,         // Dano intenso contínuo de Fogo do Caos
    Paralysis,         // Chance alta de perder a ação ou velocidade drasticamente reduzida
    Blind,             // Precisão de ataque reduzida
    Panic,             // Unidade aterrorizada / carcaça destruída
    Bleed,             // Sangramento contínuo (Razor Wind Grid)
    Invisible,         // Camuflagem / Furtividade temporária
    GoldPetrification, // Petrificação de ouro
    Frozen,            // Congelamento absoluto
    Confused           // Confusão / Ataca aliados involuntariamente
}

