using System;
using System.Collections.Generic;

/// <summary>개조 항목 하나의 현재 단계. 저장 파일에 그대로 적힌다.</summary>
[Serializable]
public struct MetaNodeLevel
{
    public string id;
    public int level;
}

/// <summary>
/// 런과 런 사이에 남는 것 전부. JsonUtility로 그대로 저장하므로
/// 필드 이름을 바꾸면 예전 저장이 깨진다. 이름은 그대로 두고 늘리기만 한다.
/// </summary>
[Serializable]
public class MetaProgressState
{
    /// <summary>저장 형식 번호. 나중에 형식을 바꿀 때 갈아 끼우는 기준이 된다.</summary>
    public int version = 1;

    /// <summary>지금 쓸 수 있는 잔존 코드.</summary>
    public int salvage;
    /// <summary>여태 모은 총량. 통계용이라 쓰더라도 줄지 않는다.</summary>
    public int lifetimeSalvage;

    public int runCount;
    public int clearCount;
    /// <summary>가장 멀리 간 지점. 1부터 센다. 0이면 아직 기록이 없다.</summary>
    public int bestChapter;
    public int bestFloor;

    public List<MetaNodeLevel> nodes = new List<MetaNodeLevel>();

    public int GetLevel(string id)
    {
        if (string.IsNullOrEmpty(id) || nodes == null) return 0;

        foreach (MetaNodeLevel entry in nodes)
        {
            if (entry.id == id) return entry.level;
        }
        return 0;
    }

    public void SetLevel(string id, int level)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (nodes == null) nodes = new List<MetaNodeLevel>();

        for (int index = 0; index < nodes.Count; index++)
        {
            if (nodes[index].id != id) continue;

            MetaNodeLevel entry = nodes[index];
            entry.level = level;
            nodes[index] = entry;
            return;
        }

        nodes.Add(new MetaNodeLevel { id = id, level = level });
    }

    /// <summary>이번 런이 여태 기록보다 멀리 갔는지 보고, 그렇다면 갈아 끼운다.</summary>
    public bool RecordDepth(int chapter, int floor)
    {
        if (chapter < bestChapter) return false;
        if (chapter == bestChapter && floor <= bestFloor) return false;

        bestChapter = chapter;
        bestFloor = floor;
        return true;
    }
}
