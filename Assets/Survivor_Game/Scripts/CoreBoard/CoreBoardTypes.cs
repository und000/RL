using System;
using UnityEngine;

/// <summary>
/// 칩의 역할. 배치 규칙과 전류 전달·회로 효과 계산에 사용한다.
/// </summary>
public enum ChipCategory
{
    /// <summary>전원 단자를 덮어 회로를 시작한다.</summary>
    Source,
    /// <summary>지나가는 회로의 효과를 수정한다.</summary>
    Amplifier,
    /// <summary>회로의 끝에서 실제 효과를 발현한다.</summary>
    Terminal,
    /// <summary>전류를 두 갈래로 나눈다.</summary>
    Junction,
    /// <summary>전류가 필요 없다. 꽂혀 있기만 하면 작동한다.</summary>
    Passive
}

public enum ChipRarity
{
    Common,
    Rare,
    Epic,
    Unique
}

/// <summary>
/// 칩의 계열. 회로 하나가 전부 같은 계열로 채워지면 종단 칩이 추가 보너스를 받는다.
/// None은 계열이 없는 것으로 쳐서 통일 판정을 깬다.
/// </summary>
public enum ChipFamily
{
    None,
    /// <summary>열.</summary>
    Thermal,
    /// <summary>전기.</summary>
    Electric,
    /// <summary>역학.</summary>
    Kinetic,
    /// <summary>나노.</summary>
    Nano
}

/// <summary>보드 한 칸의 성질.</summary>
public enum BoardCellType
{
    /// <summary>아무 칩이나 놓을 수 있는 빈 칸.</summary>
    Empty,
    /// <summary>손상되어 쓸 수 없는 칸. 정예 방 보상으로 수리한다.</summary>
    Blocked,
    /// <summary>전류가 시작되는 칸. 소스 칩만 덮을 수 있다.</summary>
    PowerRail,
    /// <summary>배선이 깔린 칸. 칩 없이도 전류를 옆으로 전달한다.</summary>
    Bus = 4 // 기존 직렬화 값을 유지한다.
}

public enum PinType
{
    Input,
    Output
}

/// <summary>칩 한 칸의 특정 변에 붙은 전류 단자.</summary>
[Serializable]
public struct ChipPin
{
    [Tooltip("칩 모양 안에서 이 핀이 붙은 칸. Shape Cells 중 하나여야 한다.")]
    public Vector2Int cell;
    [Tooltip("그 칸의 어느 변에 핀이 있는가.")]
    public RoomDirection direction;
    public PinType type;
}

/// <summary>배치가 거부된 이유.</summary>
public enum PlacementError
{
    None,
    /// <summary>칩의 일부가 보드 밖으로 나간다.</summary>
    OutOfBoard,
    /// <summary>손상 셀은 덮을 수 없다.</summary>
    BlockedCell,
    /// <summary>이미 다른 칩이 놓여 있다.</summary>
    Overlap,
    /// <summary>전원 단자는 소스 칩만 덮을 수 있다.</summary>
    PowerRailReserved,
    /// <summary>소스 칩은 전원 단자를 하나 이상 덮어야 한다.</summary>
    SourceNeedsPowerRail,
    /// <summary>칩이나 보드가 지정되지 않았다.</summary>
    InvalidInput
}

/// <summary>배치 검증 결과. 실패하면 어느 칸에서 걸렸는지도 함께 돌려준다.</summary>
public struct PlacementResult
{
    public readonly PlacementError Error;
    public readonly Vector2Int Cell;

    public PlacementResult(PlacementError error, Vector2Int cell)
    {
        Error = error;
        Cell = cell;
    }

    public bool IsValid => Error == PlacementError.None;

    public static PlacementResult Success => new PlacementResult(
        PlacementError.None, Vector2Int.zero);

    public static PlacementResult Fail(PlacementError error, Vector2Int cell) =>
        new PlacementResult(error, cell);

    /// <summary>UI에 그대로 띄울 수 있는 사유 문구.</summary>
    public string Describe()
    {
        switch (Error)
        {
            case PlacementError.None: return "배치 가능";
            case PlacementError.OutOfBoard: return "보드 밖으로 나갑니다";
            case PlacementError.BlockedCell: return "손상된 칸입니다";
            case PlacementError.Overlap: return "다른 칩과 겹칩니다";
            case PlacementError.PowerRailReserved: return "전원 단자에는 소스 칩만 놓을 수 있습니다";
            case PlacementError.SourceNeedsPowerRail: return "소스 칩은 전원 단자에 물려야 합니다";
            default: return "배치할 수 없습니다";
        }
    }
}

/// <summary>보드에 실제로 꽂혀 있는 칩 하나.</summary>
public sealed class PlacedChip
{
    public readonly int Id;
    public readonly ChipDefinition Chip;
    public Vector2Int Origin;
    public int Rotation;

    public PlacedChip(int id, ChipDefinition chip, Vector2Int origin, int rotation)
    {
        Id = id;
        Chip = chip;
        Origin = origin;
        Rotation = rotation;
    }
}

/// <summary>
/// 보드 격자 계산. 회전은 90도 단위 시계 방향이며,
/// 칸 좌표와 핀 방향이 같은 규칙으로 돈다.
/// </summary>
public static class BoardGeometry
{
    public const int RotationSteps = 4;

    private static readonly RoomDirection[] ClockwiseOrder =
    {
        RoomDirection.North,
        RoomDirection.East,
        RoomDirection.South,
        RoomDirection.West
    };

    public static int NormalizeRotation(int rotation)
    {
        int normalized = rotation % RotationSteps;
        return normalized < 0 ? normalized + RotationSteps : normalized;
    }

    /// <summary>원점 기준으로 칸 좌표를 회전시킨다.</summary>
    public static Vector2Int Rotate(Vector2Int cell, int rotation)
    {
        switch (NormalizeRotation(rotation))
        {
            case 1: return new Vector2Int(cell.y, -cell.x);
            case 2: return new Vector2Int(-cell.x, -cell.y);
            case 3: return new Vector2Int(-cell.y, cell.x);
            default: return cell;
        }
    }

    /// <summary>핀 방향을 칸 좌표와 같은 방향으로 회전시킨다.</summary>
    public static RoomDirection Rotate(RoomDirection direction, int rotation)
    {
        int index = Array.IndexOf(ClockwiseOrder, direction);
        if (index < 0) index = 0;
        return ClockwiseOrder[(index + NormalizeRotation(rotation)) % RotationSteps];
    }
}
