using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드의 런타임 상태. 어떤 칩이 어디에 어떤 회전으로 꽂혀 있는지를 들고 있으며,
/// 배치 가능 여부를 판정한다. MonoBehaviour가 아니므로 테스트에서 그냥 new 해서 쓸 수 있다.
/// </summary>
public sealed class CoreBoardState
{
    /// <summary>칩이 놓이거나 빠지거나 움직일 때마다 호출된다.</summary>
    public event Action OnChanged;

    private const int EmptyCell = 0;

    private readonly CoreBoardLayout layout;
    private readonly int[] occupancy;
    private readonly Dictionary<int, PlacedChip> placements = new Dictionary<int, PlacedChip>();
    private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>(8);
    private readonly List<Vector2Int> writeBuffer = new List<Vector2Int>(8);
    private readonly HashSet<int> shutDownChips = new HashSet<int>();
    private readonly HashSet<int> throttledChips = new HashSet<int>();
    private readonly HashSet<int> blockedChips = new HashSet<int>();
    private readonly float[] cellHeat;
    private readonly float[] localHeat;
    private CircuitTuning tuning;
    private OverclockSettings overclock = OverclockSettings.Default;
    private bool overclockActive;
    private int nextId = 1;

    public CoreBoardState(CoreBoardLayout boardLayout)
        : this(boardLayout, CircuitTuning.Default)
    {
    }

    public CoreBoardState(CoreBoardLayout boardLayout, CircuitTuning circuitTuning)
    {
        layout = boardLayout;
        tuning = circuitTuning;
        int cellCount = boardLayout != null ? boardLayout.CellCount : 0;
        occupancy = new int[cellCount];
        cellHeat = new float[cellCount];
        localHeat = new float[cellCount];
        Recalculate();
    }

    public CoreBoardLayout Layout => layout;
    public int PlacedCount => placements.Count;
    public IEnumerable<PlacedChip> Placements => placements.Values;

    /// <summary>가장 최근 전류 해석 결과. 어떤 칩이 살아 있는지 여기서 본다.</summary>
    public CircuitSolution Solution { get; private set; }

    /// <summary>회로 배율까지 반영해 합산된 스탯. 재배치할 때마다 새로 만들어진다.</summary>
    public CoreBoardStats Stats { get; private set; }

    /// <summary>전류가 닿아 실제로 작동 중인가. 패시브 칩은 항상 참이다.</summary>
    public bool IsEnergized(PlacedChip placed) =>
        placed != null && Solution != null && Solution.IsEnergized(placed.Id);

    /// <summary>회로 수치를 바꾼 뒤 다시 풀 때 쓴다.</summary>
    public void SetTuning(CircuitTuning circuitTuning)
    {
        tuning = circuitTuning;
        Recalculate();
    }

    public void SetOverclockSettings(OverclockSettings settings)
    {
        overclock = settings;
        Recalculate();
    }

    /// <summary>오버클럭. 모든 회로가 세지는 대신 발열이 배로 뛴다.</summary>
    public bool OverclockActive => overclockActive;

    public void SetOverclock(bool active)
    {
        if (overclockActive == active) return;
        overclockActive = active;
        Recalculate();
    }

    /// <summary>국소 과열로 스스로 멈춘 칩.</summary>
    public bool IsShutDownByHeat(PlacedChip placed) =>
        placed != null && shutDownChips.Contains(placed.Id);

    /// <summary>전역 과열 스로틀링으로 잠시 꺼진 칩.</summary>
    public bool IsThrottled(PlacedChip placed) =>
        placed != null && throttledChips.Contains(placed.Id);

    /// <summary>스로틀링이 끈 칩 목록을 통째로 갈아 끼운다.</summary>
    public void SetThrottled(IEnumerable<int> chipIds)
    {
        throttledChips.Clear();
        if (chipIds != null)
        {
            foreach (int id in chipIds) throttledChips.Add(id);
        }
        Recalculate();
    }

