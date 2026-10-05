using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배치된 방 하나. 플레이어가 처음 들어오면 문을 잠그고 적을 스폰하며,
/// 그 방의 적이 전멸하면 문을 열고 클리어 처리한다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Rooms/Room Instance")]
public class RoomInstance : MonoBehaviour
{
    [Header("방 정보")]
    [SerializeField] private RoomKind kind = RoomKind.Normal;
    [Tooltip("층 생성기가 방을 배치할 때 쓰는 그리드 한 칸의 월드 크기.")]
    [SerializeField] private Vector2 cellSize = new Vector2(48f, 32f);
    [Tooltip("같은 지형 디자인을 공유하는 프리팹은 같은 ID를 사용한다. 비어 있으면 프리팹 이름을 쓴다.")]
    [SerializeField] private string designId;
    [Tooltip("다음 스테이지 출구 위치. 방 중심 기준이며 보상 자리와 떨어뜨린다.")]
    [SerializeField] private Vector2 floorExitOffset = new Vector2(0f, -8f);
    [Header("보스 선택")]
    [SerializeField] private Vector2 bossChoiceOffset = new Vector2(0f, -3f);
    [SerializeField, Min(6f)] private float bossChoiceSpacing = 10f;

    [Header("구성 요소")]
    [SerializeField] private RoomDoor[] doors = Array.Empty<RoomDoor>();
    [Tooltip("적이 나타날 후보 지점. 비어 있으면 방 중심을 쓴다.")]
    [SerializeField] private Transform[] spawnPoints = Array.Empty<Transform>();
    [Tooltip("플레이어 입장을 감지하는 트리거 콜라이더.")]
    [SerializeField] private Collider2D playerDetector;

    [Tooltip("클리어 보상 받침대가 놓일 자리. 비어 있으면 방 중심에 나란히 놓는다.")]
    [SerializeField] private Transform[] rewardAnchors = Array.Empty<Transform>();

    [Header("스폰 안전 거리")]
    [Tooltip("플레이어가 들어온 지점에서 이 거리 안에는 적을 두지 않는다. " +
        "모든 스폰 지점이 이보다 가까우면 플레이어 반대쪽으로 밀어낸다.")]
    [SerializeField, Min(0f)] private float minimumSpawnDistanceFromPlayer = 8f;

    /// <summary>방을 클리어한 순간 호출된다.</summary>
    public event Action<RoomInstance> OnRoomCleared;
    /// <summary>플레이어가 이 방에 처음 들어온 순간 호출된다.</summary>
    public event Action<RoomInstance> OnRoomEntered;

    private readonly List<EnemyHealth> livingEnemies = new List<EnemyHealth>();
    private readonly List<RewardPedestal> pedestals = new List<RewardPedestal>();
    private Vector2 lastKillPosition;
    private bool hasKillPosition;
    private RoomRuntimeContext context;
    private bool entered;
    private bool cleared;
    private bool combatActive;
    private bool bossChosen;
    private readonly List<BossChoicePedestal> bossChoices = new List<BossChoicePedestal>();

    public RoomKind Kind => kind;
    public string DesignId => string.IsNullOrWhiteSpace(designId) ? name : designId;
    public Vector3 FloorExitPosition => transform.TransformPoint((Vector3)floorExitOffset);
    public Vector2 CellSize => cellSize;
    public Vector2Int GridPosition { get; private set; }
    public IReadOnlyList<RoomDoor> Doors => doors;
    public bool IsCleared => cleared;
    public bool IsCombatActive => combatActive;
    /// <summary>아직 가져가지 않은 보상 받침대가 남아 있는가.</summary>
    public bool HasPendingRewards => pedestals.Count > 0;
    /// <summary>전투가 없는 방(시작·보물·상점)은 들어가자마자 클리어로 친다.</summary>
    public bool IsCombatRoom =>
        kind == RoomKind.Normal || kind == RoomKind.Elite || kind == RoomKind.Boss;

    public void Initialize(Vector2Int gridPosition, RoomRuntimeContext runtimeContext)
    {
        GridPosition = gridPosition;
        context = runtimeContext;
        entered = false;
        cleared = false;
        combatActive = false;
        bossChosen = false;
        livingEnemies.Clear();

        foreach (RoomDoor door in doors)
        {
            if (door != null) door.Initialize(this);
        }
    }

