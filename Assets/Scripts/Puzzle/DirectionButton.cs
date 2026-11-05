using RoutePuzzle.Master;
using UnityEngine;
using UnityEngine.UI;

namespace RoutePuzzle.Puzzle
{
    /// <summary>画面上の上下左右ボタン（スマホ向け入力）。タップで PlayerController.MoveBy() を呼ぶ。</summary>
    [RequireComponent(typeof(Button))]
    public class DirectionButton : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] Direction direction = Direction.Up;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        /// <summary>登録したクリックリスナーを解除する。</summary>
        void OnDestroy()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        /// <summary>入力ロック中の判定は PlayerController.MoveBy() 側で行う。</summary>
        void OnClick()
        {
            if (player != null) player.MoveBy(direction);
        }
    }
}