    /// <summary>이 칸을 중심으로 한 3x3 범위가 요구하는 발열. 히트맵 표시에 쓴다.</summary>
    public float GetLocalHeat(Vector2Int cell) =>
        layout != null && layout.Contains(cell) ? localHeat[layout.ToIndex(cell)] : 0f;

    /// <summary>국소 한계를 넘어 그 구역이 정지한 칸인가.</summary>
    public bool IsHotspot(Vector2Int cell) =>
        layout != null && GetLocalHeat(cell) > layout.LocalHeatLimit;

    /// <summary>꽂힌 칩들의 발열 합. 방열 셀에 걸친 칩은 절반으로 친다.</summary>
    public int TotalHeat { get; private set; }

    /// <summary>보드가 감당할 수 있는 발열. 발열 용량을 올리는 칩이 있으면 함께 반영된다.</summary>
    public int HeatCapacity { get; private set; }

    public bool IsOverheated => TotalHeat > HeatCapacity;

    /// <summary>해당 칸을 차지한 칩. 비어 있으면 null.</summary>
    public PlacedChip GetChipAt(Vector2Int cell)
    {
        if (layout == null || !layout.Contains(cell)) return null;
        int id = occupancy[layout.ToIndex(cell)];
        return id == EmptyCell ? null : placements[id];
    }

    public PlacedChip GetChip(int id) =>
        placements.TryGetValue(id, out PlacedChip placed) ? placed : null;

    /// <summary>
    /// 이 칩을 여기에 놓을 수 있는지 판정한다.
    /// ignoreId를 주면 그 칩은 없는 것으로 치므로, 이동 중인 칩이 자기 자신과 겹쳐 실패하지 않는다.
    /// </summary>
    public PlacementResult CanPlace(
        ChipDefinition chip, Vector2Int origin, int rotation, int ignoreId = EmptyCell)
    {
        if (chip == null || layout == null)
        {
            return PlacementResult.Fail(PlacementError.InvalidInput, origin);
        }

        chip.GetRotatedCells(rotation, cellBuffer);
        if (cellBuffer.Count == 0)
        {
            return PlacementResult.Fail(PlacementError.InvalidInput, origin);
        }

        bool isSource = chip.Category == ChipCategory.Source;
        bool touchesPowerRail = false;

        foreach (Vector2Int offset in cellBuffer)
        {
            Vector2Int cell = origin + offset;
            if (!layout.Contains(cell))
            {
                return PlacementResult.Fail(PlacementError.OutOfBoard, cell);
            }

            BoardCellType cellType = layout.GetCell(cell);
            if (cellType == BoardCellType.Blocked)
            {
                return PlacementResult.Fail(PlacementError.BlockedCell, cell);
            }

            if (cellType == BoardCellType.PowerRail)
            {
                if (!isSource)
                {
                    return PlacementResult.Fail(PlacementError.PowerRailReserved, cell);
                }
                touchesPowerRail = true;
            }

            int occupant = occupancy[layout.ToIndex(cell)];
            if (occupant != EmptyCell && occupant != ignoreId)
            {
                return PlacementResult.Fail(PlacementError.Overlap, cell);
            }
        }

        if (isSource && !touchesPowerRail)
        {
            return PlacementResult.Fail(PlacementError.SourceNeedsPowerRail, origin);
        }
        return PlacementResult.Success;
    }

    /// <summary>검증을 통과하면 칩을 꽂고 true를 돌려준다.</summary>
    public bool TryPlace(
        ChipDefinition chip, Vector2Int origin, int rotation, out PlacedChip placed)
    {
        placed = null;
        PlacementResult result = CanPlace(chip, origin, rotation);
        if (!result.IsValid) return false;

        placed = new PlacedChip(nextId++, chip, origin, BoardGeometry.NormalizeRotation(rotation));
        placements[placed.Id] = placed;
        Stamp(placed, placed.Id);
        Recalculate();
        return true;
    }

