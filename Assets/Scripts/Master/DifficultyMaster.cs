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
                else if (HasUnlockCycle(d))
                    Debug.LogWarning($"[DifficultyMaster] difficultyId {d.difficultyId} の unlockRequiredDifficultyId が循環（自己参照を含む）しています。", this);
            }
        }

        /// <summary>解放条件の鎖をたどり、start 自身に戻るか（自己参照・循環）を判定する。</summary>
        bool HasUnlockCycle(DifficultyData start)
        {
            var visited = new HashSet<int> { start.difficultyId };
            var cur = start;
            while (cur.unlockRequiredDifficultyId != 0)
            {
                var next = Find(cur.unlockRequiredDifficultyId);
                if (next == null) return false; // 存在しないIDは別途警告
                if (next.difficultyId == start.difficultyId) return true;
                if (!visited.Add(next.difficultyId)) return false; // start を含まない循環。構成要素側で警告される
                cur = next;
            }
            return false;
        }
#endif
    }
}
