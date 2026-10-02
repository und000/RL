using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아직 보드에 꽂지 않고 들고 있는 칩. 방 보상으로 얻은 칩이 여기 쌓이고,
/// 보드에서 뽑아낸 칩도 여기로 돌아온다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Core Board/Chip Inventory")]
public class ChipInventory : MonoBehaviour
{
    [Header("시작 소지품")]
    [SerializeField] private List<ChipDefinition> startingChips = new List<ChipDefinition>();

    /// <summary>칩이 들어오거나 나갈 때마다 호출된다.</summary>
    public event Action OnInventoryChanged;

    private readonly List<ChipDefinition> chips = new List<ChipDefinition>();

    public IReadOnlyList<ChipDefinition> Chips => chips;
    public int Count => chips.Count;

    private void Awake()
    {
        foreach (ChipDefinition chip in startingChips)
        {
            if (chip != null) chips.Add(chip);
        }
    }

    public void Add(ChipDefinition chip)
    {
        if (chip == null) return;
        chips.Add(chip);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>같은 종류가 여러 개여도 하나만 뺀다.</summary>
    public bool Remove(ChipDefinition chip)
    {
        if (chip == null) return false;

        int index = chips.IndexOf(chip);
        if (index < 0) return false;

        chips.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool Contains(ChipDefinition chip) => chip != null && chips.Contains(chip);
}
