using System.Collections.Generic;
using UnityEngine;

/// <summary>회로 해석에 쓰는 수치. 디자이너가 만지도록 컨트롤러에서 넘겨 준다.</summary>
[System.Serializable]
public struct CircuitTuning
{
    [Tooltip("정션이 전류를 나눌 때 각 갈래가 가져가는 비율.")]
    [Range(0.1f, 1f)] public float junctionEfficiency;
    [Tooltip("회로 안 칩이 전부 같은 계열일 때 종단 칩이 받는 추가 배율. 0.25 = +25%.")]
    [Min(0f)] public float familyBonusRate;
    [Tooltip("회로 길이(칩 수)별 종단 배율. 배열 끝을 넘어가는 길이는 마지막 값을 쓴다.")]
    public float[] lengthMultipliers;

    public static CircuitTuning Default => new CircuitTuning
    {
        junctionEfficiency = 0.7f,
        familyBonusRate = 0.25f,
        lengthMultipliers = new[] { 0.6f, 0.8f, 1f, 1.25f, 1.5f, 1.8f }
    };

    public float GetLengthMultiplier(int chainLength)
    {
        if (lengthMultipliers == null || lengthMultipliers.Length == 0) return 1f;
        int index = Mathf.Clamp(chainLength - 1, 0, lengthMultipliers.Length - 1);
        return lengthMultipliers[index];
    }

    public void Normalize()
    {
        junctionEfficiency = Mathf.Clamp(junctionEfficiency, 0.1f, 1f);
        familyBonusRate = Mathf.Max(0f, familyBonusRate);
        if (lengthMultipliers == null || lengthMultipliers.Length == 0)
        {
            lengthMultipliers = Default.lengthMultipliers;
        }
    }
}

/// <summary>전원 단자에서 종단 칩까지 끊김 없이 이어진 경로 하나.</summary>
public sealed class ResolvedCircuit
{
    public readonly List<PlacedChip> Chain;
    public readonly PlacedChip Terminal;
    public readonly float Efficiency;
    public readonly float LengthMultiplier;
    public readonly bool UniformFamily;
    public readonly ChipFamily Family;

    public ResolvedCircuit(
        List<PlacedChip> chain,
        float efficiency,
        float lengthMultiplier,
        bool uniformFamily,
        ChipFamily family,
        float familyBonusRate)
    {
        Chain = chain;
        Terminal = chain[chain.Count - 1];
        Efficiency = efficiency;
        LengthMultiplier = lengthMultiplier;
        UniformFamily = uniformFamily;
        Family = family;
        TotalMultiplier = efficiency * lengthMultiplier *
            (uniformFamily ? 1f + familyBonusRate : 1f);
    }

    /// <summary>종단 칩의 효과에 곱해지는 최종 배율.</summary>
    public float TotalMultiplier { get; private set; }

    public int Length => Chain.Count;

    /// <summary>"전원 → 증폭 → 볼트" 처럼 회로를 한 줄로 읽는다.</summary>
    public string Describe()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (PlacedChip chip in Chain)
        {
            if (builder.Length > 0) builder.Append(" → ");
            builder.Append(chip.Chip != null ? chip.Chip.DisplayName : "?");
        }
        builder.Append(" (×").Append(TotalMultiplier.ToString("0.00")).Append(')');
        return builder.ToString();
    }
}

/// <summary>보드 한 장을 푼 결과.</summary>
public sealed class CircuitSolution
{
    public readonly List<ResolvedCircuit> Circuits = new List<ResolvedCircuit>();

    /// <summary>칩 id별 최종 배율. 여기 없는 칩은 전류가 닿지 않은 죽은 칩이다.</summary>
    private readonly Dictionary<int, float> multipliers = new Dictionary<int, float>();

    public bool IsEnergized(int chipId) => multipliers.ContainsKey(chipId);

    public float GetMultiplier(int chipId) =>
        multipliers.TryGetValue(chipId, out float value) ? value : 0f;

    /// <summary>여러 경로로 닿으면 가장 좋은 배율만 남긴다.</summary>
    public void RaiseMultiplier(int chipId, float value)
    {
        if (multipliers.TryGetValue(chipId, out float existing) && existing >= value) return;
        multipliers[chipId] = value;
    }

    public int EnergizedCount => multipliers.Count;
}

/// <summary>
/// 전원 단자에서 출발해 핀이 맞물린 칩을 따라가며 회로를 찾는다.
/// 출력핀이 향한 칸에 상대 칩의 입력핀이 마주 보고 있어야 전류가 넘어가고,
/// 버스 칸은 같은 방향으로 전류를 그대로 흘려 보낸다.
/// </summary>
public static class CircuitSolver
{
    public static CircuitSolution Solve(CoreBoardState state, CircuitTuning tuning) =>
        Solve(state, tuning, null);

    /// <summary>
    /// disabled에 담긴 칩은 없는 셈 치지 않고 "멈춘 칩"으로 다룬다.
    /// 자리는 그대로 차지하므로 전류가 그 칸을 통과하지 못한다.
    /// </summary>
    public static CircuitSolution Solve(
        CoreBoardState state, CircuitTuning tuning, HashSet<int> disabled)
    {
        CircuitSolution solution = new CircuitSolution();
        if (state == null || state.Layout == null) return solution;

        tuning.Normalize();

        List<PlacedChip> chain = new List<PlacedChip>(8);
        foreach (PlacedChip placed in state.Placements)
        {
            if (placed.Chip == null || placed.Chip.Category != ChipCategory.Source) continue;
            if (IsDisabled(disabled, placed)) continue;
            Traverse(state, tuning, solution, placed, chain, 1f, disabled);
        }

        // 패시브 칩은 전류와 무관하게 항상 제값을 낸다.
        foreach (PlacedChip placed in state.Placements)
        {
            if (placed.Chip == null || placed.Chip.NeedsCurrent) continue;
            if (IsDisabled(disabled, placed)) continue;
            solution.RaiseMultiplier(placed.Id, 1f);
        }
        return solution;
    }

