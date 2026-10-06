using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>실제 적용 데이터로 효과와 교체 손익을 설명한다. 표시 때문에 보상을 지급하거나 보드를 변경하지 않는다.</summary>
public static class RewardPresentation
{
    public static void RenderIcon(Image image, RoomRewardDefinition reward)
    {
        for (int i = image.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = image.transform.GetChild(i);
            if (child.name != "RewardPreview") continue;
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
        image.sprite = reward.Icon;
        image.preserveAspect = true;
        image.enabled = reward.Icon != null;
        if (reward.Icon != null) return;
        var root = new GameObject("RewardPreview", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(image.transform, false);
        root.sizeDelta = new Vector2(150f, 60f);
        if (reward.Kind == RoomRewardKind.Chip && reward.Chip != null)
        {
            ChipDefinition chip = reward.Chip;
            Vector2Int min = Vector2Int.zero, max = Vector2Int.zero;
            foreach (Vector2Int cell in chip.ShapeCells) { min = Vector2Int.Min(min, cell); max = Vector2Int.Max(max, cell); }
            float size = Mathf.Min(24f, 60f / (max.y - min.y + 1), 150f / (max.x - min.x + 1));
            Vector2 center = ((Vector2)min + (Vector2)max) * .5f;
            foreach (Vector2Int cell in chip.ShapeCells)
                DrawCell(root, ((Vector2)cell - center) * size, Vector2.one * (size - 2f), reward.GradeColor);
            foreach (ChipPin pin in chip.Pins)
            {
                Vector2 direction = pin.direction.ToOffset();
                DrawCell(root, ((Vector2)pin.cell - center + direction * .4f) * size,
                    new Vector2(direction.x == 0 ? size * .4f : 3f, direction.x == 0 ? 3f : size * .4f),
                    pin.type == PinType.Input ? new Color(.3f, 1f, .65f) : new Color(1f, .55f, .2f));
            }
        }
        else if (reward.Equipment != null)
        {
            TMP_Text text = root.gameObject.AddComponent<TextMeshProUGUI>();
            GameFontManager.ApplyFont(text);
            text.text = EquipmentSlotInfo.Name(reward.Equipment.Slot);
            text.fontSize = 26f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = reward.GradeColor;
            text.raycastTarget = false;
        }
    }

    private static void DrawCell(RectTransform parent, Vector2 position, Vector2 size, Color color)
    {
        var rect = new GameObject("Cell", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image graphic = rect.GetComponent<Image>();
        graphic.color = color;
        graphic.raycastTarget = false;
    }

    public static string Describe(RoomRewardDefinition reward, GameObject player)
    {
        if (reward == null) return string.Empty;
        if (reward.Kind == RoomRewardKind.Equipment && reward.Equipment != null)
        {
            PlayerEquipment slots = player != null ? player.GetComponentInChildren<PlayerEquipment>() : null;
            EquipmentDefinition current = slots != null ? slots.GetEquipped(reward.Equipment.Slot) : null;
            var proposed = new CoreBoardStats();
            reward.Equipment.ApplyModifiers(proposed);
            var previous = new CoreBoardStats();
            if (current != null) current.ApplyModifiers(previous);
            var text = new StringBuilder();
            text.AppendLine(current != null ? "교체: " + current.DisplayName : "빈 부위에 장착");
            AppendStats(text, proposed, current != null ? previous : null);
            AppendReaction(text, reward.Equipment.ReactiveEffect);
            if (current != null && current.ReactiveEffect != null && current.ReactiveEffect.IsConfigured)
                text.AppendLine("교체로 잃음: " + current.ReactiveEffect.Describe());
            if (current != null) text.Append("이전 부품은 바닥에 보관됩니다.");
            return text.ToString().TrimEnd();
        }
        if (reward.Kind == RoomRewardKind.Chip && reward.Chip != null)
        {
            CoreBoardController board = player != null ? player.GetComponentInChildren<CoreBoardController>() : null;
            if (board == null) board = UnityEngine.Object.FindFirstObjectByType<CoreBoardController>();
            return DescribeChip(reward.Chip, board);
        }
        return string.IsNullOrWhiteSpace(reward.Description) ? reward.BuildLabel() : reward.Description;
    }

    public static string DescribeChip(ChipDefinition chip, CoreBoardController board)
    {
        var text = new StringBuilder();
        text.Append(chip.CellCount).Append("칸 · ").AppendLine(FamilyName(chip.Family));
        text.AppendLine("기본 효과 · 회로에 따라 증폭");
        var stats = new CoreBoardStats();
        chip.ApplyModifiers(stats);
        AppendStats(text, stats, null);
        AppendReaction(text, chip.ReactiveEffect);
        if (chip.ReactiveEffect != null && chip.ReactiveEffect.IsConfigured)
            text.AppendLine("발동 효과는 동일 칩 중복/회로 증폭 제외");
        text.AppendLine(chip.NeedsCurrent ? "배치 후 전원 연결 필요" : "배치하면 적용 · 전원 불필요");
        if (board != null && board.IsReady)
            text.AppendLine(CanFit(chip, board.State) ? "현재 빈칸에 배치 가능" : "배치하려면 보드 재정리 필요");
        text.Append("획득 후 Tab으로 배치");
        return text.ToString();
    }

    public static string FamilyName(ChipFamily family)
    {
        switch (family)
        {
            case ChipFamily.Thermal: return "열 계열";
            case ChipFamily.Electric: return "전기 계열";
            case ChipFamily.Kinetic: return "역학 계열";
            case ChipFamily.Nano: return "나노 계열";
            default: return "무계열";
        }
    }

    private static bool CanFit(ChipDefinition chip, CoreBoardState state)
    {
        for (int r = 0; r < 4; r++)
            for (int y = 0; y < state.Layout.Height; y++)
                for (int x = 0; x < state.Layout.Width; x++)
                    if (state.CanPlace(chip, new Vector2Int(x, y), r).IsValid) return true;
        return false;
    }

    private static void AppendReaction(StringBuilder text, ReactiveItemEffect effect)
    {
        if (effect != null && effect.IsConfigured) text.AppendLine(effect.Describe());
    }

    private static void AppendStats(StringBuilder text, CoreBoardStats proposed, CoreBoardStats previous)
    {
        foreach (ChipStatKind kind in Enum.GetValues(typeof(ChipStatKind)))
        {
            float value = proposed.Get(kind);
            float before = previous != null ? previous.Get(kind) : 0f;
            if (Mathf.Approximately(value, 0f) && Mathf.Approximately(before, 0f)) continue;
            text.Append(MetaEffectFormat.StatName(kind)).Append(' ').Append(MetaEffectFormat.StatValue(kind, value));
            if (previous != null)
            {
                float delta = value - before;
                text.Append(" (변화 ").Append(MetaEffectFormat.StatValue(kind, delta)).Append(')');
            }
            if (kind == ChipStatKind.CooldownReductionRate) text.Append(" · 재사용 기능용");
            text.AppendLine();
        }
    }

    /// <summary>등급이 아닌 모든 실제 스탯으로 비교한다. 장단점이 있는 교체는 허용한다.</summary>
    public static bool IsStrictDowngrade(EquipmentDefinition candidate, EquipmentDefinition current)
    {
        if (candidate == null || current == null || candidate.Slot != current.Slot) return false;
        // 조건부 효과는 단순 스탯 우열로 제외하지 않는다.
        if (candidate.ReactiveEffect != null && candidate.ReactiveEffect.IsConfigured) return false;
        var a = new CoreBoardStats();
        var b = new CoreBoardStats();
        candidate.ApplyModifiers(a);
        current.ApplyModifiers(b);
        bool worse = false;
        foreach (ChipStatKind kind in Enum.GetValues(typeof(ChipStatKind)))
        {
            if (a.Get(kind) > b.Get(kind) + .0001f) return false;
            if (a.Get(kind) < b.Get(kind) - .0001f) worse = true;
        }
        return worse;
    }
}
