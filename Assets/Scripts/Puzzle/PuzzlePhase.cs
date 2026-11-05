namespace RoutePuzzle.Puzzle
{
    /// <summary>パズル画面の演出ステート。Playing以外の間は、キーボード・画面ボタンのどちらの入力も受け付けない。</summary>
    public enum PuzzlePhase
    {
        /// <summary>入力受付中。</summary>
        Playing,

        /// <summary>移動アニメーション再生中。</summary>
        Moving,

        /// <summary>プレイヤーの消滅演出中。終了後に自動でリセットされる。</summary>
        Dying,

        /// <summary>クリア演出中。</summary>
        Clearing,

        /// <summary>クリア演出終了。リザルトUIの操作待ち。</summary>
        Cleared,
    }
}
