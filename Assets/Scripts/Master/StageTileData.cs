using System;
using UnityEngine;

namespace RoutePuzzle.Master
{
    /// <summary>M_StageTile: ステージ内の特殊タイル配置1件。行が無い座標は床として扱う。</summary>
    [Serializable]
    public class StageTileData
    {
        [Tooltip("列座標 0〜7")]
        public int x;

        [Tooltip("行座標 0〜7（上方向がプラス）")]
        public int y;

        public TileType tileType;

        [Tooltip("FORCE_MOVE: 強制移動の向き（単一方向）")]
        public Direction direction;

        [Tooltip("WARP: ワープグループID（1〜4）。同値の2マスが対。0=未使用")]
        public int groupId;

        [Tooltip("ORDERED_CHECKPOINT: 通過順序（1始まり）。0=未使用")]
        public int orderNo;

        [Tooltip("TURN: 進入可能な移動方向（初期向き）")]
        public Direction entryDirections;

        [Tooltip("STRAIGHT: 入出可能な軸 / TURN: 出られる移動方向（初期向き）")]
        public Direction allowedDirections;

        public Vector2Int Position => new Vector2Int(x, y);
    }
}
