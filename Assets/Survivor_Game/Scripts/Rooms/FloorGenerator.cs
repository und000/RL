using System.Collections.Generic;
using UnityEngine;

/// <summary>생성된 층 한 장의 결과물.</summary>
public class GeneratedFloor
{
    public readonly List<RoomInstance> Rooms = new List<RoomInstance>();
    public RoomInstance StartRoom;
    public RoomInstance BossRoom;
    public Transform Root;

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

        List<Vector2Int> cells = PickCells(profile.RoomCount);
        Dictionary<Vector2Int, RoomKind> kinds = AssignKinds(cells, profile);

        GameObject rootObject = new GameObject("Floor_" + profile.DisplayName);
        if (parent != null) rootObject.transform.SetParent(parent, false);

        GeneratedFloor floor = new GeneratedFloor();
        floor.Root = rootObject.transform;

        Dictionary<Vector2Int, RoomInstance> placed =
            new Dictionary<Vector2Int, RoomInstance>();
        Vector2 grid = profile.GridCellSize;

        foreach (Vector2Int cell in cells)
        {
            RoomKind kind = kinds[cell];
            RoomInstance prefab = profile.PickRoomPrefab(kind);
            if (prefab == null)
            {
                Debug.LogError(
                    $"FloorProfile '{profile.DisplayName}'에 {kind} 방 프리팹이 없습니다.",
                    profile);
                continue;
            }

            Vector3 position = new Vector3(cell.x * grid.x, cell.y * grid.y, 0f);
            RoomInstance room = Object.Instantiate(
                prefab, position, Quaternion.identity, rootObject.transform);
            room.name = $"Room_{kind}_{cell.x}_{cell.y}";
            room.Initialize(cell, context);

            placed[cell] = room;
            floor.Rooms.Add(room);
            if (kind == RoomKind.Start) floor.StartRoom = room;
            if (kind == RoomKind.Boss) floor.BossRoom = room;
        }

        LinkDoors(placed);
        foreach (RoomInstance room in floor.Rooms) room.FinalizeDoors();

        if (floor.StartRoom == null && floor.Rooms.Count > 0)
        {
            floor.StartRoom = floor.Rooms[0];
        }
        return floor;
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

    /// <summary>시작에서 가장 먼 방을 보스 방으로 삼고, 나머지에 역할을 나눠준다.</summary>
    private static Dictionary<Vector2Int, RoomKind> AssignKinds(
        List<Vector2Int> cells, FloorProfile profile)
    {
        Dictionary<Vector2Int, RoomKind> kinds = new Dictionary<Vector2Int, RoomKind>();
        foreach (Vector2Int cell in cells) kinds[cell] = RoomKind.Normal;

        kinds[Vector2Int.zero] = RoomKind.Start;

        Dictionary<Vector2Int, int> distances = BuildDistances(cells);
        Vector2Int bossCell = Vector2Int.zero;
        int farthest = -1;
        foreach (Vector2Int cell in cells)
        {
            if (cell == Vector2Int.zero) continue;
            if (distances.TryGetValue(cell, out int distance) && distance > farthest)
            {
                farthest = distance;
                bossCell = cell;
            }
        }
        if (farthest >= 0) kinds[bossCell] = RoomKind.Boss;

        // 시작·보스를 뺀 나머지 중에서 특수 방을 뽑는다.
        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            if (kinds[cell] == RoomKind.Normal) candidates.Add(cell);
        }
        Shuffle(candidates);

        int cursor = 0;
        for (int i = 0; i < profile.EliteRoomCount && cursor < candidates.Count; i++)
        {
            kinds[candidates[cursor++]] = RoomKind.Elite;
        }
        for (int i = 0; i < profile.TreasureRoomCount && cursor < candidates.Count; i++)
        {
            kinds[candidates[cursor++]] = RoomKind.Treasure;
        }
        for (int i = 0; i < profile.ShopRoomCount && cursor < candidates.Count; i++)
        {
            kinds[candidates[cursor++]] = RoomKind.Shop;
        }
        return kinds;
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