    /// <summary>층 생성기가 배치를 끝낸 뒤, 이어지지 않은 문을 확정하기 위해 호출한다.</summary>
    public void FinalizeDoors()
    {
        // 연결된 문은 입장 전에 열어 둬야 안쪽 감지 영역에 도달할 수 있다.
        // 전투가 시작되면 EnterRoom에서 잠그고, 이웃이 없는 문은 봉인을 유지한다.
        SetDoorsOpen(!combatActive);
    }

    /// <summary>스테이지 시작방은 트리거 재진입 여부에 의존하지 않고 입장을 확정한다.</summary>
    public void EnterFromStageTransition(Vector2 playerPosition)
    {
        if (!entered) EnterRoom(playerPosition);
    }

    public RoomDoor GetDoor(RoomDirection direction)
    {
        foreach (RoomDoor door in doors)
        {
            if (door != null && door.Direction == direction) return door;
        }
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (entered || !other.CompareTag("Player")) return;
        EnterRoom(other.transform.position);
    }

    private void EnterRoom(Vector2 entryPosition)
    {
        entered = true;
        OnRoomEntered?.Invoke(this);

        if (!IsCombatRoom)
        {
            MarkCleared();
            return;
        }

        combatActive = true;
        SetDoorsOpen(false);
        if (kind == RoomKind.Boss)
        {
            ShowBossChoices();
            return;
        }
        SpawnEnemies(entryPosition);

        // 스폰할 적이 하나도 없으면 즉시 클리어한다.
        if (livingEnemies.Count == 0) MarkCleared();
    }

    private void ShowBossChoices()
    {
        if (context == null || context.BossChoicePrefab == null || context.BossCandidates == null)
        {
            Debug.LogError("보스 선택 프리팹과 맵별 후보가 필요합니다.", this);
            return;
        }
        for (int index = 0; index < context.BossCandidates.Count; index++)
        {
            Vector2 offset = bossChoiceOffset + Vector2.right *
                ((index - (context.BossCandidates.Count - 1) * .5f) * bossChoiceSpacing);
            BossChoicePedestal choice = Instantiate(context.BossChoicePrefab,
                transform.TransformPoint((Vector3)offset), Quaternion.identity, transform);
            choice.Configure(this, context.BossCandidates[index], context.Player);
            bossChoices.Add(choice);
        }
    }

    public bool TryChooseBoss(GameObject prefab)
    {
        if (!isActiveAndEnabled || kind != RoomKind.Boss || !entered || cleared || bossChosen ||
            context == null || context.RunManager == null || !context.RunManager.isActiveAndEnabled ||
            context.RunManager.IsRunOver || context.Spawner == null || context.SpawnPlan == null ||
            context.Player == null || Time.timeScale <= 0f || GameInputKeys.IsGameplayBlocked) return false;
        bool offered = false;
        if (context.BossCandidates != null)
            foreach (GameObject candidate in context.BossCandidates)
                if (candidate != null && candidate == prefab) offered = true;
        if (!offered) return false;

        List<Vector2> points = BuildSpawnPositions();
        Shuffle(points);
        Vector2 entry = context.Player.position;
        OrderAwayFromEntry(points, entry);
        Vector2 position = PushAwayFromEntry(ResolveSpawnPosition(points, 0), entry);
        EnemyHealth enemy = context.Spawner.SpawnForRoom(prefab, position,
            context.SpawnPlan.ResolveScaling(RoomKind.Boss), EnemyRank.Boss);
        if (enemy == null)
        {
            Debug.LogError("선택한 보스 생성에 실패했습니다. 선택을 다시 시도할 수 있습니다.", this);
            return false;
        }
        bossChosen = true;
        Track(enemy);
        foreach (BossChoicePedestal choice in bossChoices)
            if (choice != null)
            {
                choice.gameObject.SetActive(false);
                Destroy(choice.gameObject);
            }
        bossChoices.Clear();
        return true;
    }