    private static bool IsDisabled(HashSet<int> disabled, PlacedChip chip) =>
        disabled != null && disabled.Contains(chip.Id);

    private static void Traverse(
        CoreBoardState state,
        CircuitTuning tuning,
        CircuitSolution solution,
        PlacedChip current,
        List<PlacedChip> chain,
        float efficiency,
        HashSet<int> disabled)
    {
        chain.Add(current);
        solution.RaiseMultiplier(current.Id, efficiency);

        if (current.Chip.Category == ChipCategory.Terminal)
        {
            RecordCircuit(tuning, solution, chain, efficiency);
            chain.RemoveAt(chain.Count - 1);
            return;
        }

        List<PlacedChip> next = FindDownstream(state, current, disabled);
        float branchEfficiency = efficiency;
        if (current.Chip.Category == ChipCategory.Junction && next.Count > 1)
        {
            branchEfficiency *= tuning.junctionEfficiency;
        }

        foreach (PlacedChip downstream in next)
        {
            // 같은 칩을 두 번 밟으면 고리가 되므로 끊는다.
            if (chain.Contains(downstream)) continue;
            Traverse(state, tuning, solution, downstream, chain, branchEfficiency, disabled);
        }
        chain.RemoveAt(chain.Count - 1);
    }

    private static void RecordCircuit(
        CircuitTuning tuning,
        CircuitSolution solution,
        List<PlacedChip> chain,
        float efficiency)
    {
        List<PlacedChip> snapshot = new List<PlacedChip>(chain);
        float lengthMultiplier = tuning.GetLengthMultiplier(snapshot.Count);

        ChipFamily family = snapshot[0].Chip.Family;
        bool uniform = family != ChipFamily.None;
        foreach (PlacedChip chip in snapshot)
        {
            if (chip.Chip.Family == family) continue;
            uniform = false;
            break;
        }

        ResolvedCircuit circuit = new ResolvedCircuit(
            snapshot, efficiency, lengthMultiplier, uniform, family, tuning.familyBonusRate);
        solution.Circuits.Add(circuit);

        // 종단 칩만 길이·계열 보너스를 받는다. 중간 칩은 제 효율 그대로다.
        solution.RaiseMultiplier(circuit.Terminal.Id, circuit.TotalMultiplier);
    }

    /// <summary>이 칩의 출력핀이 실제로 물려 있는 다음 칩들.</summary>
    private static List<PlacedChip> FindDownstream(
        CoreBoardState state, PlacedChip current, HashSet<int> disabled)
    {
        List<PlacedChip> found = new List<PlacedChip>(2);
        List<ChipPin> pins = new List<ChipPin>(4);
        current.Chip.GetRotatedPins(current.Rotation, pins);

        foreach (ChipPin pin in pins)
        {
            if (pin.type != PinType.Output) continue;

            Vector2Int from = current.Origin + pin.cell;
            PlacedChip target = FollowWire(state, from, pin.direction, out Vector2Int landing);
            if (target == null || target == current) continue;
            // 멈춘 칩은 자리를 막고 있으므로 전류가 그 앞에서 끊긴다.
            if (IsDisabled(disabled, target)) continue;
            if (!HasInputPinFacing(target, landing, pin.direction.Opposite())) continue;
            if (!found.Contains(target)) found.Add(target);
        }
        return found;
    }

    /// <summary>
    /// 한 칸씩 나아가며 칩을 찾는다. 빈 버스 칸은 계속 통과하고,
    /// 그 밖의 빈 칸이나 보드 끝을 만나면 전류가 끊긴다.
    /// </summary>
    private static PlacedChip FollowWire(
        CoreBoardState state, Vector2Int from, RoomDirection direction, out Vector2Int landing)
    {
        Vector2Int step = direction.ToOffset();
        Vector2Int cell = from + step;
        landing = cell;

        // 보드 한 변보다 더 멀리 갈 일은 없으므로 그만큼만 돈다.
        int limit = state.Layout.Width + state.Layout.Height;
        for (int guard = 0; guard < limit; guard++)
        {
            if (!state.Layout.Contains(cell)) return null;

            PlacedChip occupant = state.GetChipAt(cell);
            if (occupant != null)
            {
                landing = cell;
                return occupant;
            }

            if (state.Layout.GetCell(cell) != BoardCellType.Bus) return null;
            cell += step;
        }
        return null;
    }

    /// <summary>상대 칩이 그 칸의 마주 보는 변에 입력핀을 갖고 있는가.</summary>
    private static bool HasInputPinFacing(
        PlacedChip target, Vector2Int boardCell, RoomDirection facing)
    {
        List<ChipPin> pins = new List<ChipPin>(4);
        target.Chip.GetRotatedPins(target.Rotation, pins);

        foreach (ChipPin pin in pins)
        {
            if (pin.type != PinType.Input) continue;
            if (pin.direction != facing) continue;
            if (target.Origin + pin.cell == boardCell) return true;
        }
        return false;
    }
}
