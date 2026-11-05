using System;
using System.Collections.Generic;
using System.Reflection;

namespace RoutePuzzle.Master
{
    /// <summary>同じグループ名を持つTileTypeは、同一ステージに共存できない。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ExclusiveGroupAttribute : Attribute
    {
        public string Group { get; }
        public ExclusiveGroupAttribute(string group) { Group = group; }
    }

    /// <summary>特殊タイル種別。FLOOR（通常の床）は列挙せず、配置が無い座標を床として扱う。</summary>
    public enum TileType
    {
        START,
        GOAL,
        BLOCK,
        STRAIGHT,
        TURN,
        FORCE_MOVE,
        WARP,
        ROTATE,

        /// <summary>通常チェックポイント（通過順不問）。ORDERED_CHECKPOINTと排他。</summary>
        [ExclusiveGroup("Checkpoint")] CHECKPOINT,

        /// <summary>順序指定チェックポイント。CHECKPOINTと排他。</summary>
        [ExclusiveGroup("Checkpoint")] ORDERED_CHECKPOINT,
    }

    public static class TileTypeExtensions
    {
        static readonly Dictionary<TileType, string> ExclusiveGroups = BuildExclusiveGroups();

        /// <summary>ExclusiveGroup 属性から、タイル種別ごとの排他グループ名の対応表を作る。</summary>
        static Dictionary<TileType, string> BuildExclusiveGroups()
        {
            var map = new Dictionary<TileType, string>();
            foreach (TileType t in Enum.GetValues(typeof(TileType)))
            {
                var field = typeof(TileType).GetField(t.ToString());
                var attr = field?.GetCustomAttribute<ExclusiveGroupAttribute>();
                if (attr != null) map[t] = attr.Group;
            }
            return map;
        }

        /// <summary>排他グループ名。排他指定が無ければnull。</summary>
        public static string GetExclusiveGroup(this TileType type)
            => ExclusiveGroups.TryGetValue(type, out var g) ? g : null;
    }
}
