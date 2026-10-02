using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 씬에 하나 두는 코어 보드의 주인. 레이아웃으로 상태를 만들고,
/// 배치가 바뀔 때마다 합산 스탯을 다시 계산해 알린다.
/// 실제 스탯 반영(PlayerCombatStats 등)은 OnBoardChanged를 구독해서 붙인다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Core Board/Core Board Controller")]
public class CoreBoardController : MonoBehaviour
{
    [Serializable]
    public struct StartingChip
    {
        public ChipDefinition chip;
        public Vector2Int origin;
        [Range(0, 3)] public int rotation;
    }

    [Header("보드")]
    [Tooltip("런의 출발 지형. 런 중 확장은 이 에셋의 복제본에만 반영되므로 원본은 그대로다.")]
    [SerializeField] private CoreBoardLayout layout;

    [Header("회로 튜닝")]
    [SerializeField] private CircuitTuning circuitTuning = CircuitTuning.Default;

    [Header("오버클럭")]
    [SerializeField] private OverclockSettings overclock = OverclockSettings.Default;
    [Tooltip("전투 중 오버클럭을 켜고 끄는 키.")]
    [SerializeField] private Key overclockKey = Key.Q;

    [Header("과열 스로틀링")]
    [Tooltip("총 발열이 방열 용량을 넘는 동안, 이 간격마다 회로 하나가 꺼진다.")]
    [SerializeField, Min(0.2f)] private float throttleInterval = 3f;
    [Tooltip("한 번 꺼진 회로가 다시 살아나기까지 걸리는 시간.")]
    [SerializeField, Min(0.2f)] private float throttleDuration = 2f;

    [Tooltip("보드 변경으로 자리를 잃은 칩이 돌아갈 곳. 비워 두면 씬에서 찾는다.")]
    [SerializeField] private ChipInventory inventory;

    [Header("시작 구성")]
    [Tooltip("런을 시작할 때 미리 꽂혀 있는 칩. 배치가 불가능하면 경고만 남기고 건너뛴다.")]
    [SerializeField] private StartingChip[] startingChips = Array.Empty<StartingChip>();

    [Header("디버그")]
    [SerializeField] private bool logStatsOnChange;

    /// <summary>배치가 바뀔 때마다 새로 합산된 스탯과 함께 호출된다.</summary>
    public event Action<CoreBoardStats> OnBoardChanged;

    /// <summary>과열로 회로 하나가 강제로 꺼진 순간. HUD 연출용.</summary>
    public event Action<PlacedChip> OnCircuitThrottled;

    /// <summary>보드 지형이 넓어지거나 칸이 수리됐을 때. UI가 다시 그리도록 알린다.</summary>
    public event Action OnLayoutChanged;

    private readonly Dictionary<int, float> throttleExpiry = new Dictionary<int, float>();
    private CoreBoardLayout runtimeLayout;
    private CoreBoardState state;
    private CoreBoardStats stats = new CoreBoardStats();
    private float nextThrottleTime;

    public CoreBoardState State => state;
    public CoreBoardStats Stats => stats;
    public bool IsReady => state != null;
    public bool IsOverclocked => state != null && state.OverclockActive;

    private void Awake()
    {
        if (layout == null)
        {
            Debug.LogError("Core Board Controller에 Board Layout이 필요합니다.", this);
            enabled = false;
            return;
        }

        circuitTuning.Normalize();
        overclock.Normalize();

        if (inventory == null) inventory = GetComponent<ChipInventory>();

        // 런 중 보드가 넓어지므로 에셋을 직접 쓰면 원본이 영구히 바뀐다. 복제본으로 논다.
        runtimeLayout = Instantiate(layout);
        runtimeLayout.name = layout.name + " (런타임)";

        state = new CoreBoardState(runtimeLayout, circuitTuning);
        state.SetOverclockSettings(overclock);
        PlaceStartingChips();
        Refresh();
    }

    private void OnDestroy()
    {
        if (runtimeLayout != null) Destroy(runtimeLayout);
    }

    private void Update()
    {
        if (state == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[overclockKey].wasPressedThisFrame)
        {
            SetOverclock(!state.OverclockActive);
        }

        UpdateThrottling();
    }

    public void SetOverclock(bool active)
    {
        if (state == null || state.OverclockActive == active) return;
        state.SetOverclock(active);
        Refresh();
    }

