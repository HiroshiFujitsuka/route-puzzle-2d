using System.Collections.Generic;
using UnityEngine;

namespace RoutePuzzle.Master
{
    /// <summary>StageMasterの読み込み時検証。エラーメッセージのリストを返す（空なら正常）。</summary>
    public static class StageMasterValidator
    {
        public static List<string> Validate(StageMaster stage)
        {
            var errors = new List<string>();
            if (stage == null)
            {
                errors.Add("stage is null");
                return errors;
            }

            var tiles = stage.tiles ?? new List<StageTileData>();
            var map = new Dictionary<Vector2Int, StageTileData>();

            // 1. 座標範囲 / 重複配置禁止（1座標に1タイル）
            foreach (var t in tiles)
            {
                if (t == null) { errors.Add("tiles に null 要素があります。"); continue; }
                if (t.x < 0 || t.x >= GridConst.Width || t.y < 0 || t.y >= GridConst.Height)
                {
                    errors.Add($"({t.x},{t.y}) {t.tileType}: 座標がグリッド範囲外です。");
                    continue;
                }
                if (!map.TryAdd(t.Position, t))
                    errors.Add($"({t.x},{t.y}): 同じ座標に複数のタイルが配置されています。");
            }

            // 2. START / GOAL はちょうど1つ
            CheckExactlyOne(tiles, TileType.START, errors);
            CheckExactlyOne(tiles, TileType.GOAL, errors);

            // 3. 排他グループ（CHECKPOINT と ORDERED_CHECKPOINT の共存禁止など）
            var groupOwner = new Dictionary<string, TileType>();
            foreach (var t in tiles)
            {
                if (t == null) continue;
                var g = t.tileType.GetExclusiveGroup();
                if (g == null) continue;
                if (groupOwner.TryGetValue(g, out var other))
                {
                    if (other != t.tileType)
                        errors.Add($"{other} と {t.tileType} は同一ステージに共存できません（排他グループ: {g}）。");
                }
                else groupOwner[g] = t.tileType;
            }

            ValidateWarp(tiles, errors);
            ValidateOrderedCheckpoint(tiles, errors);
            ValidateStraightAndTurn(tiles, errors);
            ValidateForceMove(tiles, map, errors);

            return errors;
        }

        /// <summary>10. (difficultyId, stageNo) と stageId の重複検査。</summary>
        public static List<string> ValidateStageSet(IEnumerable<StageMaster> stages)
        {
            var errors = new List<string>();
            var ids = new HashSet<int>();
            var keys = new HashSet<(int, int)>();
            foreach (var s in stages)
            {
                if (s == null) continue;
                if (!ids.Add(s.stageId))
                    errors.Add($"{s.name}: stageId {s.stageId} が重複しています。");
                if (!keys.Add((s.difficultyId, s.stageNo)))
                    errors.Add($"{s.name}: (difficultyId={s.difficultyId}, stageNo={s.stageNo}) が重複しています。");
            }
            return errors;
        }

#if UNITY_EDITOR
        /// <summary>プロジェクト内の全StageMasterを集め、個別検証と重複検査(ValidateStageSet)をまとめて実行する。</summary>
        [UnityEditor.MenuItem("RoutePuzzle/Validate All Stages")]
        public static void ValidateAllStagesInProject()
        {
            var stages = new List<StageMaster>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:StageMaster"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<StageMaster>(path);
                if (s != null) stages.Add(s);
            }

            int errorCount = 0;
            foreach (var s in stages)
                foreach (var e in Validate(s)) { Debug.LogWarning($"[StageMaster] {s.name}: {e}", s); errorCount++; }
            foreach (var e in ValidateStageSet(stages)) { Debug.LogWarning($"[StageMaster] {e}"); errorCount++; }

            Debug.Log($"[StageMaster] 検証完了: {stages.Count} ステージ / 問題 {errorCount} 件");
        }
#endif

        static void CheckExactlyOne(List<StageTileData> tiles, TileType type, List<string> errors)
        {
            int count = 0;
            foreach (var t in tiles) if (t != null && t.tileType == type) count++;
            if (count != 1) errors.Add($"{type} はちょうど1つ必要です（現在 {count} 個）。");
        }

