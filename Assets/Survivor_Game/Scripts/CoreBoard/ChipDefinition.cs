using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드에 꽂는 칩 한 종류. 모양·핀·발열·효과를 한 에셋에 담는다.
/// 회전은 배치할 때 결정되므로 여기에는 0도 기준 데이터만 넣는다.
/// </summary>
[CreateAssetMenu(fileName = "Chip_New", menuName = "Survivor/Core Board/Chip")]
public class ChipDefinition : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string displayName = "New Chip";
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color tintColor = Color.white;

    [Header("분류")]
    [SerializeField] private ChipCategory category = ChipCategory.Passive;
    [SerializeField] private ChipRarity rarity = ChipRarity.Common;
    [Tooltip("회로 하나가 전부 같은 계열이면 종단 칩이 보너스를 받는다.")]
    [SerializeField] private ChipFamily family = ChipFamily.None;

    [Header("모양")]
    [Tooltip("(0,0)을 기준점으로 하는 칸 목록. 반드시 (0,0)을 포함해야 한다.")]
    [SerializeField] private Vector2Int[] shapeCells = { Vector2Int.zero };

    [Header("핀")]
    [Tooltip("전류가 드나드는 변. 1단계에서는 저장만 하고 2단계 회로 해석에서 쓴다.")]
    [SerializeField] private ChipPin[] pins = Array.Empty<ChipPin>();

    [Header("발열")]
    [Tooltip("패시브 1 / 증폭 2~3 / 종단 4~6 / 유니크 8 정도를 기준으로 잡는다.")]
    [SerializeField, Min(0)] private int heat = 1;

    [Header("효과")]
    [SerializeField] private ChipStatModifier[] modifiers = Array.Empty<ChipStatModifier>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public Color TintColor => tintColor;
    public ChipCategory Category => category;
    public ChipRarity Rarity => rarity;
    public ChipFamily Family => family;
    /// <summary>전류가 닿아야 작동하는 칩인가. 패시브만 예외다.</summary>
    public bool NeedsCurrent => category != ChipCategory.Passive;
    public int Heat => heat;
    public int CellCount => shapeCells != null ? shapeCells.Length : 0;
    public IReadOnlyList<Vector2Int> ShapeCells => shapeCells;
    public IReadOnlyList<ChipPin> Pins => pins;

    /// <summary>회전을 적용한 칸 좌표를 버퍼에 채운다. 좌표는 아직 원점 기준이다.</summary>
    public void GetRotatedCells(int rotation, List<Vector2Int> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        if (shapeCells == null) return;

        foreach (Vector2Int cell in shapeCells)
        {
            buffer.Add(BoardGeometry.Rotate(cell, rotation));
        }
    }

    /// <summary>회전을 적용한 핀을 버퍼에 채운다. 칸 좌표와 방향이 함께 돈다.</summary>
    public void GetRotatedPins(int rotation, List<ChipPin> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        if (pins == null) return;

        foreach (ChipPin pin in pins)
        {
            ChipPin rotated = pin;
            rotated.cell = BoardGeometry.Rotate(pin.cell, rotation);
            rotated.direction = BoardGeometry.Rotate(pin.direction, rotation);
            buffer.Add(rotated);
        }
    }

    /// <summary>합산 결과에 이 칩의 효과를 더한다. scale은 회로가 정해 준 배율이다.</summary>
    public void ApplyModifiers(CoreBoardStats target, float scale = 1f)
    {
        if (target == null || modifiers == null || scale <= 0f) return;
        foreach (ChipStatModifier modifier in modifiers)
        {
            target.Add(modifier.stat, modifier.value * scale);
        }
    }

    private void OnValidate()
    {
        heat = Mathf.Max(0, heat);
        NormalizeShape();
        WarnOnDetachedPins();
    }

    /// <summary>중복 칸을 걷어내고 (0,0)이 반드시 포함되게 만든다.</summary>
    private void NormalizeShape()
    {
        if (shapeCells == null || shapeCells.Length == 0)
        {
            shapeCells = new[] { Vector2Int.zero };
            return;
        }

        List<Vector2Int> unique = new List<Vector2Int>(shapeCells.Length);
        foreach (Vector2Int cell in shapeCells)
        {
            if (!unique.Contains(cell)) unique.Add(cell);
        }
        if (!unique.Contains(Vector2Int.zero)) unique.Insert(0, Vector2Int.zero);

        if (unique.Count != shapeCells.Length) shapeCells = unique.ToArray();
    }

    private void WarnOnDetachedPins()
    {
        if (pins == null || shapeCells == null) return;

        foreach (ChipPin pin in pins)
        {
            if (Array.IndexOf(shapeCells, pin.cell) >= 0) continue;
            Debug.LogWarning(
                $"칩 '{DisplayName}'의 핀이 모양 밖의 칸 {pin.cell}을 가리킵니다.", this);
        }
    }
}
