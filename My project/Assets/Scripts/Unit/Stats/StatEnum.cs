using System;

public enum StatEnum
{
    HP = 0,
    MaxHp = 1,
    SP = 2,      // SP do NetShift (usado para manter NetFusion e habilidades)
    MaxSp = 3,
    MP = 4,
    MaxMp = 5,
    ATK = 6,
    DEF = 7,
    MATK = 8,
    INT = 8,     // Inteligência / Ataque Mágico (Compêndio Appmon)
    MDEF = 9,
    SPI = 9,     // Espírito / Defesa Mágica (Compêndio Appmon)
    SPEED = 10,  // Velocidade de ação
    SPD = 10,    // Velocidade (Compêndio Appmon)
    MOV = 11,    // Quantidade de tiles que a unidade pode andar
    CRT = 12     // Taxa de Acerto Crítico em % (Compêndio Appmon)
}

