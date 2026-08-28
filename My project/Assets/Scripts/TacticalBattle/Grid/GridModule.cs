using System;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Grid
{
    public static class GridModule
    {
        // ==========================================
        // 2.1 CONVERSÃO PARA TELA (Apenas Camada Visual)
        // ==========================================
        public static Vector2 GridToScreen(GridCoord coord, float tileWidth, float tileHeight, float heightUnit)
        {
            float screenX = (coord.x - coord.y) * (tileWidth / 2f);
            float screenY = (coord.x + coord.y) * (tileHeight / 2f) + coord.z * heightUnit;
            return new Vector2(screenX, screenY);
        }

        // ==========================================
        // 2.2 DISTÂNCIA TÁTICA (Manhattan vs Chebyshev)
        // ==========================================
        public static int DistanceManhattan(GridCoord a, GridCoord b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        public static int DistanceChebyshev(GridCoord a, GridCoord b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        public static int TacticalDistance2D(GridCoord a, GridCoord b, MovementType movementType = MovementType.Manhattan)
        {
            return movementType == MovementType.Chebyshev
                ? DistanceChebyshev(a, b)
                : DistanceManhattan(a, b);
        }

        // Retorna vizinhos ortogonais (4 direções)
        public static readonly GridCoord[] OrthogonalOffsets = new GridCoord[]
        {
            new GridCoord(0, 1),   // North (+Y)
            new GridCoord(1, 0),   // East (+X)
            new GridCoord(0, -1),  // South (-Y)
            new GridCoord(-1, 0)   // West (-X)
        };

        // Retorna vizinhos com diagonais (8 direções)
        public static readonly GridCoord[] ChebyshevOffsets = new GridCoord[]
        {
            new GridCoord(0, 1),
            new GridCoord(1, 0),
            new GridCoord(0, -1),
            new GridCoord(-1, 0),
            new GridCoord(1, 1),
            new GridCoord(1, -1),
            new GridCoord(-1, -1),
            new GridCoord(-1, 1)
        };
    }
}
