using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>실제 회로 계산의 전후 결과를 읽는다. 플레이어 스탯/아이템 효과를 실행하지 않는다.</summary>
public static class BoardPreviewPresentation
{
    public static string Describe(CoreBoardState before, CoreBoardState after,
        ChipDefinition chip, PlacedChip proposed)
    {
        var text = new StringBuilder(chip.DisplayName);
        text.AppendLine(proposed == null ? " · 제거 미리보기" : " · 배치 미리보기");
        if (proposed != null)
        {
            bool active = after.IsEnergized(proposed);
            text.AppendLine(active ? "활성 · 효과 배율 ×" + after.Solution.GetMultiplier(proposed.Id).ToString("0.00")
                : "전원 미연결 · 효과 없음");
        }
        text.Append("완성 회로 ").Append(before.Solution.Circuits.Count).Append(" → ")
            .Append(after.Solution.Circuits.Count).AppendLine();
        text.AppendLine("\n보드 보너스 변화");
        text.AppendLine("기본·장비 제외 / 최종 반올림 전");
        bool changed = false;
        foreach (ChipStatKind kind in Enum.GetValues(typeof(ChipStatKind)))
        {
            float oldValue = before.Stats.Get(kind), newValue = after.Stats.Get(kind);
            if (Mathf.Approximately(oldValue, newValue)) continue;
            changed = true;
            string color = newValue > oldValue ? "#80EFAD" : "#FF9797";
            text.Append(MetaEffectFormat.StatName(kind)).Append(' ')
                .Append(FormatValue(kind, oldValue)).Append(" → ")
                .Append(FormatValue(kind, newValue)).Append(" <color=").Append(color)
                .Append(">(").Append(FormatValue(kind, newValue - oldValue)).AppendLine(")</color>");
        }
        if (!changed) text.AppendLine("능력치 변화 없음");

        foreach (PlacedChip old in before.Placements)
        {
            PlacedChip next = after.GetChip(old.Id);
            bool wasActive = before.IsEnergized(old), isActive = next != null && after.IsEnergized(next);
            if (wasActive == isActive) continue;
            text.Append(isActive ? "활성화: " : "비활성화: ").AppendLine(old.Chip.DisplayName);
        }

        HashSet<ChipDefinition> previousEffects = ActiveEffects(before);
        HashSet<ChipDefinition> nextEffects = ActiveEffects(after);
        foreach (ChipDefinition effect in nextEffects)
            if (!previousEffects.Contains(effect))
                text.Append("반응 효과 추가: ").Append(effect.DisplayName).Append(" · ").AppendLine(effect.ReactiveEffect.Describe());
        foreach (ChipDefinition effect in previousEffects)
            if (!nextEffects.Contains(effect))
                text.Append("반응 효과 해제: ").Append(effect.DisplayName).Append(" · ").AppendLine(effect.ReactiveEffect.Describe());
        text.Append("\n반응 효과는 동일 칩 중복·회로 증폭 제외\n휠로 설명 스크롤\n놓으면 적용 · 취소하려면 Esc/Tab");
        return text.ToString();
    }

    private static string FormatValue(ChipStatKind kind, float value)
    {
        bool rate = MetaEffectFormat.IsRate(kind);
        return (value >= 0f ? "+" : "") + (rate ? value * 100f : value).ToString("0.##") + (rate ? "%" : "");
    }

    private static HashSet<ChipDefinition> ActiveEffects(CoreBoardState state)
    {
        var effects = new HashSet<ChipDefinition>();
        foreach (PlacedChip placed in state.Placements)
            if (placed.Chip != null && state.IsEnergized(placed) &&
                placed.Chip.ReactiveEffect != null && placed.Chip.ReactiveEffect.IsConfigured)
                effects.Add(placed.Chip);
        return effects;
    }
}
