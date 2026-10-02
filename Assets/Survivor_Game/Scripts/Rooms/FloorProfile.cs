using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>한 층에서 방 종류별로 무엇이 몇 마리 나올지 정한다.</summary>
[Serializable]
public class FloorSpawnPlan
{
    [Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    [Header("일반 방")]
    [Min(0)] public int normalMinCount = 4;
    [Min(0)] public int normalMaxCount = 7;
    public Entry[] normalEnemies = Array.Empty<Entry>();

    [Header("엘리트 방")]
    [Min(0)] public int eliteMinCount = 2;
    [Min(0)] public int eliteMaxCount = 3;
    public Entry[] eliteEnemies = Array.Empty<Entry>();

    [Header("층 난이도")]
    [Tooltip("이 층 적의 최대 체력 배율. 1.5면 50% 더 단단해진다.")]
    [Min(0.1f)] public float healthMultiplier = 1f;
    [Tooltip("이 층 적의 방어력에 더할 값. 한 방 피해가 작을수록 크게 체감된다.")]
    [Min(0)] public int defenseBonus;
    [Tooltip("이 층 적이 주는 경험치 배율. 레벨업이 멈추지 않게 층마다 올린다.")]
    [Min(0.1f)] public float experienceMultiplier = 1f;
    [Tooltip("이 층에서 적 하나를 잡을 때 받는 재화. 상점 물가와 함께 올린다.")]
    [Min(0)] public int creditsPerEnemy = 4;
    [Tooltip("엘리트 방 적에게 추가로 곱하는 체력 배율.")]
    [Min(1f)] public float eliteHealthMultiplier = 1.6f;
    [Tooltip("보스에게 추가로 곱하는 체력 배율.")]
    [Min(1f)] public float bossHealthMultiplier = 1f;

    [Header("보스 방")]
    [Tooltip("이 층의 보스. 비어 있으면 보스 방이 즉시 클리어된다.")]
    public GameObject bossPrefab;

    /// <summary>방 종류에 맞는 난이도 보정을 만든다.</summary>
    public EnemyScaling ResolveScaling(RoomKind kind)
    {
        float health = healthMultiplier;
        if (kind == RoomKind.Elite) health *= Mathf.Max(1f, eliteHealthMultiplier);
        else if (kind == RoomKind.Boss) health *= Mathf.Max(1f, bossHealthMultiplier);

        return new EnemyScaling(health, defenseBonus, experienceMultiplier);
    }

    public int ResolveCount(RoomKind kind)
    {
        switch (kind)
        {
            case RoomKind.Normal:
                return UnityEngine.Random.Range(
                    normalMinCount, Mathf.Max(normalMinCount, normalMaxCount) + 1);
            case RoomKind.Elite:
                return UnityEngine.Random.Range(
                    eliteMinCount, Mathf.Max(eliteMinCount, eliteMaxCount) + 1);
            case RoomKind.Boss:
                return bossPrefab != null ? 1 : 0;
            default:
                return 0;
        }
    }

    public GameObject PickPrefab(RoomKind kind)
    {
        if (kind == RoomKind.Boss) return bossPrefab;
        Entry[] pool = kind == RoomKind.Elite ? eliteEnemies : normalEnemies;
        return PickWeighted(pool);
    }

    private static GameObject PickWeighted(Entry[] pool)
    {
        if (pool == null || pool.Length == 0) return null;

        float total = 0f;
        foreach (Entry entry in pool)
        {
            if (entry != null && entry.prefab != null) total += Mathf.Max(0f, entry.weight);
        }
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.value * total;
        foreach (Entry entry in pool)
        {
            if (entry == null || entry.prefab == null) continue;
            roll -= Mathf.Max(0f, entry.weight);
            if (roll <= 0f) return entry.prefab;
        }
        return null;
    }

    public void Normalize()
    {
        normalMinCount = Mathf.Max(0, normalMinCount);
        normalMaxCount = Mathf.Max(normalMinCount, normalMaxCount);
        eliteMinCount = Mathf.Max(0, eliteMinCount);
        eliteMaxCount = Mathf.Max(eliteMinCount, eliteMaxCount);
        healthMultiplier = Mathf.Max(0.1f, healthMultiplier);
        defenseBonus = Mathf.Max(0, defenseBonus);
        experienceMultiplier = Mathf.Max(0.1f, experienceMultiplier);
        creditsPerEnemy = Mathf.Max(0, creditsPerEnemy);
        eliteHealthMultiplier = Mathf.Max(1f, eliteHealthMultiplier);
        bossHealthMultiplier = Mathf.Max(1f, bossHealthMultiplier);
    }
}

/// <summary>한 층의 방 구성과 난이도. 층 생성기가 이걸 읽어 레이아웃을 만든다.</summary>
[CreateAssetMenu(
    fileName = "FloorProfile_New",
    menuName = "Survivor/Rooms/Floor Profile")]
public class FloorProfile : ScriptableObject
{
    [Serializable]
    public class RoomPool
    {
        public RoomKind kind = RoomKind.Normal;
        [Tooltip("이 종류로 쓸 방 프리팹들. 생성 시 하나를 무작위로 고른다.")]
        public RoomInstance[] prefabs = Array.Empty<RoomInstance>();
    }

