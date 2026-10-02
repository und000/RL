using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 런 전체 진행을 관리한다. 챕터 &gt; 층 &gt; 방 순서로 내려가며,
/// 보스 방을 클리어하면 다음 층을 생성하고 플레이어를 시작 방으로 옮긴다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Rooms/Run Manager")]
public class RunManager : MonoBehaviour
{
    [Header("런 구성")]
    [SerializeField] private RunProfile runProfile;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private Transform player;
    [Tooltip("생성된 층이 들어갈 부모. 비어 있으면 이 오브젝트 아래에 만든다.")]
    [SerializeField] private Transform floorRoot;

    [Header("진행")]
    [Tooltip("보스를 잡고 다음 층으로 넘어가기 전 잠깐 두는 여유 시간.")]
    [SerializeField, Min(0f)] private float floorTransitionDelay = 1.5f;
    [Tooltip("켜면 보스 방 보상을 가져갈 때까지 다음 층으로 넘어가지 않는다.")]
    [SerializeField] private bool waitForBossReward = true;
    [Tooltip("보상을 기다리는 최대 시간. 체력이 가득 차 회복 보상을 받을 수 없는 등 " +
        "끝내 가져갈 수 없는 상황에서 층이 영원히 멈추지 않게 하는 안전장치다.")]
    [SerializeField, Min(1f)] private float maximumBossRewardWait = 60f;

    /// <summary>새 층이 만들어진 직후.</summary>
    public event Action<int, int, FloorProfile> OnFloorStarted;
    /// <summary>보스를 잡아 층을 클리어한 순간.</summary>
    public event Action<int, int> OnFloorCleared;
    /// <summary>방 하나를 클리어할 때마다. 진행도 표시에 쓴다.</summary>
    public event Action<RoomInstance> OnRoomCleared;
    /// <summary>마지막 챕터까지 끝낸 순간.</summary>
    public event Action OnRunCompleted;
    /// <summary>플레이어가 쓰러져 런이 끝난 순간.</summary>
    public event Action OnRunFailed;

    private RoomRuntimeContext context;
    private GeneratedFloor currentFloor;
    private FloorProfile currentProfile;
    private PlayerHealth playerHealth;
    private int chapterIndex;
    private int floorIndex;
    private bool transitioning;
    private bool runOver;

    public int ChapterIndex => chapterIndex;
    public int FloorIndex => floorIndex;
    public GeneratedFloor CurrentFloor => currentFloor;
    public FloorProfile CurrentProfile => currentProfile;
    public bool IsRunOver => runOver;

    /// <summary>이 층의 방 수. 진행도 표시에 쓴다.</summary>
    public int TotalRoomCount => currentFloor != null ? currentFloor.Rooms.Count : 0;
    public int ClearedRoomCount => currentFloor != null ? currentFloor.ClearedCount : 0;

    public string ChapterDisplayName
    {
        get
        {
            ChapterProfile chapter = runProfile != null
                ? runProfile.GetChapter(chapterIndex) : null;
            return chapter != null ? chapter.DisplayName : string.Empty;
        }
    }

    public string FloorDisplayName =>
        currentProfile != null ? currentProfile.DisplayName : string.Empty;

    private void Start()
    {
        if (runProfile == null || spawner == null)
        {
            Debug.LogError("RunManager에 Run Profile과 Spawn Director가 필요합니다.", this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }
        if (floorRoot == null) floorRoot = transform;

        WatchPlayerDeath();

        context = new RoomRuntimeContext
        {
            Spawner = spawner,
            PedestalPrefab = runProfile.RewardPedestalPrefab,
            DropPrefab = runProfile.RewardDropPrefab,
            Wallet = player != null ? player.GetComponentInChildren<PlayerWallet>() : null,
            Player = player
        };
        BeginRun();
    }

    private void WatchPlayerDeath()
    {
        if (player == null) return;

        playerHealth = player.GetComponentInChildren<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnDied += HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (runOver) return;

        runOver = true;
        StopAllCoroutines();
        CancelInvoke();
        OnRunFailed?.Invoke();
    }

    public void BeginRun()
    {
        chapterIndex = 0;
        floorIndex = 0;
        runOver = false;
        BuildCurrentFloor();
    }

    private void BuildCurrentFloor()
    {
        FloorProfile profile = runProfile.GetFloor(chapterIndex, floorIndex);
        if (profile == null)
        {
            Debug.LogError(
                $"챕터 {chapterIndex}, 층 {floorIndex}에 FloorProfile이 없습니다.", this);
            return;
        }

        ClearCurrentFloor();
        currentProfile = profile;
        context.SpawnPlan = profile.SpawnPlan;
        context.RewardPlan = profile.RewardPlan;
        currentFloor = FloorGenerator.Generate(profile, context, floorRoot);
        if (currentFloor == null) return;

        foreach (RoomInstance room in currentFloor.Rooms)
        {
            room.OnRoomCleared += HandleRoomCleared;
        }

        MovePlayerToStart();
        transitioning = false;
        OnFloorStarted?.Invoke(chapterIndex, floorIndex, profile);
    }

    private void MovePlayerToStart()
    {
        if (player == null || currentFloor == null || currentFloor.StartRoom == null) return;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector3 target = currentFloor.StartRoom.transform.position;
        if (body != null) body.position = target;
        player.position = target;
    }

    private void HandleRoomCleared(RoomInstance room)
    {
        if (room == null || runOver) return;

        OnRoomCleared?.Invoke(room);

        if (room.Kind != RoomKind.Boss || transitioning) return;

        transitioning = true;
        StartCoroutine(AdvanceAfterBoss(room));
    }

    /// <summary>
    /// 보상을 그냥 두고 넘어가 버리면 보스 보상을 영영 못 받으므로,
    /// 받침대가 남아 있는 동안에는 층 전환을 미룬다.
    /// </summary>
    private IEnumerator AdvanceAfterBoss(RoomInstance bossRoom)
    {
        if (waitForBossReward)
        {
            float giveUpTime = Time.unscaledTime + maximumBossRewardWait;
            while (bossRoom != null && bossRoom.HasPendingRewards)
            {
                if (Time.unscaledTime >= giveUpTime)
                {
                    Debug.LogWarning(
                        "보스 보상을 가져가지 않아 기다림을 끝내고 다음 층으로 넘어갑니다.",
                        this);
                    break;
                }
                yield return null;
            }
        }

        yield return new WaitForSeconds(floorTransitionDelay);
        if (!runOver) AdvanceFloor();
    }

    private void AdvanceFloor()
    {
        OnFloorCleared?.Invoke(chapterIndex, floorIndex);

        floorIndex++;
        ChapterProfile chapter = runProfile.GetChapter(chapterIndex);
        if (chapter != null && floorIndex >= chapter.FloorCount)
        {
            chapterIndex++;
            floorIndex = 0;
        }

        if (runProfile.GetFloor(chapterIndex, floorIndex) == null)
        {
            ClearCurrentFloor();
            runOver = true;
            OnRunCompleted?.Invoke();
            return;
        }

        BuildCurrentFloor();
    }

    private void ClearCurrentFloor()
    {
        if (currentFloor == null) return;

        foreach (RoomInstance room in currentFloor.Rooms)
        {
            if (room != null) room.OnRoomCleared -= HandleRoomCleared;
        }
        if (currentFloor.Root != null) Destroy(currentFloor.Root.gameObject);
        currentFloor = null;
    }

    private void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }
}