    /// <summary>
    /// 총 발열이 방열 용량을 넘는 동안 일정 간격으로 회로 하나를 잠시 끈다.
    /// 끄면 열이 내려가 다시 붙고, 그래도 넘치면 또 꺼진다.
    /// </summary>
    private void UpdateThrottling()
    {
        bool changed = ExpireThrottles();

        if (state.IsOverheated)
        {
            if (Time.time >= nextThrottleTime)
            {
                nextThrottleTime = Time.time + throttleInterval;
                changed |= TryThrottleOneCircuit();
            }
        }
        else
        {
            // 열이 가라앉으면 다음 발동까지 다시 한 박자 쉰다.
            nextThrottleTime = Time.time + throttleInterval;
        }

        if (!changed) return;
        state.SetThrottled(throttleExpiry.Keys);
        Refresh();
    }

    private bool ExpireThrottles()
    {
        if (throttleExpiry.Count == 0) return false;

        List<int> expired = null;
        foreach (KeyValuePair<int, float> pair in throttleExpiry)
        {
            // 그 사이 뽑혀 사라진 칩도 함께 걷어낸다.
            if (Time.time < pair.Value && state.GetChip(pair.Key) != null) continue;
            if (expired == null) expired = new List<int>();
            expired.Add(pair.Key);
        }

        if (expired == null) return false;
        foreach (int id in expired) throttleExpiry.Remove(id);
        return true;
    }

    /// <summary>살아 있는 회로 중 하나를 골라 그 종단 칩을 끈다.</summary>
    private bool TryThrottleOneCircuit()
    {
        List<PlacedChip> candidates = new List<PlacedChip>();

        foreach (ResolvedCircuit circuit in state.Solution.Circuits)
        {
            PlacedChip terminal = circuit.Terminal;
            if (terminal != null && !throttleExpiry.ContainsKey(terminal.Id))
            {
                candidates.Add(terminal);
            }
        }

        // 종단이 없는 배치라면 전류를 쓰는 아무 칩이나 끈다.
        if (candidates.Count == 0)
        {
            foreach (PlacedChip placed in state.Placements)
            {
                if (placed.Chip == null || !placed.Chip.NeedsCurrent) continue;
                if (!state.IsEnergized(placed) || throttleExpiry.ContainsKey(placed.Id)) continue;
                candidates.Add(placed);
            }
        }
        if (candidates.Count == 0) return false;

        PlacedChip victim = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        throttleExpiry[victim.Id] = Time.time + throttleDuration;
        OnCircuitThrottled?.Invoke(victim);
        return true;
    }

    private void PlaceStartingChips()
    {
        if (startingChips == null) return;

        foreach (StartingChip entry in startingChips)
        {
            if (entry.chip == null) continue;

            PlacementResult result = state.CanPlace(entry.chip, entry.origin, entry.rotation);
            if (!result.IsValid)
            {
                Debug.LogWarning(
                    $"시작 칩 '{entry.chip.DisplayName}'을 {entry.origin}에 놓을 수 없습니다: " +
                    result.Describe(),
                    this);
                continue;
            }
            state.TryPlace(entry.chip, entry.origin, entry.rotation);
        }
    }

    // 보드 성장 -----------------------------------------------------------

    /// <summary>
    /// 보드를 넓힌다. 기존 칩은 좌하단 기준으로 그대로 남는다.
    /// 정예 방 보상 등에서 부른다.
    /// </summary>
    public bool ExpandBoard(int extraColumns, int extraRows)
    {
        if (state == null || (extraColumns <= 0 && extraRows <= 0)) return false;

        RebuildPreservingChips(() => runtimeLayout.Resize(
            runtimeLayout.Width + Mathf.Max(0, extraColumns),
            runtimeLayout.Height + Mathf.Max(0, extraRows)));
        return true;
    }

    /// <summary>손상 셀 하나를 골라 수리한다. 고칠 곳이 없으면 false.</summary>
    public bool RepairDamagedCell()
    {
        Vector2Int? target = FindCellOfType(BoardCellType.Blocked);
        if (target == null) return false;

        RebuildPreservingChips(() =>
            runtimeLayout.SetCell(target.Value, BoardCellType.Empty));
        return true;
    }

    /// <summary>빈 칸 하나를 전원 단자로 바꾼다. 회로를 하나 더 굴릴 수 있게 된다.</summary>
    public bool AddPowerRail() => ConvertFreeCell(BoardCellType.PowerRail);