    [Header("표시 이름")]
    [SerializeField] private string displayName = "1F";

    [Header("방 개수")]
    [Tooltip("시작 방과 보스 방을 포함한 총 방 수.")]
    [SerializeField, Min(3)] private int roomCount = 8;
    [Tooltip("전투 방 중 엘리트 방으로 승격할 개수.")]
    [SerializeField, Min(0)] private int eliteRoomCount = 1;
    [Tooltip("보물 방 개수. 전투가 없는 방이다.")]
    [SerializeField, Min(0)] private int treasureRoomCount = 1;
    [Tooltip("상점 방 개수. 전투가 없고, 재화를 내면 여러 개를 살 수 있다.")]
    [SerializeField, Min(0)] private int shopRoomCount = 1;

    [Header("배치")]
    [Tooltip("그리드 한 칸의 월드 크기. 이 층에 쓰는 방 프리팹들의 크기와 맞춰야 " +
        "방 사이가 벌어지거나 겹치지 않는다.")]
    [SerializeField] private Vector2 gridCellSize = new Vector2(48f, 32f);

    [Header("방 프리팹")]
    [SerializeField] private RoomPool[] roomPools = Array.Empty<RoomPool>();

    [Header("적 구성")]
    [SerializeField] private FloorSpawnPlan spawnPlan = new FloorSpawnPlan();

    [Header("보상 구성")]
    [SerializeField] private FloorRewardPlan rewardPlan = new FloorRewardPlan();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? name : displayName;
    public int RoomCount => Mathf.Max(3, roomCount);
    public int EliteRoomCount => Mathf.Max(0, eliteRoomCount);
    public int TreasureRoomCount => Mathf.Max(0, treasureRoomCount);
    public int ShopRoomCount => Mathf.Max(0, shopRoomCount);
    public Vector2 GridCellSize => new Vector2(
        Mathf.Max(1f, gridCellSize.x), Mathf.Max(1f, gridCellSize.y));
    public FloorSpawnPlan SpawnPlan => spawnPlan;
    public FloorRewardPlan RewardPlan => rewardPlan;

    /// <summary>해당 종류의 방 프리팹을 하나 고른다. 없으면 Normal 풀로 대체한다.</summary>
    public RoomInstance PickRoomPrefab(RoomKind kind)
    {
        RoomInstance picked = PickFromPool(kind);
        if (picked != null) return picked;
        return kind == RoomKind.Normal ? null : PickFromPool(RoomKind.Normal);
    }

    private RoomInstance PickFromPool(RoomKind kind)
    {
        foreach (RoomPool pool in roomPools)
        {
            if (pool == null || pool.kind != kind) continue;
            List<RoomInstance> valid = new List<RoomInstance>();
            foreach (RoomInstance prefab in pool.prefabs)
            {
                if (prefab != null) valid.Add(prefab);
            }
            if (valid.Count == 0) continue;
            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }
        return null;
    }

    private void OnValidate()
    {
        roomCount = Mathf.Max(3, roomCount);
        eliteRoomCount = Mathf.Clamp(eliteRoomCount, 0, Mathf.Max(0, roomCount - 2));
        treasureRoomCount = Mathf.Clamp(treasureRoomCount, 0, Mathf.Max(0, roomCount - 2));
        shopRoomCount = Mathf.Clamp(shopRoomCount, 0, Mathf.Max(0, roomCount - 2));
        if (spawnPlan == null) spawnPlan = new FloorSpawnPlan();
        spawnPlan.Normalize();
        if (rewardPlan == null) rewardPlan = new FloorRewardPlan();
        rewardPlan.Normalize();
    }
}
