using UnityEngine;

/// <summary>
/// 런이 끝났을 때 잔존 코드를 얼마나 남길지 정하는 표.
/// 값은 전부 인스펙터에서 만진다.
/// </summary>
[CreateAssetMenu(
    fileName = "MetaSalvageProfile", menuName = "Survivor/Meta/Salvage Profile")]
public class MetaSalvageProfile : ScriptableObject
{
    [Header("진행")]
    [Tooltip("방 하나를 클리어할 때마다.")]
    [SerializeField, Min(0)] private int perRoom = 2;
    [Tooltip("층 하나를 넘길 때마다.")]
    [SerializeField, Min(0)] private int perFloor = 10;
    [Tooltip("챕터를 넘길 때마다 추가로. 뒤로 갈수록 한 층의 무게가 커진다.")]
    [SerializeField, Min(0)] private int perChapter = 40;

    [Header("마무리")]
    [Tooltip("마지막 챕터까지 끝냈을 때의 몫.")]
    [SerializeField, Min(0)] private int clearBonus = 200;
    [Tooltip("남은 크레딧을 잔존 코드로 바꾸는 비율. 0.1이면 10 크레딧에 1이다.")]
    [SerializeField, Range(0f, 1f)] private float creditConversion = 0.1f;

    [Header("바닥값")]
    [Tooltip("아무것도 못 하고 끝나도 이만큼은 남는다. 다시 시작할 이유를 남겨 둔다.")]
    [SerializeField, Min(0)] private int minimumAward = 5;

    public int PerRoom => perRoom;
    public int PerFloor => perFloor;
    public int PerChapter => perChapter;
    public int ClearBonus => clearBonus;
    public float CreditConversion => creditConversion;
    public int MinimumAward => minimumAward;
}

/// <summary>한 런이 남긴 잔존 코드의 내역. 결과 화면에 그대로 펴 보인다.</summary>
public struct SalvageBreakdown
{
    public int Rooms;
    public int Floors;
    public int Chapters;
    public int FromRooms;
    public int FromFloors;
    public int FromChapters;
    public int FromCredits;
    public int ClearBonus;
    /// <summary>개조로 붙은 배율이 더해 준 몫.</summary>
    public int RateBonus;
    public int Total;

    public int Subtotal =>
        FromRooms + FromFloors + FromChapters + FromCredits + ClearBonus;
}
