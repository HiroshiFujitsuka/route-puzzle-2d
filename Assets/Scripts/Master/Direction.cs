using System;
using UnityEngine;

namespace RoutePuzzle.Master
{
    /// <summary>移動方向。複数方向はビットORで表す（例: 縦直進 = Up | Down）。</summary>
    [Flags]
    public enum Direction
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8,
    }

    public static class DirectionExtensions
    {
        public const Direction All = Direction.Up | Direction.Right | Direction.Down | Direction.Left;

        /// <summary>方向を90°時計回りに回転させる（Up→Right→Down→Left→Up）。複数方向フラグにも対応。</summary>
        public static Direction RotateClockwise(this Direction d)
        {
            int v = (int)d & (int)All;
            // 4ビットの巡回左シフト
            int rotated = ((v << 1) | (v >> 3)) & (int)All;
            return (Direction)rotated;
        }

        /// <summary>単一方向の反対向き（Up↔Down, Left↔Right）。単一方向以外はNoneを返す。</summary>
        public static Direction Opposite(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
        }

        /// <summary>90°時計回りの回転を count 回適用する。</summary>
        public static Direction RotateClockwise(this Direction d, int count)
        {
            int times = ((count % 4) + 4) % 4;
            for (int i = 0; i < times; i++) d = d.RotateClockwise();
            return d;
        }

        /// <summary>単一方向のグリッド上の移動量。Upはy+1（y軸は上向き）。単一方向以外はzeroを返す。</summary>
        public static Vector2Int ToOffset(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Right: return new Vector2Int(1, 0);
                case Direction.Down: return new Vector2Int(0, -1);
                case Direction.Left: return new Vector2Int(-1, 0);
                default: return Vector2Int.zero;
            }
        }

        /// <summary>ちょうど1方向だけが立っているか。</summary>
        public static bool IsSingle(this Direction d)
        {
            int v = (int)d;
            return v != 0 && (v & (v - 1)) == 0 && (v & ~(int)All) == 0;
        }

        public static bool Has(this Direction set, Direction d) => d != Direction.None && (set & d) == d;
    }
}