    public bool TryPlace(ChipDefinition chip, Vector2Int origin, int rotation) =>
        TryPlace(chip, origin, rotation, out _);

    /// <summary>이미 꽂힌 칩을 다른 자리나 다른 회전으로 옮긴다. 실패하면 원래 자리를 유지한다.</summary>
    public bool TryMove(int id, Vector2Int origin, int rotation)
    {
        if (!placements.TryGetValue(id, out PlacedChip placed)) return false;

        PlacementResult result = CanPlace(placed.Chip, origin, rotation, id);
        if (!result.IsValid) return false;

        Stamp(placed, EmptyCell);
        placed.Origin = origin;
        placed.Rotation = BoardGeometry.NormalizeRotation(rotation);
        Stamp(placed, id);
        Recalculate();
        return true;
    }

    public bool Remove(int id)
    {
        if (!placements.TryGetValue(id, out PlacedChip placed)) return false;

        Stamp(placed, EmptyCell);
        placements.Remove(id);
        Recalculate();
        return true;
    }

    public bool RemoveAt(Vector2Int cell)
    {
        PlacedChip placed = GetChipAt(cell);
        return placed != null && Remove(placed.Id);
    }

    public void Clear()
    {
        if (placements.Count == 0) return;
        placements.Clear();
        Array.Clear(occupancy, 0, occupancy.Length);
        Recalculate();
    }

    /// <summary>칩이 실제로 차지하는 보드 칸을 버퍼에 채운다.</summary>
    public void CollectOccupiedCells(PlacedChip placed, List<Vector2Int> buffer)
    {
        if (buffer == null) return;
        buffer.Clear();
        if (placed == null || placed.Chip == null) return;

        placed.Chip.GetRotatedCells(placed.Rotation, writeBuffer);
        foreach (Vector2Int offset in writeBuffer)
        {
            buffer.Add(placed.Origin + offset);
        }
    }

    /// <summary>
    /// 꽂힌 칩의 효과를 합산한다. 전류가 닿지 않은 칩은 배율이 0이라 아무것도 내지 않고,
    /// 종단 칩은 회로 길이·계열 보너스가 곱해진 값으로 들어간다.
    /// </summary>
    public CoreBoardStats BuildStats()
    {
        CoreBoardStats stats = new CoreBoardStats();
        foreach (PlacedChip placed in placements.Values)
        {
            if (placed.Chip == null) continue;

            float multiplier = Solution.GetMultiplier(placed.Id);
            // 오버클럭은 전류를 쓰는 칩만 밀어 준다. 패시브는 그대로다.
            if (overclockActive && placed.Chip.NeedsCurrent)
            {
                multiplier *= 1f + overclock.boost;
            }
            placed.Chip.ApplyModifiers(stats, multiplier);
        }
        return stats;
    }

    /// <summary>칩이 차지한 칸에 자기 id를 찍는다. id가 0이면 지운다.</summary>
    private void Stamp(PlacedChip placed, int id)
    {
        if (layout == null) return;

        placed.Chip.GetRotatedCells(placed.Rotation, writeBuffer);
        foreach (Vector2Int offset in writeBuffer)
        {
            Vector2Int cell = placed.Origin + offset;
            if (!layout.Contains(cell)) continue;
            occupancy[layout.ToIndex(cell)] = id;
        }
    }

