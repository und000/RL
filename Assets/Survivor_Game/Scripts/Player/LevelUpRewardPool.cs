using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>레벨업에서 직접 지급할 아이템 목록. 기존 방 보상 자산을 재사용한다.</summary>
[CreateAssetMenu(fileName = "LevelUpRewards", menuName = "Survivor/Rewards/Level Up Items")]
public class LevelUpRewardPool : ScriptableObject
{
    [SerializeField] private RoomRewardDefinition[] items = Array.Empty<RoomRewardDefinition>();

    public List<RoomRewardDefinition> GetEligible(Predicate<RoomRewardDefinition> allowed)
    {
        var result = new List<RoomRewardDefinition>();
        if (items == null) return result;
        foreach (RoomRewardDefinition item in items)
        {
            // 무기 교체나 경험치에 의한 연쇄 레벨업은 이 아이템 선택에 포함하지 않는다.
            if (item == null || (item.Kind != RoomRewardKind.Chip && item.Kind != RoomRewardKind.Equipment) ||
                result.Contains(item) || (allowed != null && !allowed(item))) continue;
            result.Add(item);
        }
        return result;
    }
}

/// <summary>한 런의 선택지와 공유 리롤 예산. 레벨업마다 다시 만들지 않는다.</summary>
public sealed class LevelUpRewardDraft
{
    public const int ChoiceCount = 4;
    private readonly List<RoomRewardDefinition> choices = new List<RoomRewardDefinition>();
    public IReadOnlyList<RoomRewardDefinition> Choices => choices;
    public int RemainingRerolls { get; private set; }

    public LevelUpRewardDraft(int rerolls) { RemainingRerolls = Mathf.Max(0, rerolls); }

    public bool Refresh(List<RoomRewardDefinition> eligible, bool spendReroll)
    {
        if (eligible == null || eligible.Count < ChoiceCount ||
            (spendReroll && (RemainingRerolls <= 0 || !HasAlternative(eligible)))) return false;
        var fresh = new List<RoomRewardDefinition>();
        var previous = new List<RoomRewardDefinition>();
        foreach (RoomRewardDefinition item in eligible)
        {
            if (item == null || fresh.Contains(item) || previous.Contains(item)) continue;
            if (spendReroll && choices.Contains(item)) previous.Add(item);
            else fresh.Add(item);
        }
        Shuffle(fresh);
        Shuffle(previous);
        fresh.AddRange(previous);
        if (fresh.Count < ChoiceCount) return false;
        choices.Clear();
        choices.AddRange(fresh.GetRange(0, ChoiceCount));
        if (spendReroll) RemainingRerolls--;
        return true;
    }

    public bool HasAlternative(List<RoomRewardDefinition> eligible)
    {
        if (eligible == null) return false;
        foreach (RoomRewardDefinition item in eligible)
            if (item != null && !choices.Contains(item)) return true;
        return false;
    }

    private static void Shuffle(List<RoomRewardDefinition> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            RoomRewardDefinition temp = list[i]; list[i] = list[j]; list[j] = temp;
        }
    }
}
