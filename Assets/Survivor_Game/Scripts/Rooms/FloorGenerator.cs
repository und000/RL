using System.Collections.Generic;
using UnityEngine;

/// <summary>생성된 층 한 장의 결과물.</summary>
public class GeneratedFloor
{
    public readonly List<RoomInstance> Rooms = new List<RoomInstance>();
    public RoomInstance StartRoom;
    public RoomInstance BossRoom;
    public RoomInstance ExitRoom;
    public FloorExit Exit;
    public Transform Root;

    public bool IsCombatCleared
    {
        get
        {
            foreach (RoomInstance room in Rooms)
                if (room != null && room.IsCombatRoom && !room.IsCleared) return false;
            return Rooms.Count > 0;
        }
    }

    public int ClearedCount
    {
        get
        {
            int count = 0;
            foreach (RoomInstance room in Rooms)
            {
                if (room != null && room.IsCleared) count++;
            }
            return count;
        }
    }
}

/// <summary>
/// 방 프리팹을 그리드에 절차적으로 배치해 한 층을 만든다.
/// 방 자체의 지형·장애물은 프리팹에서 직접 디자인하고, 여기서는 배치와 연결만 담당한다.
/// </summary>
public static class FloorGenerator
{
    /// <summary>단일 방 생성이 필요한 별도 모드용. 현재 런은 Generate로 여러 방을 연결한다.</summary>
    public static GeneratedFloor GenerateRoom(FloorProfile profile, RoomInstance prefab,
        RoomRuntimeContext context, Transform parent)
    {
        if (profile == null || prefab == null || context == null || context.ExitPrefab == null)
        {
            Debug.LogError("선택한 방과 출구 프리팹이 필요합니다.");
            return null;
        }
        var root = new GameObject("Stage_" + profile.DisplayName);
        root.transform.SetParent(parent, false);
        RoomInstance room = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
        room.name = "Room_" + prefab.Kind;
        room.Initialize(Vector2Int.zero, context);
        room.FinalizeDoors();
        var floor = new GeneratedFloor
        {
            Root = root.transform,
            StartRoom = room,
            ExitRoom = room,
            BossRoom = prefab.Kind == RoomKind.Boss ? room : null,
            Exit = Object.Instantiate(context.ExitPrefab, room.FloorExitPosition,
                Quaternion.identity, room.transform)
        };
        floor.Rooms.Add(room);
        return floor;
    }

    public static GeneratedFloor Generate(
        FloorProfile profile,
        RoomRuntimeContext context,
        Transform parent)
    {
        if (profile == null)
        {
            Debug.LogError("FloorProfile이 없어 층을 만들 수 없습니다.");
            return null;
        }

        if (context == null || context.ExitPrefab == null)
        {
            Debug.LogError("층 출구 프리팹이 없어 맵을 생성할 수 없습니다.");
            return null;
        }
        if (!TryPlan(profile, out List<Vector2Int> cells,
            out Dictionary<Vector2Int, RoomKind> kinds,
            out Dictionary<Vector2Int, RoomInstance> prefabs))
        {
            Debug.LogError($"'{profile.DisplayName}' 맵의 거리·디자인·방 수 제한을 만족할 수 없습니다. 방 수와 디자인 후보를 확인하세요.", profile);
            return null;
        }

        GameObject rootObject = new GameObject("Floor_" + profile.DisplayName);
        if (parent != null) rootObject.transform.SetParent(parent, false);

        GeneratedFloor floor = new GeneratedFloor();
        floor.Root = rootObject.transform;

        Dictionary<Vector2Int, RoomInstance> placed =
            new Dictionary<Vector2Int, RoomInstance>();
        Vector2 grid = profile.GridCellSize;
        Vector2Int exitCell = FindFarthestCell(cells);

        foreach (Vector2Int cell in cells)
        {
            RoomKind kind = kinds[cell];
            RoomInstance prefab = prefabs[cell];

            Vector3 position = new Vector3(cell.x * grid.x, cell.y * grid.y, 0f);
            RoomInstance room = Object.Instantiate(
                prefab, position, Quaternion.identity, rootObject.transform);
            room.name = $"Room_{kind}_{cell.x}_{cell.y}";
            room.Initialize(cell, context);

            placed[cell] = room;
            floor.Rooms.Add(room);
            if (kind == RoomKind.Start) floor.StartRoom = room;
            if (kind == RoomKind.Boss) floor.BossRoom = room;
            if (cell == exitCell) floor.ExitRoom = room;
        }

        LinkDoors(placed);
        foreach (RoomInstance room in floor.Rooms) room.FinalizeDoors();

        floor.Exit = Object.Instantiate(context.ExitPrefab, floor.ExitRoom.FloorExitPosition,
            Quaternion.identity, floor.ExitRoom.transform);
        return floor;
    }

