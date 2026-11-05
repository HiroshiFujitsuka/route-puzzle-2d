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
        [SerializeField, Tooltip("横長画面で、盤面と重ならないように避ける操作ボタン領域（画面左下に置く想定）。未設定なら避けない")]
        RectTransform buttonArea;
        [SerializeField, Min(0f), Tooltip("横長画面で、ボタン領域と盤面の間に空ける画面ピクセル数")]
        float buttonGapPixels = 16f;

        Camera cam;
        int lastWidth, lastHeight;

        /// <summary>Camera を取得し、正射影に設定する。</summary>
        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        /// <summary>画面サイズが変わったときだけカメラを再調整する。</summary>
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
            float halfW = (GridConst.Width + 2) * 0.5f + margin;
            float halfH = (GridConst.Height + 2) * 0.5f + margin;
            Calculate(Screen.width, Screen.height, halfW, halfH, portraitUpShift, ReservedLeftPixels(),
                out float size, out var offset);

            cam.orthographicSize = size;
            var pos = transform.position;
            transform.position = new Vector3(offset.x, offset.y, pos.z);
        }

        /// <summary>buttonArea の右端までの画面ピクセル数（+隙間）。横長で盤面を右へ避けるために使う。未設定なら0。</summary>
        float ReservedLeftPixels()
        {
            if (buttonArea == null) return 0f;
            Canvas.ForceUpdateCanvases();
            var canvas = buttonArea.GetComponentInParent<Canvas>();
            var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            var corners = new Vector3[4];
            buttonArea.GetWorldCorners(corners);
            float right = 0f;
            foreach (var c in corners)
                right = Mathf.Max(right, RectTransformUtility.WorldToScreenPoint(uiCamera, c).x);
            return right + buttonGapPixels;
        }

        /// <summary>
        /// 画面サイズから、カメラの大きさ(size)と位置(offset)を求める。
        /// 縦長: 盤面を上へ寄せる（視野の高さ×shiftRatio）。寄せる分は縦方向の必要サイズに含める。
        /// 横長: 左側 reservedLeftPixels を操作ボタン用に空け、盤面が残りの領域に収まるようにする。
        /// </summary>
        public static void Calculate(int screenW, int screenH, float halfW, float halfH, float shiftRatio,
            float reservedLeftPixels, out float size, out Vector2 offset)
        {
            float aspect = (float)screenW / Mathf.Max(1, screenH);

            if (aspect < 1f)
            {
                // 寄せる量は視野の高さに対する割合。0.5以上だと上端が必ず見切れるため未満に制限する。
                float k = Mathf.Clamp(shiftRatio, 0f, 0.49f);
                // カメラを下へ size*2*k ずらすと上端は size*(1-2k) まで下がる。上辺が見えるよう必要サイズに含める。
                size = Mathf.Max(halfH / (1f - 2f * k), halfW / aspect);
                offset = new Vector2(0f, -size * 2f * k);
                return;
            }

            // 予約領域が画面の半分を超えないようにして、盤面を映せなくなるのを防ぐ。
            float reserved = Mathf.Clamp(reservedLeftPixels, 0f, screenW * 0.5f);
            float usableW = Mathf.Max(1f, screenW - reserved);
            // 中央に置いたままで盤面の左端が予約領域に掛からないなら、寄せずにそのまま映す（横に十分広い画面）。
            float centeredLeft = screenW * 0.5f - halfW * screenH / (2f * halfH);
            if (centeredLeft >= reserved)
            {
                size = halfH;
                offset = Vector2.zero;
                return;
            }

            // 残りの領域（幅 usableW px）に盤面の幅（2*halfW ユニット）が収まる大きさ。
            size = Mathf.Max(halfH, halfW * screenH / usableW);
            // 残りの領域の中心が盤面の中心に来るよう、カメラを左へずらす（盤面は右へ寄る）。
            float unitsPerPixel = 2f * size / screenH;
            offset = new Vector2(-reserved * 0.5f * unitsPerPixel, 0f);
        }
    }
}
