using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 층에서 방 종류별로 어떤 보상이 몇 개 나올지 정한다.
/// 보상 개수가 2 이상이면 그만큼 받침대가 놓이고, 플레이어는 그중 하나만 가져갈 수 있다.
/// </summary>
[Serializable]
public class FloorRewardPlan
{
    [Serializable]
    public class Entry
    {
        public RoomRewardDefinition reward;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public class Pool
    {
        public RoomKind kind = RoomKind.Treasure;
        [Tooltip("이 방에 놓을 받침대 수. 배타적인 방이면 그중 하나만 고를 수 있다.")]
        [Min(0)] public int choiceCount = 3;
        [Tooltip("켜면 값을 치러야 한다(상점). 끄면 공짜 보상이며 하나만 고를 수 있다.")]
        public bool purchasable;
        [Tooltip("상점일 때 보상 기본 가격에 곱하는 층 배율.")]
        [Min(0.1f)] public float priceMultiplier = 1f;
        [Tooltip("받침대에 놓여 플레이어가 고르는 보상들.")]
        public Entry[] rewards = Array.Empty<Entry>();
        [Tooltip("방을 클리어하면 바닥에 떨어지는 보상들. 받침대와 달리 고르는 것이 아니라 " +
            "밟으면 들어오고 지나치면 그만이다. 체력 회복처럼 선택지 한 칸을 " +
            "잡아먹으면 아까운 것을 여기에 둔다.")]
        public RoomRewardDefinition[] dropRewards = Array.Empty<RoomRewardDefinition>();
    }

    [SerializeField] private Pool[] pools = Array.Empty<Pool>();

    public int ResolveChoiceCount(RoomKind kind)
    {
        Pool pool = FindPool(kind);
        return pool != null ? Mathf.Max(0, pool.choiceCount) : 0;
    }

    /// <summary>방을 클리어할 때 바닥에 떨어지는 보상들.</summary>
    public IReadOnlyList<RoomRewardDefinition> GetDrops(RoomKind kind)
    {
        Pool pool = FindPool(kind);
        return pool != null && pool.dropRewards != null
            ? pool.dropRewards
            : Array.Empty<RoomRewardDefinition>();
    }

    /// <summary>이 방의 보상이 유료인가. 유료 방은 여러 개를 살 수 있다.</summary>
    public bool IsPurchasable(RoomKind kind)
    {
        Pool pool = FindPool(kind);
        return pool != null && pool.purchasable;
    }

    /// <summary>이 방에서 해당 보상에 매길 가격. 무료 방이면 0.</summary>
    public int ResolvePrice(RoomKind kind, RoomRewardDefinition reward)
    {
        Pool pool = FindPool(kind);
        if (pool == null || !pool.purchasable || reward == null) return 0;
        return Mathf.Max(1,
            Mathf.RoundToInt(reward.ShopPrice * Mathf.Max(0.1f, pool.priceMultiplier)));
    }

    /// <summary>
    /// 해당 방 종류의 보상을 하나 뽑는다. exclude에 든 보상은 피해서
    /// 같은 방에 똑같은 선택지가 나란히 놓이지 않게 한다.
    /// </summary>
    public RoomRewardDefinition Pick(
        RoomKind kind, IList<RoomRewardDefinition> exclude)
    {
        Pool pool = FindPool(kind);
        if (pool == null || pool.rewards == null) return null;

        RoomRewardDefinition picked = PickWeighted(pool.rewards, exclude);
        // 남은 후보가 없으면 중복을 허용해서라도 빈 받침대를 만들지 않는다.
        return picked != null ? picked : PickWeighted(pool.rewards, null);
    }

    private Pool FindPool(RoomKind kind)
    {
        if (pools == null) return null;
        foreach (Pool pool in pools)
        {
            if (pool != null && pool.kind == kind) return pool;
        }
        return null;
    }

    private static RoomRewardDefinition PickWeighted(
        Entry[] entries, IList<RoomRewardDefinition> exclude)
    {
        float total = 0f;
        foreach (Entry entry in entries)
        {
            if (!IsUsable(entry, exclude)) continue;
            total += Mathf.Max(0f, entry.weight);
        }
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.value * total;
        foreach (Entry entry in entries)
        {
            if (!IsUsable(entry, exclude)) continue;
            roll -= Mathf.Max(0f, entry.weight);
            if (roll <= 0f) return entry.reward;
        }
        return null;
    }

    private static bool IsUsable(Entry entry, IList<RoomRewardDefinition> exclude)
    {
        if (entry == null || entry.reward == null) return false;
        return exclude == null || !exclude.Contains(entry.reward);
    }

    public void Normalize()
    {
        if (pools == null) return;
        foreach (Pool pool in pools)
        {
            if (pool == null) continue;
            pool.choiceCount = Mathf.Max(0, pool.choiceCount);
            pool.priceMultiplier = Mathf.Max(0.1f, pool.priceMultiplier);
        }
    }
}