    /// <summary>빈 칸 하나를 방열 셀로 바꾼다.</summary>
    public bool AddHeatSink() => ConvertFreeCell(BoardCellType.HeatSink);

    /// <summary>빈 칸 하나를 버스 칸으로 바꿔 전류가 멀리 뻗게 한다.</summary>
    public bool AddBusCell() => ConvertFreeCell(BoardCellType.Bus);

    /// <summary>칩이 놓이지 않은 빈 칸을 골라 다른 성질로 바꾼다.</summary>
    private bool ConvertFreeCell(BoardCellType type)
    {
        Vector2Int? target = FindCellOfType(BoardCellType.Empty);
        if (target == null) return false;

        RebuildPreservingChips(() => runtimeLayout.SetCell(target.Value, type));
        return true;
    }

    /// <summary>해당 성질이면서 칩이 덮고 있지 않은 칸을 무작위로 하나 고른다.</summary>
    private Vector2Int? FindCellOfType(BoardCellType type)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int y = 0; y < runtimeLayout.Height; y++)
        {
            for (int x = 0; x < runtimeLayout.Width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (runtimeLayout.GetCell(cell) != type) continue;
                if (state.GetChipAt(cell) != null) continue;
                candidates.Add(cell);
            }
        }
        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 지형을 바꾼 뒤 상태를 새로 만든다. 칸 배열 크기가 달라지므로 상태를 재사용할 수 없어,
    /// 꽂혀 있던 칩을 적어 두었다가 같은 자리에 다시 꽂는다.
    /// </summary>
    private void RebuildPreservingChips(Action mutateLayout)
    {
        List<PlacedChip> snapshot = new List<PlacedChip>(state.Placements);
        mutateLayout();

        state = new CoreBoardState(runtimeLayout, circuitTuning);
        state.SetOverclockSettings(overclock);
        state.SetOverclock(false);
        throttleExpiry.Clear();

        foreach (PlacedChip placed in snapshot)
        {
            if (placed.Chip == null) continue;
            if (state.TryPlace(placed.Chip, placed.Origin, placed.Rotation)) continue;

            // 수리한 칸이 하필 칩 자리와 겹치는 일은 없지만, 못 돌려놓으면 트레이로 보낸다.
            Debug.LogWarning(
                $"보드 변경 후 '{placed.Chip.DisplayName}'을 제자리에 돌려놓지 못했습니다.", this);
            if (inventory != null) inventory.Add(placed.Chip);
        }

        OnLayoutChanged?.Invoke();
        Refresh();
    }

    public PlacementResult CanPlace(ChipDefinition chip, Vector2Int origin, int rotation) =>
        state != null
            ? state.CanPlace(chip, origin, rotation)
            : PlacementResult.Fail(PlacementError.InvalidInput, origin);

    public bool TryPlace(ChipDefinition chip, Vector2Int origin, int rotation)
    {
        if (state == null || !state.TryPlace(chip, origin, rotation)) return false;
        Refresh();
        return true;
    }

    public bool TryMove(int id, Vector2Int origin, int rotation)
    {
        if (state == null || !state.TryMove(id, origin, rotation)) return false;
        Refresh();
        return true;
    }

    /// <summary>제자리에서 90도 돌린다. 돌린 자리가 막혀 있으면 아무 일도 일어나지 않는다.</summary>
    public bool TryRotate(int id)
    {
        PlacedChip placed = state != null ? state.GetChip(id) : null;
        return placed != null && TryMove(id, placed.Origin, placed.Rotation + 1);
    }

    public bool Remove(int id)
    {
        if (state == null || !state.Remove(id)) return false;
        Refresh();
        return true;
    }

    private void Refresh()
    {
        stats = state.Stats;
        if (logStatsOnChange)
        {
            Debug.Log(
                $"[코어 보드] 발열 {state.TotalHeat}/{state.HeatCapacity}" +
                $"{(state.OverclockActive ? " (오버클럭)" : string.Empty)} · " +
                $"회로 {state.Solution.Circuits.Count}개 · {stats}", this);
            foreach (ResolvedCircuit circuit in state.Solution.Circuits)
            {
                Debug.Log("[회로] " + circuit.Describe(), this);
            }
        }
        OnBoardChanged?.Invoke(stats);
    }
}
