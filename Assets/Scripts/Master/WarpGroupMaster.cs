using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoutePuzzle.Master
{
    /// <summary>M_WarpGroup の1行。</summary>
    [Serializable]
    public class WarpGroupData
    {
        [Tooltip("1〜4")]
        public int groupId;

        [Tooltip("対のワープ両端に共通で適用する表示色。グループ間で重複させない")]
        public Color color = Color.white;
    }

    /// <summary>M_WarpGroup: 全ワープグループを1アセットに保持する。</summary>
    [CreateAssetMenu(fileName = "WarpGroupMaster", menuName = "RoutePuzzle/Master/WarpGroup")]
    public class WarpGroupMaster : ScriptableObject
    {
        public List<WarpGroupData> groups = new List<WarpGroupData>();

        public WarpGroupData Find(int groupId) => groups.Find(g => g.groupId == groupId);

#if UNITY_EDITOR
        /// <summary>エディタ上でのグループID重複・範囲外を警告する。</summary>
        void OnValidate()
        {
            if (groups.Count > GridConst.MaxWarpPairs)
                Debug.LogWarning($"[WarpGroupMaster] グループ数は最大 {GridConst.MaxWarpPairs} です。", this);

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].groupId < 1 || groups[i].groupId > GridConst.MaxWarpPairs)
                    Debug.LogWarning($"[WarpGroupMaster] groupId {groups[i].groupId} は 1〜{GridConst.MaxWarpPairs} の範囲外です。", this);
                for (int j = i + 1; j < groups.Count; j++)
                {
                    if (groups[i].groupId == groups[j].groupId)
                        Debug.LogWarning($"[WarpGroupMaster] groupId {groups[i].groupId} が重複しています。", this);
                    if (groups[i].color == groups[j].color)
                        Debug.LogWarning($"[WarpGroupMaster] groupId {groups[i].groupId} と {groups[j].groupId} の色が重複しています。", this);
                }
            }
        }
#endif
    }
}
