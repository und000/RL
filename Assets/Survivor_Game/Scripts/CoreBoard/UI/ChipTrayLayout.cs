using System.Collections.Generic;
using UnityEngine;

/// <summary>표시용 묶음만 만든다. 인벤토리의 개별 칩과 순서는 바꾸지 않는다.</summary>
public static class ChipTrayLayout
{
    public sealed class Entry
    {
        public readonly ChipDefinition Chip;
        public int Count;
        public Entry(ChipDefinition chip) { Chip = chip; Count = 1; }
    }

    public static List<Entry> Group(IReadOnlyList<ChipDefinition> chips)
    {
        var result = new List<Entry>();
        var entries = new Dictionary<ChipDefinition, Entry>();
        if (chips == null) return result;
        foreach (ChipDefinition chip in chips)
        {
            if (chip == null) continue;
            if (entries.TryGetValue(chip, out Entry entry)) entry.Count++;
            else { entry = new Entry(chip); entries.Add(chip, entry); result.Add(entry); }
        }
        // 한 개를 꺼내도 남은 묶음이 인벤토리 내부 순서 때문에 이동하지 않게 한다.
        result.Sort((a, b) =>
        {
            int category = a.Chip.Category.CompareTo(b.Chip.Category);
            return category != 0 ? category : string.CompareOrdinal(a.Chip.DisplayName, b.Chip.DisplayName);
        });
        return result;
    }

    public static int Columns(float width, float slot) => Mathf.Max(1, Mathf.FloorToInt(width / Mathf.Max(1f, slot)));
    public static float Height(int count, int columns, float slot, float minimum) =>
        Mathf.Max(minimum, ((Mathf.Max(0, count) + Mathf.Max(1, columns) - 1) / Mathf.Max(1, columns)) * slot);

    public static float ShapeScale(ChipDefinition chip, float cellSize, float availableWidth, float availableHeight)
    {
        if (chip == null || chip.CellCount == 0) return 1f;
        Vector2Int min = chip.ShapeCells[0], max = min;
        foreach (Vector2Int cell in chip.ShapeCells)
        {
            min = Vector2Int.Min(min, cell);
            max = Vector2Int.Max(max, cell);
        }
        return Mathf.Min(1f, Mathf.Min(availableWidth / ((max.x - min.x + 1) * cellSize),
            availableHeight / ((max.y - min.y + 1) * cellSize)));
    }
}
