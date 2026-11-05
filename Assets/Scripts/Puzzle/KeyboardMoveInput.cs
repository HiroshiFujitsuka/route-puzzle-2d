using RoutePuzzle.Master;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoutePuzzle.Puzzle
{
    /// <summary>W/A/S/Dキーを PlayerController.MoveBy() に橋渡しする（PC向け入力）。</summary>
    public class KeyboardMoveInput : MonoBehaviour
    {
        [SerializeField] PlayerController player;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || player == null) return;

            if (kb.wKey.wasPressedThisFrame) player.MoveBy(Direction.Up);
            else if (kb.dKey.wasPressedThisFrame) player.MoveBy(Direction.Right);
            else if (kb.sKey.wasPressedThisFrame) player.MoveBy(Direction.Down);
            else if (kb.aKey.wasPressedThisFrame) player.MoveBy(Direction.Left);
        }
    }
}
