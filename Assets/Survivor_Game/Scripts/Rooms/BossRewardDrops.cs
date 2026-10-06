using System.Collections.Generic;

/// <summary>보스별 확정 드롭은 방의 공통 드롭·선택 보상을 대체하지 않는다.</summary>
public static class BossRewardDrops
{
    public static IReadOnlyList<RoomRewardDefinition> Build(
        IReadOnlyList<RoomRewardDefinition> common, RoomRewardDefinition signature)
    {
        if (signature == null) return common ?? System.Array.Empty<RoomRewardDefinition>();
        var result = new List<RoomRewardDefinition>();
        if (common != null) result.AddRange(common);
        result.Add(signature);
        return result;
    }
}