        // 4. WARP: 総数は偶数、groupIdは1〜4、各groupIdにちょうど2タイル
        static void ValidateWarp(List<StageTileData> tiles, List<string> errors)
        {
            var counts = new Dictionary<int, int>();
            int total = 0;
            foreach (var t in tiles)
            {
                if (t == null || t.tileType != TileType.WARP) continue;
                total++;
                if (t.groupId < 1 || t.groupId > GridConst.MaxWarpPairs)
                {
                    errors.Add($"({t.x},{t.y}) WARP: groupId {t.groupId} は 1〜{GridConst.MaxWarpPairs} の範囲外です。");
                    continue;
                }
                counts[t.groupId] = counts.TryGetValue(t.groupId, out var c) ? c + 1 : 1;
            }
            if (total % 2 != 0) errors.Add($"WARP の総数が奇数です（{total}）。");
            foreach (var kv in counts)
                if (kv.Value != 2) errors.Add($"WARP groupId {kv.Key} はちょうど2タイル必要です（現在 {kv.Value}）。");
        }

        // 5. ORDERED_CHECKPOINT: orderNo が 1..N の連番で重複なし
        static void ValidateOrderedCheckpoint(List<StageTileData> tiles, List<string> errors)
        {
            var orders = new List<int>();
            foreach (var t in tiles)
                if (t != null && t.tileType == TileType.ORDERED_CHECKPOINT) orders.Add(t.orderNo);
            if (orders.Count == 0) return;

            orders.Sort();
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i] != i + 1)
                {
                    errors.Add($"ORDERED_CHECKPOINT の orderNo は 1..{orders.Count} の連番（重複なし）である必要があります。");
                    return;
                }
            }
        }

        // 8, 9. STRAIGHT / TURN の方向指定
        static void ValidateStraightAndTurn(List<StageTileData> tiles, List<string> errors)
        {
            const Direction vertical = Direction.Up | Direction.Down;
            const Direction horizontal = Direction.Left | Direction.Right;
            foreach (var t in tiles)
            {
                if (t == null) continue;
                if (t.tileType == TileType.STRAIGHT && t.allowedDirections != vertical && t.allowedDirections != horizontal)
                    errors.Add($"({t.x},{t.y}) STRAIGHT: allowedDirections は Up|Down または Left|Right である必要があります。");
                if (t.tileType == TileType.TURN)
                {
                    CheckTurnMask(t, "entryDirections", t.entryDirections, errors);
                    CheckTurnMask(t, "allowedDirections", t.allowedDirections, errors);
                }
            }
        }

        static void CheckTurnMask(StageTileData t, string field, Direction mask, List<string> errors)
        {
            if (mask == Direction.None)
                errors.Add($"({t.x},{t.y}) TURN: {field} が None です。");
            else if (((int)mask & ~(int)DirectionExtensions.All) != 0)
                errors.Add($"({t.x},{t.y}) TURN: {field} に定義外のビットが含まれています（{(int)mask}）。");
        }

        // 6, 7. FORCE_MOVE の連鎖経路: 方向指定・BLOCK/外周への衝突禁止・循環禁止
        static void ValidateForceMove(List<StageTileData> tiles, Dictionary<Vector2Int, StageTileData> map, List<string> errors)
        {
            foreach (var start in tiles)
            {
                if (start == null || start.tileType != TileType.FORCE_MOVE) continue;
                if (!start.direction.IsSingle())
                {
                    errors.Add($"({start.x},{start.y}) FORCE_MOVE: direction は単一方向である必要があります。");
                    continue;
                }

                var visited = new HashSet<Vector2Int> { start.Position };
                var cur = start;
                while (true)
                {
                    var next = cur.Position + cur.direction.ToOffset();
                    if (next.x < 0 || next.x >= GridConst.Width || next.y < 0 || next.y >= GridConst.Height)
                    {
                        errors.Add($"({start.x},{start.y}) FORCE_MOVE の連鎖経路が外周ブロックに衝突します（({cur.x},{cur.y})→外周）。");
                        break;
                    }
                    if (!map.TryGetValue(next, out var nt)) break; // 床に到達して終了

                    if (nt.tileType == TileType.BLOCK)
                    {
                        errors.Add($"({start.x},{start.y}) FORCE_MOVE の連鎖経路上に BLOCK ({next.x},{next.y}) があります。");
                        break;
                    }
                    if (nt.tileType != TileType.FORCE_MOVE) break; // 連鎖はここで終了
                    if (!nt.direction.IsSingle()) break; // 当該タイル側で別途エラー

                    if (!visited.Add(next))
                    {
                        errors.Add($"({start.x},{start.y}) FORCE_MOVE の連鎖が循環しています。");
                        break;
                    }
                    cur = nt;
                }
            }
        }
    }
}
