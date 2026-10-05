using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>한 챕터. 여러 층으로 이루어진다.</summary>
[CreateAssetMenu(
    fileName = "ChapterProfile_New",
    menuName = "Survivor/Rooms/Chapter Profile")]
public class ChapterProfile : ScriptableObject
{
    [SerializeField] private string displayName = "Chapter 1";
    [Tooltip("보스전 전에 순서대로 진행할 일반 스테이지 4~5개.")]
    [SerializeField] private FloorProfile[] floors = Array.Empty<FloorProfile>();
    [SerializeField] private FloorProfile bossFloor;
    [Tooltip("이 맵에서 선택할 보스 1~3종. 플레이어가 한 종만 선택한다.")]
    [SerializeField] private GameObject[] bossCandidates = Array.Empty<GameObject>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? name : displayName;
    public int OrdinaryFloorCount => floors != null ? floors.Length : 0;
    public int FloorCount => OrdinaryFloorCount + 1;
    public IReadOnlyList<GameObject> BossCandidates => bossCandidates;

    public FloorProfile GetFloor(int index)
    {
        if (index == OrdinaryFloorCount) return bossFloor;
        if (floors == null || index < 0 || index >= floors.Length) return null;
        return floors[index];
    }

    public bool IsValid(out string error)
    {
        error = null;
        if (OrdinaryFloorCount < 4 || OrdinaryFloorCount > 5)
            error = "일반 스테이지는 4~5개여야 합니다.";
        else if (bossFloor == null || !bossFloor.IsBossStage)
            error = "별도의 보스 스테이지 프로필이 필요합니다.";
        else if (bossCandidates == null || bossCandidates.Length < 1 || bossCandidates.Length > 3)
            error = "보스 후보는 1~3종이어야 합니다.";
        if (error != null) return false;
        foreach (FloorProfile floor in floors)
            if (floor == null || floor.IsBossStage)
            {
                error = "일반 스테이지 목록에 비어 있거나 보스인 프로필이 있습니다.";
                return false;
            }
        var seen = new HashSet<GameObject>();
        foreach (GameObject candidate in bossCandidates)
        {
            EnemyHealth health = candidate != null ? candidate.GetComponent<EnemyHealth>() : null;
            if (health == null || health.Profile == null || health.Profile.Rank != EnemyRank.Boss || !seen.Add(candidate))
            {
                error = "보스 후보는 중복 없이 EnemyHealth와 보스 등급 프로필을 가져야 합니다.";
                return false;
            }
        }
        return true;
    }
}