    private void SpawnEnemies(Vector2 entryPosition)
    {
        if (context == null || context.Spawner == null || context.SpawnPlan == null) return;

        List<Vector2> points = BuildSpawnPositions();
        Shuffle(points);
        OrderAwayFromEntry(points, entryPosition);

        int spawnCount = context.SpawnPlan.ResolveCount(kind);
        EnemyScaling scaling = context.SpawnPlan.ResolveScaling(kind);
        for (int index = 0; index < spawnCount; index++)
        {
            GameObject prefab = context.SpawnPlan.PickPrefab(kind);
            if (prefab == null) continue;

            Vector2 position = ResolveSpawnPosition(points, index);
            position = PushAwayFromEntry(position, entryPosition);
            EnemyHealth spawned = context.Spawner.SpawnForRoom(prefab, position, scaling,
                kind == RoomKind.Elite ? EnemyRank.Elite : EnemyRank.Normal);
            if (spawned != null) Track(spawned);
        }
    }

    /// <summary>플레이어가 들어온 지점에서 먼 스폰 지점을 앞쪽으로 모은다.</summary>
    private void OrderAwayFromEntry(List<Vector2> points, Vector2 entryPosition)
    {
        if (minimumSpawnDistanceFromPlayer <= 0f) return;

        List<Vector2> far = new List<Vector2>();
        List<Vector2> near = new List<Vector2>();
        foreach (Vector2 point in points)
        {
            if (Vector2.Distance(point, entryPosition) >= minimumSpawnDistanceFromPlayer)
            {
                far.Add(point);
            }
            else near.Add(point);
        }

        points.Clear();
        points.AddRange(far);
        points.AddRange(near);
    }

    /// <summary>그래도 너무 가까우면 플레이어 반대 방향으로 밀어낸다.</summary>
    private Vector2 PushAwayFromEntry(Vector2 position, Vector2 entryPosition)
    {
        if (minimumSpawnDistanceFromPlayer <= 0f) return position;

        Vector2 offset = position - entryPosition;
        float distance = offset.magnitude;
        if (distance >= minimumSpawnDistanceFromPlayer) return position;

        Vector2 direction = distance > 0.01f
            ? offset / distance
            : UnityEngine.Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;

        Vector2 pushed = entryPosition + direction * minimumSpawnDistanceFromPlayer;
        // 방 밖으로 나가지 않도록 방 내부로 되돌린다
        Vector2 center = transform.position;
        Vector2 half = cellSize * 0.5f - Vector2.one * 3f;
        pushed.x = Mathf.Clamp(pushed.x, center.x - half.x, center.x + half.x);
        pushed.y = Mathf.Clamp(pushed.y, center.y - half.y, center.y + half.y);
        return pushed;
    }

    /// <summary>
    /// 스폰 지점을 겹치지 않게 순서대로 쓴다. 적이 지점보다 많으면
    /// 한 바퀴 더 돌되 조금씩 흩어 놓아 완전히 포개지지 않게 한다.
    /// </summary>
    private Vector2 ResolveSpawnPosition(List<Vector2> points, int index)
    {
        if (points.Count == 0) return transform.position;

        Vector2 position = points[index % points.Count];
        int lap = index / points.Count;
        if (lap > 0)
        {
            float spread = 1.2f * lap;
            position += new Vector2(
                UnityEngine.Random.Range(-spread, spread),
                UnityEngine.Random.Range(-spread, spread));
        }
        return position;
    }

    private static void Shuffle(List<Vector2> points)
    {
        for (int index = points.Count - 1; index > 0; index--)
        {
            int swap = UnityEngine.Random.Range(0, index + 1);
            Vector2 temp = points[index];
            points[index] = points[swap];
            points[swap] = temp;
        }
    }

    private List<Vector2> BuildSpawnPositions()
    {
        List<Vector2> points = new List<Vector2>();
        foreach (Transform point in spawnPoints)
        {
            if (point != null) points.Add(point.position);
        }
        return points;
    }

    private void Track(EnemyHealth enemy)
    {
        livingEnemies.Add(enemy);
        enemy.OnDied += HandleEnemyDied;
    }

