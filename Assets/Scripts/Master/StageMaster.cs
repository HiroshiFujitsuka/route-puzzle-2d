using System.Collections.Generic;
using UnityEngine;

namespace RoutePuzzle.Master
{
    /// <summary>M_Stage: 1ステージ1アセット。M_StageTile（tiles）も内包する。</summary>
    [CreateAssetMenu(fileName = "Stage_", menuName = "RoutePuzzle/Master/Stage")]
    public class StageMaster : ScriptableObject
    {
        [Tooltip("全体で一意。セーブデータのクリア記録のキー")]
        public int stageId;

        [Tooltip("所属難易度（M_Difficulty.difficultyId）")]
        public int difficultyId;

        [Tooltip("難易度内の番号（1始まり）。(difficultyId, stageNo)は一意")]
        public int stageNo;

        public string title;

        [Tooltip("特殊タイルの配置。含まれない座標は床")]
        public List<StageTileData> tiles = new List<StageTileData>();

#if UNITY_EDITOR
        /// <summary>エディタ上でマスターの検証エラーを警告として表示する。</summary>
        void OnValidate()
        {
            var errors = StageMasterValidator.Validate(this);
            foreach (var e in errors) Debug.LogWarning($"[StageMaster] {name}: {e}", this);
        }
#endif
    }
}
