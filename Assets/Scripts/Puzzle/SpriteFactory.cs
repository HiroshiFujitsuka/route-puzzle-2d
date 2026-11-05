using UnityEngine;

namespace RoutePuzzle.Puzzle
{
    /// <summary>仮素材用。実行時に1×1ユニットの白いスプライトを生成する（色はSpriteRendererで付ける）。</summary>
    public static class SpriteFactory
    {
        static Sprite white;

        /// <summary>1×1ユニットの白い正方形スプライト。</summary>
        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return white;
            }
        }
    }
}