    private void HandleEnemyDied()
    {
        // 어떤 적이 죽었는지 이벤트가 알려주지 않으므로, 죽었거나 사라진 것을 걷어낸다.
        int removed = 0;
        for (int index = livingEnemies.Count - 1; index >= 0; index--)
        {
            EnemyHealth enemy = livingEnemies[index];
            if (enemy == null || enemy.GetCurrentHealth() <= 0)
            {
                if (enemy != null)
                {
                    enemy.OnDied -= HandleEnemyDied;
                    // 보상을 여기서 던져야 처치와 이어 보인다.
                    lastKillPosition = enemy.transform.position;
                    hasKillPosition = true;
                }
                livingEnemies.RemoveAt(index);
                removed++;
            }
        }
        AwardCredits(removed);

        if (combatActive && livingEnemies.Count == 0) MarkCleared();
    }

    /// <summary>잡은 적 수만큼 재화를 준다. 상점 방에서 쓸 밑천이 여기서 나온다.</summary>
    private void AwardCredits(int killedCount)
    {
        if (killedCount <= 0 || context == null ||
            context.Wallet == null || context.SpawnPlan == null)
        {
            return;
        }
        context.Wallet.Add(killedCount * context.SpawnPlan.creditsPerEnemy);
    }

    private void MarkCleared()
    {
        if (cleared) return;
        cleared = true;
        combatActive = false;
        SetDoorsOpen(true);
        SpawnDrops();
        SpawnRewards();
        OnRoomCleared?.Invoke(this);
    }

    /// <summary>
    /// 고르는 보상과 별개로, 바닥에 떨어뜨리는 보상을 뿌린다.
    /// 마지막으로 쓰러진 적 자리에서 튀어나오게 해 처치와 이어 보이게 한다.
    /// </summary>
    private void SpawnDrops()
    {
        if (context == null || context.RewardPlan == null ||
            context.DropPrefab == null)
        {
            return;
        }

        IReadOnlyList<RoomRewardDefinition> drops = context.RewardPlan.GetDrops(kind);
        if (drops.Count == 0) return;

        Vector2 origin = hasKillPosition ? lastKillPosition : (Vector2)transform.position;
        for (int index = 0; index < drops.Count; index++)
        {
            RoomRewardDefinition reward = drops[index];
            if (reward == null || !IsRewardAllowed(reward)) continue;

            RewardDrop drop = Instantiate(
                context.DropPrefab, origin, Quaternion.identity, transform);
            drop.Configure(reward, origin, ResolveDropLanding(origin, index, drops.Count));
        }
    }

    /// <summary>여러 개가 겹치지 않게 처치 자리 둘레로 흩어 놓는다.</summary>
    private Vector2 ResolveDropLanding(Vector2 origin, int index, int total)
    {
        const float radius = 3.5f;
        float angle = total <= 1
            ? 90f * Mathf.Deg2Rad
            : (index / (float)total) * Mathf.PI * 2f;
        Vector2 landing = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        // 벽 너머로 던지지 않도록 방 안으로 되돌린다.
        Vector2 center = transform.position;
        Vector2 half = cellSize * 0.5f - Vector2.one * 3f;
        landing.x = Mathf.Clamp(landing.x, center.x - half.x, center.x + half.x);
        landing.y = Mathf.Clamp(landing.y, center.y - half.y, center.y + half.y);
        return landing;
    }

