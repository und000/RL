using UnityEngine;

/// <summary>방이 이웃과 이어지는 네 방향.</summary>
public enum RoomDirection
{
    North,
    East,
    South,
    West
}

/// <summary>방의 역할. 층 생성기가 종류별로 프리팹 풀을 골라 배치한다.</summary>
public enum RoomKind
{
    Start,
    Normal,
    Elite,
    Treasure,
    Shop,
    Boss
}

public static class RoomDirectionUtility
{
    public static readonly RoomDirection[] All =
    {
        RoomDirection.North,
        RoomDirection.East,
        RoomDirection.South,
        RoomDirection.West
    };

    public static RoomDirection Opposite(this RoomDirection direction)
    {
        switch (direction)
        {
            case RoomDirection.North: return RoomDirection.South;
            case RoomDirection.South: return RoomDirection.North;
            case RoomDirection.East: return RoomDirection.West;
            default: return RoomDirection.East;
        }
    }

    /// <summary>그리드 좌표 기준 이웃 칸 오프셋.</summary>
    public static Vector2Int ToOffset(this RoomDirection direction)
    {
        switch (direction)
        {
            case RoomDirection.North: return new Vector2Int(0, 1);
            case RoomDirection.South: return new Vector2Int(0, -1);
            case RoomDirection.East: return new Vector2Int(1, 0);
            default: return new Vector2Int(-1, 0);
        }
    }
}
