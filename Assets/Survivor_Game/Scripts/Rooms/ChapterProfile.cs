using System;
using UnityEngine;

/// <summary>한 챕터. 여러 층으로 이루어진다.</summary>
[CreateAssetMenu(
    fileName = "ChapterProfile_New",
    menuName = "Survivor/Rooms/Chapter Profile")]
public class ChapterProfile : ScriptableObject
{
    [SerializeField] private string displayName = "Chapter 1";
    [Tooltip("이 챕터의 층들. 순서대로 올라간다.")]
    [SerializeField] private FloorProfile[] floors = Array.Empty<FloorProfile>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? name : displayName;
    public int FloorCount => floors != null ? floors.Length : 0;

    public FloorProfile GetFloor(int index)
    {
        if (floors == null || index < 0 || index >= floors.Length) return null;
        return floors[index];
    }
}