    /// <summary>클리어 보상을 받침대로 놓는다. 여러 개면 그중 하나만 가져갈 수 있다.</summary>
    private void SpawnRewards()
    {
        if (context == null || context.RewardPlan == null ||
            context.PedestalPrefab == null)
        {
            return;
        }

        int count = context.RewardPlan.ResolveChoiceCount(kind);
        // 선택지를 늘려 주는 개조가 있으면 그만큼 더 놓는다.
        // 보상이 없는 방까지 생기지 않도록 0인 방은 그대로 둔다.
        if (count > 0) count += Mathf.Max(0, MetaProgressRuntime.Bonuses.RewardChoiceBonus);
        if (count <= 0) return;

        bool purchasable = context.RewardPlan.IsPurchasable(kind);

        List<RoomRewardDefinition> chosen = new List<RoomRewardDefinition>(count);
        for (int index = 0; index < count; index++)
        {
            RoomRewardDefinition reward = context.RewardPlan.Pick(kind, chosen, IsRewardAllowed);
            if (reward == null) break;
            chosen.Add(reward);
        }
        if (chosen.Count == 0) return;

        for (int index = 0; index < chosen.Count; index++)
        {
            Vector2 position = ResolveRewardPosition(index, chosen.Count);
            RewardPedestal pedestal = Instantiate(
                context.PedestalPrefab, position, Quaternion.identity, transform);
            int price = ApplyShopDiscount(
                context.RewardPlan.ResolvePrice(kind, chosen[index]));
            pedestal.Configure(chosen[index], this, price, !purchasable);
            pedestals.Add(pedestal);
        }
    }

    private bool IsRewardAllowed(RoomRewardDefinition reward) =>
        context.WeaponEquipment == null || context.WeaponEquipment.CanOfferReward(reward);

    /// <summary>영구 개조의 할인을 값에 반영한다. 공짜가 되지는 않는다.</summary>
    private static int ApplyShopDiscount(int price)
    {
        if (price <= 0) return 0;

        float discount = MetaProgressRuntime.Bonuses.ShopDiscountRate;
        if (discount <= 0f) return price;
        return Mathf.Max(1, Mathf.RoundToInt(price * (1f - discount)));
    }

    /// <summary>받침대 자리를 정한다. 지정된 자리가 모자라면 방 중심에 나란히 편다.</summary>
    private Vector2 ResolveRewardPosition(int index, int total)
    {
        if (rewardAnchors != null && index < rewardAnchors.Length &&
            rewardAnchors[index] != null)
        {
            return rewardAnchors[index].position;
        }

        const float spacing = 5f;
        float offset = (index - (total - 1) * 0.5f) * spacing;
        Vector2 center = transform.position;
        return new Vector2(center.x + offset, center.y);
    }

    /// <summary>
    /// 받침대 하나를 가져갔을 때 호출된다. 배타적인 방(보물·엘리트·보스)이면
    /// 나머지 선택지를 거두고, 상점이면 산 것만 빼고 나머지는 남긴다.
    /// </summary>
    public void NotifyRewardClaimed(RewardPedestal claimed, bool exclusive)
    {
        if (!exclusive)
        {
            pedestals.Remove(claimed);
            return;
        }

        for (int index = pedestals.Count - 1; index >= 0; index--)
        {
            RewardPedestal pedestal = pedestals[index];
            if (pedestal != null && pedestal != claimed) Destroy(pedestal.gameObject);
        }
        pedestals.Clear();
    }

    private void SetDoorsOpen(bool open)
    {
        foreach (RoomDoor door in doors)
        {
            if (door == null) continue;
            if (open) door.Open();
            else door.Close();
        }
    }

    private void OnDisable()
    {
        foreach (EnemyHealth enemy in livingEnemies)
        {
            if (enemy != null) enemy.OnDied -= HandleEnemyDied;
        }
        livingEnemies.Clear();
    }

    private void OnValidate()
    {
        cellSize.x = Mathf.Max(1f, cellSize.x);
        cellSize.y = Mathf.Max(1f, cellSize.y);
        if (playerDetector != null && !playerDetector.isTrigger)
        {
            Debug.LogWarning(
                "Player Detector는 Is Trigger가 켜져 있어야 방 입장을 감지합니다.", this);
        }
    }
}

/// <summary>방이 스폰할 때 필요한 런타임 참조 묶음.</summary>
public class RoomRuntimeContext
{
    public PlayerWeaponEquipment WeaponEquipment;
    public RunManager RunManager;
    public BossChoicePedestal BossChoicePrefab;
    public IReadOnlyList<GameObject> BossCandidates;
    public FloorExit ExitPrefab;
    public EnemySpawner Spawner;
    public FloorSpawnPlan SpawnPlan;
    public FloorRewardPlan RewardPlan;
    public RewardPedestal PedestalPrefab;
    public PlayerWallet Wallet;
    public Transform Player;
    public RewardDrop DropPrefab;
}
