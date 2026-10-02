using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 칩 하나를 칸 단위로 그린다. 보드에 꽂힌 칩과 트레이의 칩, 드래그 중인 미리보기가
/// 모두 이 컴포넌트를 쓴다. 입력은 직접 처리하지 않고 주인인 CoreBoardView로 넘긴다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class ChipView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private readonly List<Image> cellImages = new List<Image>();
    private readonly List<Vector2Int> cellBuffer = new List<Vector2Int>(8);
    private readonly List<ChipPin> pinBuffer = new List<ChipPin>(4);

    private CoreBoardView owner;
    private RectTransform rect;
    private float cellSize;
    private bool interactive;
    private Color baseColor = Color.white;
    private Color inputPinColor;
    private Color outputPinColor;

    public ChipDefinition Chip { get; private set; }
    public int Rotation { get; private set; }

    /// <summary>보드에 꽂힌 칩이면 그 id, 트레이에 있는 칩이면 0.</summary>
    public int PlacedId { get; private set; }

    public RectTransform Rect
    {
        get
        {
            if (rect == null) rect = (RectTransform)transform;
            return rect;
        }
    }

    public void Bind(
        CoreBoardView view,
        ChipDefinition chip,
        int rotation,
        int placedId,
        float cellPixelSize,
        bool allowInput,
        Color color,
        Color inputPin,
        Color outputPin)
    {
        owner = view;
        Chip = chip;
        Rotation = rotation;
        PlacedId = placedId;
        cellSize = cellPixelSize;
        interactive = allowInput;
        baseColor = color;
        inputPinColor = inputPin;
        outputPinColor = outputPin;

        Rect.sizeDelta = Vector2.zero;
        RebuildVisual();
    }

    public void SetRotation(int rotation)
    {
        Rotation = BoardGeometry.NormalizeRotation(rotation);
        RebuildVisual();
    }

    /// <summary>죽은 칩을 흐리게 하거나, 드래그 중 배치 가부를 색으로 알릴 때 쓴다.</summary>
    public void SetTint(Color color)
    {
        foreach (Image image in cellImages)
        {
            if (image != null) image.color = color;
        }
    }

    public void ResetTint() => SetTint(baseColor);

    private void RebuildVisual()
    {
        for (int index = Rect.childCount - 1; index >= 0; index--)
        {
            CoreBoardView.DetachAndDestroy(Rect.GetChild(index));
        }
        cellImages.Clear();
        if (Chip == null) return;

        Chip.GetRotatedCells(Rotation, cellBuffer);
        foreach (Vector2Int offset in cellBuffer)
        {
            RectTransform cell = CreateChild("Cell");
            cell.sizeDelta = Vector2.one * (cellSize - 3f);
            cell.anchoredPosition = new Vector2(offset.x * cellSize, offset.y * cellSize);

            Image image = cell.gameObject.AddComponent<Image>();
            image.color = baseColor;
            image.raycastTarget = interactive;
            cellImages.Add(image);
        }

        Chip.GetRotatedPins(Rotation, pinBuffer);
        foreach (ChipPin pin in pinBuffer)
        {
            BuildPin(pin);
        }
    }

    /// <summary>핀은 칸 가장자리에 붙는 작은 돌기로 그린다.</summary>
    private void BuildPin(ChipPin pin)
    {
        Vector2 step = pin.direction.ToOffset();
        bool horizontal = Mathf.Abs(step.x) > 0.5f;

        RectTransform nub = CreateChild("Pin");
        nub.sizeDelta = horizontal
            ? new Vector2(cellSize * 0.14f, cellSize * 0.36f)
            : new Vector2(cellSize * 0.36f, cellSize * 0.14f);
        nub.anchoredPosition =
            new Vector2(pin.cell.x * cellSize, pin.cell.y * cellSize) + step * (cellSize * 0.45f);

        Image image = nub.gameObject.AddComponent<Image>();
        image.color = pin.type == PinType.Input ? inputPinColor : outputPinColor;
        image.raycastTarget = false;
    }

    private RectTransform CreateChild(string childName)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform));
        RectTransform childRect = (RectTransform)child.transform;
        childRect.SetParent(Rect, false);
        childRect.anchorMin = new Vector2(0.5f, 0.5f);
        childRect.anchorMax = new Vector2(0.5f, 0.5f);
        childRect.pivot = new Vector2(0.5f, 0.5f);
        return childRect;
    }

    /// <summary>왼쪽 버튼만 드래그로 친다. 오른쪽은 빼내기 클릭이다.</summary>
    private bool CanHandle(PointerEventData eventData) =>
        interactive && owner != null &&
        eventData.button == PointerEventData.InputButton.Left;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CanHandle(eventData)) owner.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CanHandle(eventData)) owner.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (CanHandle(eventData)) owner.EndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactive || owner == null) return;
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            owner.RemoveToTray(this);
        }
    }
}