    // 검증된 배치 계획을 먼저 만들고 나서만 실제 방을 생성한다.
    internal static bool TryPlan(FloorProfile profile, out List<Vector2Int> cells,
        out Dictionary<Vector2Int, RoomKind> kinds,
        out Dictionary<Vector2Int, RoomInstance> prefabs)
    {
        cells = null;
        kinds = null;
        prefabs = null;
        if (profile.EliteRoomCount + profile.TreasureRoomCount + profile.ShopRoomCount > profile.RoomCount - 2)
            return false;

        for (int attempt = 0; attempt < profile.GenerationAttempts; attempt++)
        {
            List<Vector2Int> candidateCells = PickCells(profile.RoomCount);
            Dictionary<Vector2Int, RoomKind> candidateKinds = AssignKinds(candidateCells, profile);
            if (candidateKinds == null) continue;
            if (!TryAssignPrefabs(candidateCells, candidateKinds, profile, out var candidatePrefabs)) continue;
            cells = candidateCells;
            kinds = candidateKinds;
            prefabs = candidatePrefabs;
            return true;
        }
        return false;
    }

    private static bool TryAssignPrefabs(List<Vector2Int> cells,
        Dictionary<Vector2Int, RoomKind> kinds, FloorProfile profile,
        out Dictionary<Vector2Int, RoomInstance> selected)
    {
        selected = new Dictionary<Vector2Int, RoomInstance>();
        var options = new Dictionary<Vector2Int, List<RoomInstance>>();
        var order = new List<Vector2Int>(cells);
        foreach (Vector2Int cell in cells)
        {
            List<RoomInstance> candidates = profile.GetRoomPrefabs(kinds[cell]);
            // 존재하는 모든 이웃과 실제 문으로 연결할 수 있어야 한다.
            candidates.RemoveAll(prefab => !HasRequiredDoors(prefab, cell, kinds));
            if (candidates.Count == 0) return false;
            Shuffle(candidates);
            options[cell] = candidates;
        }
        // 선택지가 적은 방부터 배정하고, 실패하면 앞선 디자인 선택을 되돌린다.
        order.Sort((a, b) => options[a].Count.CompareTo(options[b].Count));
        int searchBudget = 10000;
        return AssignPrefabAt(0, order, options, selected, ref searchBudget);
    }

    private static bool HasRequiredDoors(RoomInstance prefab, Vector2Int cell,
        Dictionary<Vector2Int, RoomKind> kinds)
    {
        foreach (RoomDirection direction in RoomDirectionUtility.All)
            if (kinds.ContainsKey(cell + direction.ToOffset()) && prefab.GetDoor(direction) == null) return false;
        return true;
    }

    private static bool AssignPrefabAt(int index, List<Vector2Int> order,
        Dictionary<Vector2Int, List<RoomInstance>> options,
        Dictionary<Vector2Int, RoomInstance> selected, ref int searchBudget)
    {
        if (index == order.Count) return true;
        if (--searchBudget < 0) return false;
        Vector2Int cell = order[index];
        foreach (RoomInstance prefab in options[cell])
        {
            bool repeated = false;
            foreach (RoomDirection direction in RoomDirectionUtility.All)
            {
                if (selected.TryGetValue(cell + direction.ToOffset(), out RoomInstance neighbour) &&
                    string.Equals(prefab.DesignId, neighbour.DesignId, System.StringComparison.Ordinal))
                {
                    repeated = true;
                    break;
                }
            }
            if (repeated) continue;
            selected[cell] = prefab;
            if (AssignPrefabAt(index + 1, order, options, selected, ref searchBudget)) return true;
            selected.Remove(cell);
        }
        return false;
    }

    /// <summary>시작 칸에서 뻗어나가며 방이 놓일 그리드 칸을 고른다.</summary>
    private static List<Vector2Int> PickCells(int roomCount)
    {
        HashSet<Vector2Int> taken = new HashSet<Vector2Int> { Vector2Int.zero };
        List<Vector2Int> ordered = new List<Vector2Int> { Vector2Int.zero };
        List<Vector2Int> frontier = new List<Vector2Int> { Vector2Int.zero };

        while (ordered.Count < roomCount && frontier.Count > 0)
        {
            int frontierIndex = Random.Range(0, frontier.Count);
            Vector2Int from = frontier[frontierIndex];

            List<RoomDirection> directions =
                new List<RoomDirection>(RoomDirectionUtility.All);
            Shuffle(directions);

            bool grew = false;
            foreach (RoomDirection direction in directions)
            {
                Vector2Int next = from + direction.ToOffset();
                if (taken.Contains(next)) continue;

                taken.Add(next);
                ordered.Add(next);
                frontier.Add(next);
                grew = true;
                break;
            }

            // 사방이 막힌 칸은 더 뻗을 수 없으므로 후보에서 뺀다.
            if (!grew) frontier.RemoveAt(frontierIndex);
        }
        return ordered;
    }

