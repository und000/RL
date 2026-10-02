using System;
using UnityEngine;

/// <summary>
/// 보드 한 장의 지형. 손상 셀과 전원 단자 배치가 여기서 결정되므로,
/// 층마다 다른 레이아웃을 물려 주면 매 판 다른 퍼즐이 된다.
/// </summary>
[CreateAssetMenu(fileName = "CoreBoard_New", menuName = "Survivor/Core Board/Board Layout")]
public class CoreBoardLayout : ScriptableObject
{
    [Header("크기")]
    [SerializeField, Min(1)] private int width = 5;
    [SerializeField, Min(1)] private int height = 5;

    [Header("칸")]
    [Tooltip("좌하단 (0,0)부터 가로 우선 순서로 채운다. 크기가 맞지 않으면 자동으로 맞춰진다.")]
    [SerializeField] private BoardCellType[] cells = Array.Empty<BoardCellType>();

    [Header("방열")]
    [Tooltip("칩을 더 꽂지 않은 상태에서 감당할 수 있는 총 발열.")]
    [SerializeField, Min(0)] private int baseHeatCapacity = 10;
    [Tooltip("한 칸을 중심으로 한 3x3 범위가 견딜 수 있는 발열. " +
        "이걸 넘으면 그 구역의 칩이 정지한다.")]
    [SerializeField, Min(1f)] private float localHeatLimit = 8f;

    public int Width => width;
    public int Height => height;
    public int CellCount => width * height;
    public int BaseHeatCapacity => baseHeatCapacity;
    public float LocalHeatLimit => localHeatLimit;

    public bool Contains(Vector2Int cell) =>
        cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;

    public int ToIndex(Vector2Int cell) => cell.y * width + cell.x;

    public Vector2Int ToCell(int index) => new Vector2Int(index % width, index / width);

    public BoardCellType GetCell(Vector2Int cell)
    {
        if (!Contains(cell)) return BoardCellType.Blocked;
        EnsureSize();
        return cells[ToIndex(cell)];
    }

    /// <summary>전원 단자 수 = 동시에 굴릴 수 있는 회로 수.</summary>
    public int PowerRailCount
    {
        get
        {
            EnsureSize();
            int count = 0;
            foreach (BoardCellType cell in cells)
            {
                if (cell == BoardCellType.PowerRail) count++;
            }
            return count;
        }
    }

    /// <summary>에디터 툴이나 런타임 확장에서 칸 하나를 바꿀 때 쓴다.</summary>
    public void SetCell(Vector2Int cell, BoardCellType type)
    {
        if (!Contains(cell)) return;
        EnsureSize();
        cells[ToIndex(cell)] = type;
    }

    private void EnsureSize()
    {
        if (cells != null && cells.Length == CellCount) return;
        Resize(width, height);
    }

    /// <summary>기존 칸 내용을 좌하단 기준으로 유지한 채 크기를 바꾼다.</summary>
    public void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        BoardCellType[] resized = new BoardCellType[newWidth * newHeight];
        if (cells != null)
        {
            int copyWidth = Mathf.Min(width, newWidth);
            int copyHeight = Mathf.Min(height, newHeight);
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    int sourceIndex = y * width + x;
                    if (sourceIndex < 0 || sourceIndex >= cells.Length) continue;
                    resized[y * newWidth + x] = cells[sourceIndex];
                }
            }
        }

        width = newWidth;
        height = newHeight;
        cells = resized;
    }

    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        baseHeatCapacity = Mathf.Max(0, baseHeatCapacity);
        localHeatLimit = Mathf.Max(1f, localHeatLimit);
        if (cells == null || cells.Length != CellCount) Resize(width, height);
    }
}
