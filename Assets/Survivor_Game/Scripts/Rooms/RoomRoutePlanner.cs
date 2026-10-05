using System.Collections.Generic;
using UnityEngine;

/// <summary>한 번 제시된 목적지. 창을 닫았다 다시 열어도 같은 후보를 사용한다.</summary>
public sealed class RoomRouteChoice
{
    public RoomInstance Prefab { get; }
    public RoomKind Kind => Prefab.Kind;

    public RoomRouteChoice(RoomInstance prefab) { Prefab = prefab; }
}

/// <summary>기존 방 프리팹 풀에서 다음 방의 종류와 디자인을 고른다.</summary>
public static class RoomRoutePlanner
{
    private static readonly RoomKind[] OrdinaryKinds =
        { RoomKind.Normal, RoomKind.Elite, RoomKind.Treasure, RoomKind.Shop };

    public static List<RoomRouteChoice> CreateChoices(FloorProfile profile, int stageNumber,
        string previousDesign, int minimumChoices, int maximumChoices, int firstEliteStage)
    {
        var choices = new List<RoomRouteChoice>();
        if (profile == null || profile.IsBossStage) return choices;
        foreach (RoomKind kind in OrdinaryKinds)
        {
            if (kind == RoomKind.Elite && stageNumber < Mathf.Max(3, firstEliteStage)) continue;
            RoomInstance prefab = PickPrefab(profile, kind, previousDesign);
            if (prefab != null) choices.Add(new RoomRouteChoice(prefab));
        }
        for (int i = choices.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            RoomRouteChoice swap = choices[i];
            choices[i] = choices[other];
            choices[other] = swap;
        }
        int min = Mathf.Clamp(minimumChoices, 2, 3);
        int max = Mathf.Clamp(maximumChoices, min, 3);
        int count = Mathf.Min(choices.Count, Random.Range(min, max + 1));
        if (count < choices.Count) choices.RemoveRange(count, choices.Count - count);
        return choices;
    }

    public static RoomInstance PickPrefab(FloorProfile profile, RoomKind kind, string previousDesign)
    {
        if (profile == null) return null;
        List<RoomInstance> candidates = profile.GetRoomPrefabs(kind);
        candidates.RemoveAll(room => string.Equals(room.DesignId, previousDesign,
            System.StringComparison.Ordinal));
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    public static string GetTitle(RoomKind kind)
    {
        switch (kind)
        {
            case RoomKind.Normal: return "일반 전투";
            case RoomKind.Elite: return "엘리트";
            case RoomKind.Treasure: return "보물";
            case RoomKind.Shop: return "상점";
            case RoomKind.Boss: return "보스";
            default: return "시작";
        }
    }

    public static string GetDescription(RoomKind kind)
    {
        switch (kind)
        {
            case RoomKind.Normal: return "적을 처치하고\n경험치와 재화 획득";
            case RoomKind.Elite: return "강한 적과 전투\n승리 후 보상 선택";
            case RoomKind.Treasure: return "전투 없이\n무료 보상 하나 선택";
            case RoomKind.Shop: return "모은 재화로\n아이템 구매";
            default: return "보스를 선택하고 전투";
        }
    }
}
