using UnityEngine;

/// <summary>
/// 방 한쪽 벽의 출입구. 층 전체가 한 번에 배치되므로 이동은 그냥 걸어서 하고,
/// 이 컴포넌트는 통로를 막는 장벽을 켜고 끄는 역할만 한다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Rooms/Room Door")]
public class RoomDoor : MonoBehaviour
{
    [Header("방향")]
    [SerializeField] private RoomDirection direction = RoomDirection.North;

    [Header("비주얼")]
    [Tooltip("전투 중 통로를 막는 문. 열리면 꺼진다.")]
    [SerializeField] private GameObject closedVisual;
    [Tooltip("이웃 방이 없을 때 통로를 영구히 막는 벽.")]
    [SerializeField] private GameObject sealedVisual;
    [Tooltip("열렸을 때만 보이는 장식(빛, 화살표 등). 없어도 된다.")]
    [SerializeField] private GameObject openVisual;

    /// <summary>이 문이 향하는 방향.</summary>
    public RoomDirection Direction => direction;

    /// <summary>이웃 방이 없어 영구히 막힌 통로인가.</summary>
    public bool IsSealed { get; private set; }

    /// <summary>현재 지나갈 수 있는가.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>이 문이 속한 방.</summary>
    public RoomInstance Owner { get; private set; }

    /// <summary>맞은편 방의 짝이 되는 문. 봉인된 문은 null이다.</summary>
    public RoomDoor LinkedDoor { get; private set; }

    public void Initialize(RoomInstance owner)
    {
        Owner = owner;
        IsSealed = true;
        LinkedDoor = null;
        ApplyVisual();
    }

    /// <summary>층 생성기가 이웃 방과 통로를 이어줄 때 호출한다.</summary>
    public void LinkTo(RoomDoor other)
    {
        LinkedDoor = other;
        IsSealed = false;
        ApplyVisual();
    }

    public void Open()
    {
        if (IsSealed) return;
        IsOpen = true;
        ApplyVisual();
    }

    public void Close()
    {
        IsOpen = false;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        // 봉인 = 벽만, 닫힘 = 문만, 열림 = 둘 다 꺼짐(+선택 장식)
        if (sealedVisual != null) sealedVisual.SetActive(IsSealed);
        if (closedVisual != null) closedVisual.SetActive(!IsSealed && !IsOpen);
        if (openVisual != null) openVisual.SetActive(!IsSealed && IsOpen);
    }

    private void OnValidate()
    {
        if (closedVisual == sealedVisual && closedVisual != null)
        {
            Debug.LogWarning(
                "Closed Visual과 Sealed Visual이 같은 오브젝트입니다. 서로 다른 것을 지정하세요.",
                this);
        }
    }
}
