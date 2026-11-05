using RoutePuzzle.Master;
using UnityEngine;

namespace RoutePuzzle.Puzzle
{
    /// <summary>画面サイズ（縦長・横長）に合わせて、外周ブロックを含む盤面全体が収まるようにカメラを調整する。</summary>
    [RequireComponent(typeof(Camera))]
    public class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField, Tooltip("盤面の周囲に空ける余白（ユニット）")] float margin = 0.75f;
        [SerializeField, Range(0f, 0.5f), Tooltip("縦長画面で盤面を上へ寄せる量（画面下部の操作ボタン用の余白。視野の高さに対する割合）")]
        float portraitUpShift = 0.2f;

        Camera cam;
        int lastWidth, lastHeight;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Fit();
        }

        /// <summary>外周を含む盤面（幅+2, 高さ+2）に余白を足した範囲がすべて映る大きさにする。</summary>
        void Fit()
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float halfW = (GridConst.Width + 2) * 0.5f + margin;
            float halfH = (GridConst.Height + 2) * 0.5f + margin;

            // 縦長のときだけ盤面を上へ寄せ、下に操作ボタンの領域を確保する（横長は寄せない）。
            // 寄せる量は視野の高さに対する割合。0.5以上だと上端が必ず見切れるため未満に制限する。
            float k = aspect < 1f ? Mathf.Clamp(portraitUpShift, 0f, 0.49f) : 0f;

            // カメラを下へ size*2*k ずらすと上端は size*(1-2k) まで下がる。上辺が見えるよう縦方向の必要サイズに含める。
            cam.orthographicSize = Mathf.Max(halfH / (1f - 2f * k), halfW / aspect);

            var pos = transform.position;
            transform.position = new Vector3(0f, -cam.orthographicSize * 2f * k, pos.z);
        }
    }
}
