public enum RoomMapSymbol { Start, Enemy, Reward, Shop, Complete, Exit }

/// <summary>작은 지도와 Tab 지도에서 공유하는 탐색/표시 규칙.</summary>
public static class RoomMapState
{
    public static bool Visible(bool visited) => visited;
    public static bool TravelAllowed(bool visited, bool anyCombat, bool busy, bool current) =>
        visited && !anyCombat && !busy && !current;
    public static RoomMapSymbol Symbol(RoomKind kind, bool visited, bool cleared, bool rewards)
    {
        if (!visited) return RoomMapSymbol.Exit;
        if (kind == RoomKind.Normal || kind == RoomKind.Elite || kind == RoomKind.Boss)
            return !cleared ? RoomMapSymbol.Enemy : rewards ? RoomMapSymbol.Reward : RoomMapSymbol.Complete;
        if (kind == RoomKind.Shop) return rewards ? RoomMapSymbol.Shop : RoomMapSymbol.Complete;
        if (rewards) return RoomMapSymbol.Reward;
        return kind == RoomKind.Start ? RoomMapSymbol.Start : RoomMapSymbol.Complete;
    }
}
