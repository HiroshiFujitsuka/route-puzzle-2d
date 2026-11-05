using System;
using System.Collections;
using RoutePuzzle.Master;
using UnityEngine;

namespace RoutePuzzle.Puzzle
{
    /// <summary>
    /// プレイヤーの移動制御と演出ステート管理。キーボード・画面ボタンのどちらからも MoveBy() を呼ぶ。
    /// 移動ルールそのものは StageState が持ち、ここでは結果の再生（アニメーション・イベント通知）を行う。
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] TileManager tileManager;
        [SerializeField, Tooltip("1マス移動にかかる秒数")] float stepDuration = 0.12f;
        [SerializeField, Tooltip("消滅演出の秒数")] float dieDuration = 0.4f;
        [SerializeField, Tooltip("クリア演出の秒数")] float clearDuration = 0.6f;
        [SerializeField, Tooltip("プレイヤーの表示色（仮素材）")] Color playerColor = new Color(0.2f, 0.45f, 0.95f);

        StageMaster currentStage;
        SpriteRenderer body;
        Coroutine playing;

        /// <summary>現在の演出ステート。Playing以外では入力を受け付けない。</summary>
        public PuzzlePhase Phase { get; private set; } = PuzzlePhase.Cleared;

        /// <summary>現在のステージ状態。Initialize前はnull。</summary>
        public StageState State { get; private set; }

        /// <summary>入力を受け付けない状態か。</summary>
        public bool InputLocked => Phase != PuzzlePhase.Playing;

        /// <summary>進入不可方向への入力があった（衝突音の再生契機）。</summary>
        public event Action Collided;

        /// <summary>ゴールがアンロックされた（アンロック演出・SEの契機）。</summary>
        public event Action GoalUnlocked;

        /// <summary>プレイヤーが消滅した（消滅演出・SEの契機）。演出後に自動でリセットされる。</summary>
        public event Action Died;

        /// <summary>クリア演出が終わった（リザルトUIを出す契機）。</summary>
        public event Action Cleared;

        /// <summary>詰み状態が変化した（リセットボタン強調の開始/停止）。引数は詰みかどうか。</summary>
        public event Action<bool> StuckChanged;

        /// <summary>ステージをセットアップして開始する。</summary>
        public void Initialize(StageMaster stage)
        {
            currentStage = stage;
            State = new StageState(stage);
            tileManager.Build(stage);
            EnsureBody();
            ApplyStartState();
        }

        /// <summary>ステージ開始時の状態に戻す（リセットボタン用）。移動・演出中は無視する。</summary>
        public void ResetStage()
        {
            if (State == null || Phase == PuzzlePhase.Moving || Phase == PuzzlePhase.Dying || Phase == PuzzlePhase.Clearing)
                return;
            ApplyStartState();
        }

        /// <summary>1マス分の移動入力。キーボード・画面ボタン共通の入口。</summary>
        public void MoveBy(Direction direction)
        {
            if (State == null || InputLocked) return;

            var outcome = State.TryMove(direction);
            if (outcome.Result == MoveResult.Blocked)
            {
                Collided?.Invoke();
                return;
            }
            playing = StartCoroutine(Play(outcome));
        }

        void EnsureBody()
        {
            if (body != null) return;
            var go = new GameObject("PlayerBody");
            go.transform.SetParent(transform, false);
            body = go.AddComponent<SpriteRenderer>();
            body.sprite = SpriteFactory.White;
            body.color = playerColor;
            body.sortingOrder = 5;
        }

        void ApplyStartState()
        {
            if (playing != null) { StopCoroutine(playing); playing = null; }
            State.Reset();
            body.transform.localScale = Vector3.one * 0.6f;
            transform.position = tileManager.CellToWorld(State.PlayerPosition);
            tileManager.Refresh(State);
            Phase = PuzzlePhase.Playing;
            StuckChanged?.Invoke(State.IsStuck);
        }

        IEnumerator Play(MoveOutcome outcome)
        {
            Phase = PuzzlePhase.Moving;

            var prev = transform.position;
            Vector2Int? prevCell = null;
            foreach (var cell in outcome.Path)
            {
                var target = tileManager.CellToWorld(cell);
                bool adjacent = prevCell.HasValue && (cell - prevCell.Value).sqrMagnitude == 1;
                // ワープの着地は瞬間移動、それ以外は1マスずつスライド。最初の1歩は現在位置から滑らせる。
                if (prevCell == null || adjacent) yield return Slide(prev, target);
                else transform.position = target;
                prev = target;
                prevCell = cell;
            }

            // State は入力時点で最終状態まで進んでいる。途中で Refresh すると後続のマスが先に通過済み表示になるため、
            // 経路の再生が終わってから盤面へ反映する。
            tileManager.Refresh(State);

            if (outcome.GoalUnlockedNow) GoalUnlocked?.Invoke();

            switch (outcome.Result)
            {
                case MoveResult.Died:
                    yield return Die();
                    break;
                case MoveResult.Cleared:
                    yield return Clear();
                    break;
                default:
                    Phase = PuzzlePhase.Playing;
                    if (outcome.IsStuck) StuckChanged?.Invoke(true);
                    break;
            }
            playing = null;
        }

        IEnumerator Slide(Vector3 from, Vector3 to)
        {
            for (float t = 0f; t < stepDuration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, t / stepDuration);
                yield return null;
            }
            transform.position = to;
        }

        IEnumerator Die()
        {
            Phase = PuzzlePhase.Dying;
            Died?.Invoke();
            var start = body.transform.localScale;
            for (float t = 0f; t < dieDuration; t += Time.deltaTime)
            {
                body.transform.localScale = Vector3.Lerp(start, Vector3.zero, t / dieDuration);
                yield return null;
            }
            body.transform.localScale = Vector3.zero;
            playing = null;
            ApplyStartState();
        }

        IEnumerator Clear()
        {
            Phase = PuzzlePhase.Clearing;
            var start = body.transform.localScale;
            for (float t = 0f; t < clearDuration; t += Time.deltaTime)
            {
                float k = t / clearDuration;
                body.transform.localScale = start * (1f + 0.5f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            body.transform.localScale = start;
            Phase = PuzzlePhase.Cleared;
            Cleared?.Invoke();
        }
    }
}
