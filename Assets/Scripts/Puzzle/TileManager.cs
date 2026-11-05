using System.Collections.Generic;
using RoutePuzzle.Master;
using UnityEngine;

namespace RoutePuzzle.Puzzle
{
    /// <summary>
    /// 盤面の見た目を生成・更新する。内側8×8に加えて、外周に境界ブロックを自動配置する。
    /// 仮素材（色付き正方形とバー）で描画する。
    /// </summary>
    public class TileManager : MonoBehaviour
    {
        const float MarkerThickness = 0.14f;
        const float MarkerLength = 0.4f;

        static readonly Color FloorColor = new Color(0.88f, 0.88f, 0.88f);
        static readonly Color VisitedColor = new Color(0.55f, 0.55f, 0.6f);
        static readonly Color BorderColor = new Color(0.15f, 0.15f, 0.18f);
        static readonly Color GoalLockedColor = new Color(0.45f, 0.4f, 0.2f);
        static readonly Color GoalUnlockedColor = new Color(1f, 0.85f, 0.1f);

        [SerializeField, Tooltip("ワープグループの色。未設定ならマゼンタで描画する")]
        WarpGroupMaster warpGroups;

        readonly Dictionary<Vector2Int, SpriteRenderer> cells = new Dictionary<Vector2Int, SpriteRenderer>();
        readonly Dictionary<Vector2Int, StageTileData> tileData = new Dictionary<Vector2Int, StageTileData>();
        readonly Dictionary<Vector2Int, Transform> markerRoots = new Dictionary<Vector2Int, Transform>();
        readonly List<GameObject> spawned = new List<GameObject>();

        /// <summary>盤面中央を原点としたセル中心のワールド座標。</summary>
        public Vector3 CellToWorld(Vector2Int cell)
            => new Vector3(cell.x - (GridConst.Width - 1) * 0.5f, cell.y - (GridConst.Height - 1) * 0.5f, 0f);

        /// <summary>StageMasterから盤面を生成する。既に生成済みなら作り直す。</summary>
        public void Build(StageMaster stage)
        {
            Clear();

            // 外周の境界ブロック（-1 .. Width/Height）
            for (int x = -1; x <= GridConst.Width; x++)
                for (int y = -1; y <= GridConst.Height; y++)
                {
                    bool inside = x >= 0 && x < GridConst.Width && y >= 0 && y < GridConst.Height;
                    if (!inside) CreateSquare(new Vector2Int(x, y), BorderColor, 0, transform);
                }

            foreach (var t in stage.tiles) tileData[t.Position] = t;

            for (int x = 0; x < GridConst.Width; x++)
                for (int y = 0; y < GridConst.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    tileData.TryGetValue(pos, out var data);
                    var sr = CreateSquare(pos, data != null ? TileColor(data) : FloorColor, 0, transform);
                    cells[pos] = sr;

                    var root = new GameObject("Markers").transform;
                    root.SetParent(sr.transform, false);
                    markerRoots[pos] = root;
                    spawned.Add(root.gameObject);
                }
        }

        /// <summary>現在の状態（通過済み・回転・ゴールのアンロック）を見た目に反映する。</summary>
        public void Refresh(StageState state)
        {
            foreach (var kv in cells)
            {
                tileData.TryGetValue(kv.Key, out var data);
                Color color = data != null ? TileColor(data) : FloorColor;

                if (data != null && data.tileType == TileType.GOAL)
                    color = state.GoalUnlocked ? GoalUnlockedColor : GoalLockedColor;
                else if (state.IsVisited(kv.Key))
                    color = VisitedColor;

                kv.Value.color = color;
                RebuildMarkers(kv.Key, data, state);
            }
        }

        /// <summary>生成済みの盤面を破棄する。</summary>
        public void Clear()
        {
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
            cells.Clear();
            tileData.Clear();
            markerRoots.Clear();
        }

        /// <summary>タイル種別ごとの基本色（仮素材）。</summary>
        Color TileColor(StageTileData t)
        {
            switch (t.tileType)
            {
                case TileType.START: return new Color(0.5f, 0.85f, 1f);
                case TileType.GOAL: return GoalLockedColor;
                case TileType.BLOCK: return new Color(0.3f, 0.2f, 0.15f);
                case TileType.STRAIGHT:
                case TileType.TURN: return new Color(0.7f, 0.85f, 1f);
                case TileType.FORCE_MOVE: return new Color(1f, 0.7f, 0.35f);
                case TileType.ROTATE: return new Color(0.75f, 0.55f, 0.95f);
                case TileType.CHECKPOINT:
                case TileType.ORDERED_CHECKPOINT: return new Color(0.55f, 0.9f, 0.55f);
                case TileType.WARP:
                    var g = warpGroups != null ? warpGroups.Find(t.groupId) : null;
                    return g != null ? g.color : Color.magenta;
                default: return FloorColor;
            }
        }

        /// <summary>方向の矢印バーと、順序チェックポイントの番号ドットを描き直す。</summary>
        void RebuildMarkers(Vector2Int pos, StageTileData data, StageState state)
        {
            var root = markerRoots[pos];
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            if (data == null) return;

            switch (data.tileType)
            {
                case TileType.FORCE_MOVE:
                    AddBars(root, data.direction, new Color(0.6f, 0.25f, 0f));
                    break;
                case TileType.STRAIGHT:
                    AddBars(root, state.GetAllowedDirections(data), new Color(0.1f, 0.3f, 0.6f));
                    break;
                case TileType.TURN:
                    // 濃い青=出口方向 / 薄い青=進入可能な移動方向
                    AddBars(root, state.GetEntryDirections(data), new Color(0.35f, 0.55f, 0.85f));
                    AddBars(root, state.GetAllowedDirections(data), new Color(0.1f, 0.3f, 0.6f));
                    break;
                case TileType.ORDERED_CHECKPOINT:
                    for (int i = 0; i < data.orderNo; i++)
                    {
                        var dot = CreateChildSquare(root, new Color(0.1f, 0.45f, 0.1f), 0.12f);
                        dot.transform.localPosition = new Vector3((i - (data.orderNo - 1) * 0.5f) * 0.18f, 0f, 0f);
                    }
                    break;
            }
        }

        void AddBars(Transform root, Direction mask, Color color)
        {
            foreach (var d in new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left })
            {
                if (!mask.Has(d)) continue;
                var off = d.ToOffset();
                var bar = CreateChildSquare(root, color, 1f);
                bar.transform.localPosition = new Vector3(off.x, off.y, 0f) * (MarkerLength * 0.5f);
                bar.transform.localScale = off.x != 0
                    ? new Vector3(MarkerLength, MarkerThickness, 1f)
                    : new Vector3(MarkerThickness, MarkerLength, 1f);
            }
        }

        SpriteRenderer CreateSquare(Vector2Int cell, Color color, int order, Transform parent)
        {
            var go = new GameObject($"Cell_{cell.x}_{cell.y}");
            go.transform.SetParent(parent, false);
            go.transform.position = CellToWorld(cell);
            go.transform.localScale = Vector3.one * 0.96f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White;
            sr.color = color;
            sr.sortingOrder = order;
            spawned.Add(go);
            return sr;
        }

        SpriteRenderer CreateChildSquare(Transform parent, Color color, float scale)
        {
            var go = new GameObject("Marker");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White;
            sr.color = color;
            sr.sortingOrder = 1;
            return sr;
        }
    }
}
