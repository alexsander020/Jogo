using System;
using UnityEngine;

public static class DirectionUtils
{
    // Converte um vetor de deslocamento na direção de orientação mais próxima e precisa
    public static FacingDirection VectorToDirection(Vector3Int dir)
    {
        if (dir.x != 0 && dir.y == 0)
        {
            return dir.x > 0 ? FacingDirection.East : FacingDirection.West;
        }
        else if (dir.y != 0 && dir.x == 0)
        {
            return dir.y > 0 ? FacingDirection.North : FacingDirection.South;
        }
        else if (dir.x != 0 && dir.y != 0)
        {
            // Para diagonais, avalia magnitude ou o sinal predominante
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                return dir.x > 0 ? FacingDirection.East : FacingDirection.West;
            }
            else if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            {
                return dir.y > 0 ? FacingDirection.North : FacingDirection.South;
            }
            else
            {
                // Em caso de empate |x| == |y|, analisa o eixo y (North/South)
                return dir.y > 0 ? FacingDirection.North : FacingDirection.South;
            }
        }
        return FacingDirection.South;
    }

    // Converte FacingDirection para Vector3Int no grid
    public static Vector3Int DirectionToVector(FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.North:
                return Vector3Int.up;
            case FacingDirection.East:
                return Vector3Int.right;
            case FacingDirection.South:
                return Vector3Int.down;
            case FacingDirection.West:
                return Vector3Int.left;
            default:
                return Vector3Int.down;
        }
    }

    // Retorna nome legível e ícone da direção
    public static string GetDirectionName(FacingDirection dir)
    {
        return dir switch
        {
            FacingDirection.North => "NORTE (▲ / +Y)",
            FacingDirection.East => "LESTE (► / +X)",
            FacingDirection.South => "SUL (▼ / -Y)",
            FacingDirection.West => "OESTE (◄ / -X)",
            _ => "SUL (▼)"
        };
    }


    // Determina a orientação do ataque: Frontal, Flanco (Lateral) ou Backstab (Traseiro)
    public static AttackOrientation GetAttackOrientation(FacingDirection targetFacing, Vector3Int targetPos, Vector3Int attackerPos)
    {
        Vector3Int diff = attackerPos - targetPos;
        if (diff == Vector3Int.zero)
        {
            return AttackOrientation.Frontal;
        }

        // Direção de onde o atacante está atacando em relação ao alvo
        FacingDirection attackFromDir = VectorToDirection(diff);

        // Se o atacante está na mesma direção para a qual o alvo olha -> Ataque Frontal
        // Se o atacante está na direção oposta à que o alvo olha -> Ataque Traseiro (Backstab)
        // Se está nos lados -> Flanco
        int facingInt = (int)targetFacing;
        int attackFromInt = (int)attackFromDir;
        int delta = Mathf.Abs(facingInt - attackFromInt);
        if (delta == 3) delta = 1; // Wrap-around circular entre North(0) e West(3)

        if (delta == 0)
        {
            return AttackOrientation.Frontal;
        }
        else if (delta == 2)
        {
            return AttackOrientation.Backstab;
        }
        else
        {
            return AttackOrientation.Flank;
        }
    }

    // Retorna o multiplicador de dano posicional e se o crítico é garantido (conforme GDD V3)
    public static float GetOrientationDamageMultiplier(AttackOrientation orientation, out bool guaranteedCrit)
    {
        switch (orientation)
        {
            case AttackOrientation.Backstab:
                guaranteedCrit = true;
                return 1.50f; // 150% + Crítico garantido
            case AttackOrientation.Flank:
                guaranteedCrit = false;
                return 1.25f; // 125%
            case AttackOrientation.Frontal:
            default:
                guaranteedCrit = false;
                return 1.00f; // 100%
        }
    }

    // Ciclo dos 7 Atributos Funcionais:
    // Social -> Navi -> Tool -> Game -> Entertainment -> Life -> System -> Social
    public static bool HasCategoryAdvantage(FunctionalCategory attacker, FunctionalCategory defender)
    {
        switch (attacker)
        {
            case FunctionalCategory.Social:
                return defender == FunctionalCategory.Navi;
            case FunctionalCategory.Navi:
                return defender == FunctionalCategory.Tool;
            case FunctionalCategory.Tool:
                return defender == FunctionalCategory.Game;
            case FunctionalCategory.Game:
                return defender == FunctionalCategory.Entertainment;
            case FunctionalCategory.Entertainment:
                return defender == FunctionalCategory.Life;
            case FunctionalCategory.Life:
                return defender == FunctionalCategory.System;
            case FunctionalCategory.System:
                return defender == FunctionalCategory.Social;
            case FunctionalCategory.Security:
                return defender == FunctionalCategory.System;
            default:
                return false;
        }
    }

    // Trindade de Protocolo:
    // Firewall (Vacina) > Overclock (Vírus) > Ping (Dados) > Firewall (Vacina)
    public static bool HasProtocolAdvantage(ProtocolTrinity attacker, ProtocolTrinity defender)
    {
        switch (attacker)
        {
            case ProtocolTrinity.Firewall:
                return defender == ProtocolTrinity.Overclock;
            case ProtocolTrinity.Overclock:
                return defender == ProtocolTrinity.Ping;
            case ProtocolTrinity.Ping:
                return defender == ProtocolTrinity.Firewall;
            default:
                return false;
        }
    }
}
