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

    public int ChapterCount => chapters != null ? chapters.Length : 0;
    public RewardPedestal RewardPedestalPrefab => rewardPedestalPrefab;
    public RewardDrop RewardDropPrefab => rewardDropPrefab;

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