    /// <summary>
    /// 두 번 푼다. 먼저 열 제한 없이 풀어 "이 배치가 요구하는 발열"을 구하고,
    /// 그 히트맵에서 국소 한계를 넘은 구역의 칩을 멈춘 뒤 다시 푼다.
    /// 칩을 멈추면 열은 줄기만 하므로 한 번만 되풀어도 값이 흔들리지 않는다.
    /// </summary>
    private void Recalculate()
    {
        blockedChips.Clear();
        foreach (int id in throttledChips) blockedChips.Add(id);

        // 1차: 스로틀링만 반영해 풀고, 그 결과로 발열 요구량을 잰다.
        Solution = CircuitSolver.Solve(this, tuning, blockedChips);
        BuildHeatMap();

        // 2차: 국소 과열로 멈춘 칩까지 빼고 다시 푼다.
        CollectHeatShutdowns();
        if (shutDownChips.Count > 0)
        {
            foreach (int id in shutDownChips) blockedChips.Add(id);
            Solution = CircuitSolver.Solve(this, tuning, blockedChips);
        }

        Stats = BuildStats();

        TotalHeat = 0;
        foreach (PlacedChip placed in placements.Values)
        {
            TotalHeat += ResolveHeat(placed, true);
        }

        int baseCapacity = layout != null ? layout.BaseHeatCapacity : 0;
        HeatCapacity = baseCapacity + Stats.GetInt(ChipStatKind.HeatCapacity);

        OnChanged?.Invoke();
    }

    /// <summary>칸별 발열과 3x3 합을 만든다. 칩의 열은 자기가 덮은 칸에 고르게 나눈다.</summary>
    private void BuildHeatMap()
    {
        if (layout == null) return;

        Array.Clear(cellHeat, 0, cellHeat.Length);
        Array.Clear(localHeat, 0, localHeat.Length);

        foreach (PlacedChip placed in placements.Values)
        {
            // 히트맵은 오버클럭을 빼고 잰다. 국소 과열은 배치로 푸는 문제고,
            // 오버클럭이 만드는 열은 전역 스로틀링이 맡는다.
            int heat = ResolveHeat(placed, false);
            if (heat <= 0) continue;

            placed.Chip.GetRotatedCells(placed.Rotation, writeBuffer);
            if (writeBuffer.Count == 0) continue;

            float perCell = (float)heat / writeBuffer.Count;
            foreach (Vector2Int offset in writeBuffer)
            {
                Vector2Int cell = placed.Origin + offset;
                if (!layout.Contains(cell)) continue;
                cellHeat[layout.ToIndex(cell)] += perCell;
            }
        }

        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                float sum = 0f;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        Vector2Int neighbour = new Vector2Int(x + offsetX, y + offsetY);
                        if (!layout.Contains(neighbour)) continue;
                        sum += cellHeat[layout.ToIndex(neighbour)];
                    }
                }
                localHeat[layout.ToIndex(new Vector2Int(x, y))] = sum;
            }
        }
    }

    /// <summary>칸 하나라도 국소 한계를 넘는 자리에 있으면 그 칩은 정지한다.</summary>
    private void CollectHeatShutdowns()
    {
        shutDownChips.Clear();
        if (layout == null) return;

        foreach (PlacedChip placed in placements.Values)
        {
            if (placed.Chip == null || throttledChips.Contains(placed.Id)) continue;

            placed.Chip.GetRotatedCells(placed.Rotation, writeBuffer);
            foreach (Vector2Int offset in writeBuffer)
            {
                Vector2Int cell = placed.Origin + offset;
                if (!IsHotspot(cell)) continue;
                shutDownChips.Add(placed.Id);
                break;
            }
        }
    }

    /// <summary>
    /// 방열 셀에 한 칸이라도 걸쳐 있으면 발열이 절반이 된다.
    /// 전류가 닿지 않은 칩은 일을 하지 않으므로 열도 내지 않는다.
    /// </summary>
    private int ResolveHeat(PlacedChip placed, bool applyOverclock)
    {
        if (placed == null || placed.Chip == null || layout == null) return 0;
        if (!IsEnergized(placed)) return 0;

        float heat = placed.Chip.Heat;
        if (applyOverclock && overclockActive) heat *= overclock.heatScale;

        placed.Chip.GetRotatedCells(placed.Rotation, writeBuffer);
        foreach (Vector2Int offset in writeBuffer)
        {
            Vector2Int cell = placed.Origin + offset;
            if (layout.GetCell(cell) != BoardCellType.HeatSink) continue;
            heat *= 0.5f;
            break;
        }
        return Mathf.CeilToInt(heat);
    }
}
