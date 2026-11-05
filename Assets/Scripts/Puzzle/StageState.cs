using System.Collections.Generic;
using RoutePuzzle.Master;
using UnityEngine;

namespace RoutePuzzle.Puzzle
{
    public enum MoveResult
    {
        /// <summary>進入不可。衝突音を再生し、移動もリセットもしない。</summary>
        Blocked,
        /// <summary>移動した（強制移動・ワープ・回転を含む）。</summary>
        Moved,
        /// <summary>強制移動中に進入不可のマスへ衝突した。消滅演出のあとリセットする。</summary>
        Died,
        /// <summary>ゴールに到達した。</summary>
        Cleared,
    }

    /// <summary>1回の入力の結果。演出側はこの内容を再生する。</summary>
    public class MoveOutcome
    {
        public MoveResult Result;

        /// <summary>プレイヤーが通過・着地した座標（順番どおり）。ワープは着地先も含む。</summary>
        public readonly List<Vector2Int> Path = new List<Vector2Int>();

        /// <summary>この入力でゴールがアンロックされた。</summary>
        public bool GoalUnlockedNow;

        /// <summary>この入力で回転ギミックが発動した。回転マスで移動が止まるため、1入力につき最大1回。</summary>
        public bool Rotated;

        /// <summary>この入力のあと詰み状態になった（リセットボタンを強調する）。</summary>
        public bool IsStuck;
    }

