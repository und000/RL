using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 영구 진행을 읽고 쓰는 곳. 저장 파일은 persistentDataPath에 하나만 두며,
/// 처음 읽을 때 한 번 불러온 뒤로는 메모리에 들고 있다가 바뀔 때마다 적는다.
/// </summary>
public static class MetaProgress
{
    private const string FileName = "meta_progress.json";

    /// <summary>잔존 코드나 개조 단계가 바뀔 때마다 호출된다.</summary>
    public static event Action OnChanged;

    private static MetaProgressState state;

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static MetaProgressState State
    {
        get
        {
            if (state == null) Load();
            return state;
        }
    }

    public static int Salvage => State.salvage;
    public static WeaponFamily SelectedWeaponFamily => (WeaponFamily)State.selectedWeaponFamily;

    public static bool SelectWeaponFamily(WeaponFamily family)
    {
        if (WeaponFamilyUtility.NumberBase(family) == 0) return false;
        State.selectedWeaponFamily = (int)family;
        Save();
        OnChanged?.Invoke();
        return true;
    }

    public static void Load()
    {
        state = ReadFromDisk() ?? new MetaProgressState();
    }

    private static MetaProgressState ReadFromDisk()
    {
        string path = FilePath;
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            MetaProgressState loaded = JsonUtility.FromJson<MetaProgressState>(json);
            // 빈 파일이나 다른 형식이면 null이 돌아온다. 그때는 새로 시작한다.
            if (loaded != null && loaded.nodes == null)
            {
                loaded.nodes = new System.Collections.Generic.List<MetaNodeLevel>();
            }
            return loaded;
        }
        catch (Exception error)
        {
            Debug.LogWarning(
                "영구 진행을 읽지 못해 새로 시작합니다: " + error.Message);
            return null;
        }
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(State, true));
        }
        catch (Exception error)
        {
            Debug.LogWarning("영구 진행을 저장하지 못했습니다: " + error.Message);
        }
    }

    public static int GetLevel(MetaUpgradeNode node) =>
        node != null ? State.GetLevel(node.Id) : 0;

    public static bool IsMaxed(MetaUpgradeNode node) =>
        node != null && GetLevel(node) >= node.MaxLevel;

    public static bool IsAvailable(MetaUpgradeNode node) =>
        node != null && node.IsAvailable(GetLevel);

    /// <summary>
    /// 지금 한 단계 올릴 수 있는가. 열려 있고, 최고 단계가 아니고,
    /// 값을 치를 수 있어야 한다.
    /// </summary>
    public static bool CanRaise(MetaUpgradeNode node)
    {
        if (node == null || IsMaxed(node) || !IsAvailable(node)) return false;
        return State.salvage >= node.CostToRaise(GetLevel(node));
    }

    /// <summary>한 단계 올린다. 올렸으면 true. 값을 치를 수 없으면 아무것도 하지 않는다.</summary>
    public static bool TryRaise(MetaUpgradeNode node)
    {
        if (!CanRaise(node)) return false;

        int level = GetLevel(node);
        State.salvage -= node.CostToRaise(level);
        State.SetLevel(node.Id, level + 1);
        Save();
        OnChanged?.Invoke();
        return true;
    }

    public static void AddSalvage(int amount)
    {
        if (amount <= 0) return;

        State.salvage += amount;
        State.lifetimeSalvage += amount;
        Save();
        OnChanged?.Invoke();
    }

    /// <summary>런이 끝났을 때의 기록. 도달 지점은 1부터 세어 넘긴다.</summary>
    public static void RecordRun(int chapter, int floor, bool cleared)
    {
        MetaProgressState current = State;
        current.runCount++;
        if (cleared) current.clearCount++;
        current.RecordDepth(chapter, floor);
        Save();
        OnChanged?.Invoke();
    }

    /// <summary>개조를 전부 되돌린다. 쓴 값은 돌려주지 않는다.</summary>
    public static void ResetAll()
    {
        state = new MetaProgressState();
        Save();
        OnChanged?.Invoke();
    }
}
