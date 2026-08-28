using System;
using System.Collections.Generic;
using TacticalBattle.Core;
using UnityEngine;

namespace TacticalBattle.Grid
{
    public static class AttackRangeService
    {
        public static List<GridCoord> GetValidTargetCells(
            GridCoord origin,
            GridFacing facing,
            AttackRangeShape shape,
            GridState grid,
            GridConfig config = null)
        {
            config ??= BattleRulesConfig.DefaultGrid;
            var result = new List<GridCoord>();

            switch (shape.type)
            {
                case AttackShapeType.Single:
                    result = GetSingleShapeCells(origin, shape.minRange, shape.maxRange, grid, config);
                    break;

                case AttackShapeType.Line:
                    result = GetLineShapeCells(origin, facing, shape.length, grid, config);
                    break;

                case AttackShapeType.Cone:
                    result = GetConeShapeCells(origin, facing, shape.radius, shape.angleDegrees, grid, config);
                    break;

                case AttackShapeType.Area:
                    result = GetSingleShapeCells(origin, shape.minRange, shape.maxRange, grid, config);
                    break;
            }

            return result;
        }

        private static List<GridCoord> GetSingleShapeCells(GridCoord origin, int minRange, int maxRange, GridState grid, GridConfig config)
        {
            var result = new List<GridCoord>();

            for (int dx = -maxRange; dx <= maxRange; dx++)
            {
                for (int dy = -maxRange; dy <= maxRange; dy++)
                {
                    var targetCoord = new GridCoord(origin.x + dx, origin.y + dy);
                    if (!grid.IsInside(targetCoord.x, targetCoord.y)) continue;

                    int dist = GridModule.TacticalDistance2D(origin, targetCoord, config.movementType);
                    if (dist >= minRange && dist <= maxRange)
                    {
                        var cell = grid.GetCell(targetCoord.x, targetCoord.y);
                        if (cell != null)
                        {
                            targetCoord.z = cell.coord.z;
                        }

                        if (!config.checkLineOfSight || HasLineOfSight(origin, targetCoord, grid))
                        {
                            result.Add(targetCoord);
                        }
                    }
                }
            }

            return result;
        }

        private static List<GridCoord> GetLineShapeCells(GridCoord origin, GridFacing facing, int length, GridState grid, GridConfig config)
        {
            var result = new List<GridCoord>();
            GridCoord forwardOffset = GetFacingOffset(facing);

            for (int step = 1; step <= length; step++)
            {
                var targetCoord = new GridCoord(origin.x + forwardOffset.x * step, origin.y + forwardOffset.y * step);
                if (!grid.IsInside(targetCoord.x, targetCoord.y)) break;

                var cell = grid.GetCell(targetCoord.x, targetCoord.y);
                if (cell != null)
                {
                    targetCoord.z = cell.coord.z;
                }

                if (config.checkLineOfSight && cell != null && !cell.isWalkable)
                {
                    // Obstáculo sólido bloqueia continuação da linha
                    break;
                }

                result.Add(targetCoord);
            }

            return result;
        }

        private static List<GridCoord> GetConeShapeCells(GridCoord origin, GridFacing facing, int radius, float angleDegrees, GridState grid, GridConfig config)
        {
            var result = new List<GridCoord>();
            Vector2 forwardDir = GetFacingVector(facing);
            float halfAngle = angleDegrees / 2f;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var targetCoord = new GridCoord(origin.x + dx, origin.y + dy);
                    if (!grid.IsInside(targetCoord.x, targetCoord.y)) continue;

                    int dist = GridModule.TacticalDistance2D(origin, targetCoord, config.movementType);
                    if (dist > radius) continue;

                    Vector2 toTarget = new Vector2(dx, dy).normalized;
                    float angle = Vector2.Angle(forwardDir, toTarget);

                    if (angle <= halfAngle)
                    {
                        var cell = grid.GetCell(targetCoord.x, targetCoord.y);
                        if (cell != null) targetCoord.z = cell.coord.z;

                        if (!config.checkLineOfSight || HasLineOfSight(origin, targetCoord, grid))
                        {
                            result.Add(targetCoord);
                        }
                    }
                }
            }

            return result;
        }

        public static List<GridCoord> GetAreaCells(GridCoord center, int radius, GridState grid)
        {
            var result = new List<GridCoord>();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var coord = new GridCoord(center.x + dx, center.y + dy);
                    if (!grid.IsInside(coord.x, coord.y)) continue;

                    int dist = Math.Abs(dx) + Math.Abs(dy);
                    if (dist <= radius)
                    {
                        var cell = grid.GetCell(coord.x, coord.y);
                        if (cell != null) coord.z = cell.coord.z;
                        result.Add(coord);
                    }
                }
            }

            return result;
        }

        // Checagem simplificada de Linha de Visão usando algoritmo de Bresenham
        public static bool HasLineOfSight(GridCoord from, GridCoord to, GridState grid)
        {
            int dx = Math.Abs(to.x - from.x);
            int dy = Math.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;

            int currX = from.x;
            int currY = from.y;

            while (currX != to.x || currY != to.y)
            {
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    currX += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    currY += sy;
                }

                if (currX == to.x && currY == to.y) break;

                var cell = grid.GetCell(currX, currY);
                if (cell != null && !cell.isWalkable)
                {
                    return false; // Bloqueado por obstáculo
                }
            }

            return true;
        }

        public static GridCoord GetFacingOffset(GridFacing facing)
        {
            return facing switch
            {
                GridFacing.North => new GridCoord(0, 1),
                GridFacing.East => new GridCoord(1, 0),
                GridFacing.South => new GridCoord(0, -1),
                GridFacing.West => new GridCoord(-1, 0),
                _ => new GridCoord(0, -1)
            };
        }

        public static Vector2 GetFacingVector(GridFacing facing)
        {
            return facing switch
            {
                GridFacing.North => Vector2.up,
                GridFacing.East => Vector2.right,
                GridFacing.South => Vector2.down,
                GridFacing.West => Vector2.left,
                _ => Vector2.down
            };
        }
    }
}
