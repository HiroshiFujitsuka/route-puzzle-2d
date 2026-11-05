using RoutePuzzle.Master;
using UnityEngine;

namespace RoutePuzzle.Puzzle
{
    /// <summary>シーン開始時に、指定したステージで PlayerController を初期化する（ステージ選択画面の実装までの暫定）。</summary>
    public class StageBootstrap : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] StageMaster stage;

        void Start()
        {
            if (player == null || stage == null)
            {
                Debug.LogError("[StageBootstrap] player または stage が未設定です。", this);
                return;
            }

            var errors = StageMasterValidator.Validate(stage);
            foreach (var e in errors) Debug.LogWarning($"[StageBootstrap] {stage.name}: {e}", stage);

            // 不正なマスターで開始すると StageState が破綻するため、エラーがあれば初期化しない。
            if (errors.Count > 0)
            {
                Debug.LogError($"[StageBootstrap] {stage.name}: 検証エラーが {errors.Count} 件あるため開始しません。", stage);
                return;
            }

            player.Initialize(stage);
        }
    }
}
