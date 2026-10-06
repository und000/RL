using UnityEngine;
using UnityEngine.UI;

/// <summary>폰트에 의존하지 않는 편집 가능한 픽셀 도형 아이콘.</summary>
public class RoomMapIcon : MaskableGraphic
{
    [SerializeField] private RoomMapSymbol symbol;
    public void SetSymbol(RoomMapSymbol value)
    {
        if (symbol == value) return;
        symbol = value;
        SetVerticesDirty();
    }
    private static readonly string[][] Pixels =
    {
        new[] {"0001000","0011100","0111110","1111111","0111110","0110110","0110110"},
        new[] {"0100010","0111110","1101011","1111111","0111110","0101010","0101010"},
        new[] {"0111110","1100011","1111111","1001001","1111111","1000001","1111111"},
        new[] {"0111110","1111111","1010101","0000000","0100010","0101010","0111110"},
        new[] {"0000000","0000001","0000011","1000110","1101100","0111000","0010000"},
        new[] {"1111100","1000100","1000110","1011111","1000110","1000100","1111100"}
    };
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float pixel = Mathf.Min(rect.width, rect.height) / 7f;
        Vector2 origin = rect.center - Vector2.one * pixel * 3.5f;
        string[] rows = Pixels[(int)symbol];
        for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++)
        {
            if (rows[y][x] != '1') continue;
            float left = origin.x + x * pixel, bottom = origin.y + (6 - y) * pixel;
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            vh.AddVert(new Vector3(left, bottom + pixel), color, Vector2.zero);
            vh.AddVert(new Vector3(left + pixel, bottom + pixel), color, Vector2.zero);
            vh.AddVert(new Vector3(left + pixel, bottom), color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
