using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoutePuzzle.Master
{
    /// <summary>M_Difficulty の1行。</summary>
    [Serializable]
    public class DifficultyData
    {
        [Tooltip("1=Easy / 2=Normal / 3=Hard")]
        public int difficultyId;

        public string displayName;

        [Tooltip("解放に必要な『全ステージクリア済み』の難易度ID。0=常時解放（Easy）")]
        public int unlockRequiredDifficultyId;

        public bool IsAlwaysUnlocked => unlockRequiredDifficultyId == 0;
    }

    /// <summary>M_Difficulty: 全難易度を1アセットに保持する。</summary>
    [CreateAssetMenu(fileName = "DifficultyMaster", menuName = "RoutePuzzle/Master/Difficulty")]
    public class DifficultyMaster : ScriptableObject
    {
        public List<DifficultyData> difficulties = new List<DifficultyData>();

        public DifficultyData Find(int difficultyId)
            => difficulties.Find(d => d.difficultyId == difficultyId);

#if UNITY_EDITOR
        void OnValidate()
        {
            var seen = new HashSet<int>();
            foreach (var d in difficulties)
            {
                if (!seen.Add(d.difficultyId))
                    Debug.LogWarning($"[DifficultyMaster] difficultyId {d.difficultyId} が重複しています。", this);
                if (d.unlockRequiredDifficultyId != 0 && Find(d.unlockRequiredDifficultyId) == null)
                    Debug.LogWarning($"[DifficultyMaster] difficultyId {d.difficultyId} の unlockRequiredDifficultyId {d.unlockRequiredDifficultyId} が存在しません。", this);
            }
        }
#endif
    }
}