    /// <summary>
    /// 1ステージ分の実行時状態と移動ルール。UnityのMonoBehaviourには依存しない。
    /// マスターデータ(StageMaster)は変更しない。
    /// </summary>
    public class StageState
    {
        static readonly Direction[] AllDirections =
            { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        readonly Dictionary<Vector2Int, StageTileData> tiles = new Dictionary<Vector2Int, StageTileData>();
        readonly Dictionary<Vector2Int, Vector2Int> warpPartner = new Dictionary<Vector2Int, Vector2Int>();
        readonly HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        readonly Vector2Int startPosition;
        readonly int requiredCheckpoints;

        public Vector2Int PlayerPosition { get; private set; }
        public int RotationCount { get; private set; }
        public int CheckpointCount { get; private set; }
        public bool OrderViolated { get; private set; }
        public bool GoalUnlocked { get; private set; }
        public bool IsCleared { get; private set; }
        public bool IsStuck { get; private set; }

        public StageState(StageMaster master)
        {
            var warpFirst = new Dictionary<int, Vector2Int>();
            foreach (var t in master.tiles)
            {
                tiles[t.Position] = t;
                switch (t.tileType)
                {
                    case TileType.START:
                        startPosition = t.Position;
                        break;
                    case TileType.CHECKPOINT:
                    case TileType.ORDERED_CHECKPOINT:
                        requiredCheckpoints++;
                        break;
                    case TileType.WARP:
                        if (warpFirst.TryGetValue(t.groupId, out var other))
                        {
                            warpPartner[t.Position] = other;
                            warpPartner[other] = t.Position;
                        }
                        else warpFirst[t.groupId] = t.Position;
                        break;
                }
            }
            Reset();
        }

        /// <summary>ステージ開始時の状態に戻す。</summary>
        public void Reset()
        {
            visited.Clear();
            visited.Add(startPosition);
            PlayerPosition = startPosition;
            RotationCount = 0;
            CheckpointCount = 0;
            OrderViolated = false;
            GoalUnlocked = requiredCheckpoints == 0;
            IsCleared = false;
            IsStuck = ComputeStuck();
        }

        public bool IsVisited(Vector2Int pos) => visited.Contains(pos);

        /// <summary>現在の回転状態を反映した、タイルの進入可能方向。</summary>
        public Direction GetEntryDirections(StageTileData t) => t.entryDirections.RotateClockwise(RotationCount);

        /// <summary>現在の回転状態を反映した、タイルの許容方向（STRAIGHTは軸、TURNは出口）。</summary>
        public Direction GetAllowedDirections(StageTileData t) => t.allowedDirections.RotateClockwise(RotationCount);

        StageTileData TileAt(Vector2Int pos) => tiles.TryGetValue(pos, out var t) ? t : null;

        static bool InBounds(Vector2Int p)
            => p.x >= 0 && p.x < GridConst.Width && p.y >= 0 && p.y < GridConst.Height;

        /// <summary>現在地から dir 方向へ出られるか（STRAIGHT / TURN の出口判定）。</summary>
        bool CanLeave(Vector2Int pos, Direction dir)
        {
            var t = TileAt(pos);
            if (t == null) return true;
            if (t.tileType == TileType.STRAIGHT || t.tileType == TileType.TURN)
                return GetAllowedDirections(t).Has(dir);
            return true;
        }

        /// <summary>pos へ dir 方向で進入できるか。外周・BLOCK・通過済み・ロック中ゴール・入口方向を判定する。</summary>
        bool CanEnter(Vector2Int pos, Direction dir)
        {
            if (!InBounds(pos) || visited.Contains(pos)) return false;
            var t = TileAt(pos);
            if (t == null) return true;
            switch (t.tileType)
            {
                case TileType.BLOCK: return false;
                case TileType.GOAL: return GoalUnlocked;
                case TileType.STRAIGHT: return GetAllowedDirections(t).Has(dir);
                case TileType.TURN: return GetEntryDirections(t).Has(dir);
                default: return true;
            }
        }

        bool CanStep(Direction dir)
            => CanLeave(PlayerPosition, dir) && CanEnter(PlayerPosition + dir.ToOffset(), dir);

        bool ComputeStuck()
        {
            if (IsCleared) return false;
            if (OrderViolated) return true;
            foreach (var d in AllDirections)
                if (CanStep(d)) return false;
            return true;
        }

        /// <summary>1入力ぶんの移動を試みる。強制移動・ワープ・回転・チェックポイントの効果もここで処理する。</summary>
        public MoveOutcome TryMove(Direction dir)
        {
            var outcome = new MoveOutcome();
            if (IsCleared || !dir.IsSingle() || !CanStep(dir))
            {
                outcome.Result = MoveResult.Blocked;
                outcome.IsStuck = IsStuck;
                return outcome;
            }

            var pos = PlayerPosition + dir.ToOffset();
            while (true)
            {
                visited.Add(pos);
                PlayerPosition = pos;
                outcome.Path.Add(pos);

                var t = TileAt(pos);
                Vector2Int? next = null;
                Direction nextDir = Direction.None;

                if (t != null)
                {
                    switch (t.tileType)
                    {
                        case TileType.GOAL:
                            IsCleared = true;
                            IsStuck = false;
                            outcome.Result = MoveResult.Cleared;
                            return outcome;

                        case TileType.ROTATE:
                            RotationCount = (RotationCount + 1) % 4;
                            outcome.Rotated = true;
                            break;

                        case TileType.CHECKPOINT:
                            CheckpointCount++;
                            UnlockGoalIfReady(outcome);
                            break;

                        case TileType.ORDERED_CHECKPOINT:
                            if (t.orderNo == CheckpointCount + 1)
                            {
                                CheckpointCount++;
                                UnlockGoalIfReady(outcome);
                            }
                            else OrderViolated = true;
                            break;

                        case TileType.WARP:
                            if (!warpPartner.TryGetValue(pos, out var partner) || visited.Contains(partner))
                                return Die(outcome);
                            // 着地先は「通常の床を通過した」扱い。効果は発動しない。
                            visited.Add(partner);
                            PlayerPosition = partner;
                            outcome.Path.Add(partner);
                            break;

                        case TileType.FORCE_MOVE:
                            next = pos + t.direction.ToOffset();
                            nextDir = t.direction;
                            break;
                    }
                }

                if (next == null) break;
                if (!CanEnter(next.Value, nextDir)) return Die(outcome);
                pos = next.Value;
            }

            outcome.Result = MoveResult.Moved;
            IsStuck = ComputeStuck();
            outcome.IsStuck = IsStuck;
            return outcome;
        }

        MoveOutcome Die(MoveOutcome outcome)
        {
            outcome.Result = MoveResult.Died;
            return outcome;
        }

        void UnlockGoalIfReady(MoveOutcome outcome)
        {
            if (GoalUnlocked || CheckpointCount < requiredCheckpoints) return;
            GoalUnlocked = true;
            outcome.GoalUnlockedNow = true;
        }
    }
}
