using System;
using UnityEngine;

/// <summary>런 전체 구성. 챕터를 순서대로 진행한다.</summary>
[CreateAssetMenu(
    fileName = "RunProfile_New",
    menuName = "Survivor/Rooms/Run Profile")]
public class RunProfile : ScriptableObject
{
    [Tooltip("순서대로 진행할 챕터들.")]
    [SerializeField] private ChapterProfile[] chapters = Array.Empty<ChapterProfile>();

    [Header("보상")]
    [Tooltip("방 보상이 놓일 받침대 프리팹. 층마다 같은 것을 쓴다.")]
    [SerializeField] private RewardPedestal rewardPedestalPrefab;
    [Tooltip("바닥에 떨어지는 보상 프리팹. 층마다 같은 것을 쓴다.")]
    [SerializeField] private RewardDrop rewardDropPrefab;

    [Header("층 출구")]
    [SerializeField] private FloorExit floorExitPrefab;
    [SerializeField] private BossChoicePedestal bossChoicePrefab;

    [Header("스테이지 전환")]
    [SerializeField] private StageTransitionUI stageTransitionUIPrefab;
    public StageTransitionUI StageTransitionUIPrefab => stageTransitionUIPrefab;

    [Header("보존된 단일 방 모드 설정 (현재 런에서 사용하지 않음)")]
    [SerializeField] private RoomChoiceUI roomChoiceUIPrefab;
    [SerializeField, Range(2, 3)] private int minimumRoomChoices = 2;
    [SerializeField, Range(2, 3)] private int maximumRoomChoices = 3;
    [Tooltip("엘리트가 처음 후보로 나오는 방 번호. 첫 두 방에는 나오지 않는다.")]
    [SerializeField, Min(3)] private int firstEliteStage = 3;

    public int ChapterCount => chapters != null ? chapters.Length : 0;
    public RewardPedestal RewardPedestalPrefab => rewardPedestalPrefab;
    public RewardDrop RewardDropPrefab => rewardDropPrefab;
    public FloorExit FloorExitPrefab => floorExitPrefab;
    public BossChoicePedestal BossChoicePrefab => bossChoicePrefab;
    public RoomChoiceUI RoomChoiceUIPrefab => roomChoiceUIPrefab;
    public int MinimumRoomChoices => Mathf.Clamp(minimumRoomChoices, 2, 3);
    public int MaximumRoomChoices => Mathf.Clamp(maximumRoomChoices, MinimumRoomChoices, 3);
    public int FirstEliteStage => Mathf.Max(3, firstEliteStage);

    public ChapterProfile GetChapter(int index)
    {
        if (chapters == null || index < 0 || index >= chapters.Length) return null;
        return chapters[index];
    }

    /// <summary>챕터·층 인덱스를 받아 해당 층을 돌려준다. 끝이면 null.</summary>
    public FloorProfile GetFloor(int chapterIndex, int floorIndex)
    {
        ChapterProfile chapter = GetChapter(chapterIndex);
        return chapter != null ? chapter.GetFloor(floorIndex) : null;
    }
}