    /// <summary>시작에서 가장 먼 전투방에 출구를 두고, 나머지에 역할을 나눠준다.</summary>
    private static Dictionary<Vector2Int, RoomKind> AssignKinds(
        List<Vector2Int> cells, FloorProfile profile)
    {
        Dictionary<Vector2Int, RoomKind> kinds = new Dictionary<Vector2Int, RoomKind>();
        foreach (Vector2Int cell in cells) kinds[cell] = RoomKind.Normal;

        kinds[Vector2Int.zero] = RoomKind.Start;

        Dictionary<Vector2Int, int> distances = BuildDistances(cells);
        Vector2Int exitCell = FindFarthestCell(cells);
        if (profile.IsBossStage) kinds[exitCell] = RoomKind.Boss;

        // 출구 방은 일반 전투방(보스 스테이지에서는 보스방)으로 예약한다.
        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            if (cell != exitCell && kinds[cell] == RoomKind.Normal) candidates.Add(cell);
        }
        Shuffle(candidates);

        if (profile.KeepNonCombatRoomsAwayFromStart)
        {
            if (!AssignRooms(candidates, kinds, distances, RoomKind.Treasure, profile.TreasureRoomCount, 2) ||
                !AssignRooms(candidates, kinds, distances, RoomKind.Shop, profile.ShopRoomCount, 2)) return null;
        }
        if (!AssignRooms(candidates, kinds, distances, RoomKind.Elite, profile.EliteRoomCount, 2)) return null;
        if (profile.KeepNonCombatRoomsAwayFromStart) return kinds;
        if (!AssignRooms(candidates, kinds, distances, RoomKind.Treasure, profile.TreasureRoomCount, 0) ||
            !AssignRooms(candidates, kinds, distances, RoomKind.Shop, profile.ShopRoomCount, 0)) return null;
        return kinds;
    }

    private static Vector2Int FindFarthestCell(List<Vector2Int> cells)
    {
        var distances = BuildDistances(cells);
        Vector2Int result = Vector2Int.zero;
        int farthest = -1;
        foreach (Vector2Int cell in cells)
        {
            if (cell == Vector2Int.zero) continue;
            if (distances.TryGetValue(cell, out int distance) && distance > farthest)
            {
                farthest = distance;
                result = cell;
            }
        }
        return result;
    }

    private static bool AssignRooms(
        List<Vector2Int> candidates,
        Dictionary<Vector2Int, RoomKind> kinds,
        Dictionary<Vector2Int, int> distances,
        RoomKind kind,
        int count, int minimumDistance)
    {
        int assigned = 0;
        for (int index = 0; index < candidates.Count && assigned < count;)
        {
            Vector2Int cell = candidates[index];
            if (!distances.TryGetValue(cell, out int distance) || distance < minimumDistance)
            {
                index++;
                continue;
            }

            kinds[cell] = kind;
            candidates.RemoveAt(index);
            assigned++;
        }

        return assigned == count;
    }

    private static Dictionary<Vector2Int, int> BuildDistances(List<Vector2Int> cells)
    {
        HashSet<Vector2Int> valid = new HashSet<Vector2Int>(cells);
        Dictionary<Vector2Int, int> distances =
            new Dictionary<Vector2Int, int> { { Vector2Int.zero, 0 } };
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(Vector2Int.zero);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int nextDistance = distances[current] + 1;
            foreach (RoomDirection direction in RoomDirectionUtility.All)
            {
                Vector2Int neighbour = current + direction.ToOffset();
                if (!valid.Contains(neighbour) || distances.ContainsKey(neighbour)) continue;
                distances[neighbour] = nextDistance;
                queue.Enqueue(neighbour);
            }
        }
        return distances;
    }

    /// <summary>맞닿은 방끼리 문을 이어준다. 이어지지 않은 문은 봉인된 채로 남는다.</summary>
    private static void LinkDoors(Dictionary<Vector2Int, RoomInstance> placed)
    {
        foreach (KeyValuePair<Vector2Int, RoomInstance> pair in placed)
        {
            foreach (RoomDirection direction in RoomDirectionUtility.All)
            {
                Vector2Int neighbourCell = pair.Key + direction.ToOffset();
                if (!placed.TryGetValue(neighbourCell, out RoomInstance neighbour)) continue;

                RoomDoor door = pair.Value.GetDoor(direction);
                RoomDoor otherDoor = neighbour.GetDoor(direction.Opposite());
                if (door == null || otherDoor == null) continue;

                door.LinkTo(otherDoor);
            }
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int index = list.Count - 1; index > 0; index--)
        {
            int swap = Random.Range(0, index + 1);
            T temp = list[index];
            list[index] = list[swap];
            list[swap] = temp;
        }
    }
}
