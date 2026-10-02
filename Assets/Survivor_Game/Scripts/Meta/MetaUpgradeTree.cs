using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영구 개조 항목을 모아 둔 표. 화면에 뜨는 순서도 여기서 정해진다.
/// </summary>
[CreateAssetMenu(
    fileName = "MetaUpgradeTree", menuName = "Survivor/Meta/Upgrade Tree")]
public class MetaUpgradeTree : ScriptableObject
{
    [Tooltip("개조 항목들. Order가 작은 것부터 화면에 뜬다.")]
    [SerializeField] private MetaUpgradeNode[] nodes = Array.Empty<MetaUpgradeNode>();

    private List<MetaUpgradeNode> sorted;

    /// <summary>Order 순으로 정렬한 목록. 빈 칸은 걸러 낸다.</summary>
    public IReadOnlyList<MetaUpgradeNode> Nodes
    {
        get
        {
            if (sorted != null) return sorted;

            sorted = new List<MetaUpgradeNode>();
            if (nodes != null)
            {
                foreach (MetaUpgradeNode node in nodes)
                {
                    if (node != null) sorted.Add(node);
                }
                sorted.Sort(CompareOrder);
            }
            return sorted;
        }
    }

    private static int CompareOrder(MetaUpgradeNode left, MetaUpgradeNode right)
    {
        int byOrder = left.Order.CompareTo(right.Order);
        return byOrder != 0
            ? byOrder
            : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    public MetaUpgradeNode Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (MetaUpgradeNode node in Nodes)
        {
            if (node.Id == id) return node;
        }
        return null;
    }

    private void OnValidate()
    {
        // 인스펙터에서 목록을 고치면 정렬을 다시 만들어야 한다.
        sorted = null;
    }
}
