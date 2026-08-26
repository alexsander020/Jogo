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
    MDEF = 9,
    SPEED = 10,  // Define a ordem de turnos na batalha
    MOV = 11     // Quantidade de tiles que a unidade pode andar
}
