using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 여러 방이 연결된 스테이지를 생성하고 유일한 출구에서 다음 스테이지로 이동한다.
/// 일반 스테이지 4~5개 뒤 선택 보스전을 완료하고 다음 맵으로 이동한다.
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
    [Tooltip("화면이 완전히 검어진 뒤 유지할 최소 로딩 시간(초).")]
    [SerializeField, Min(0f)] private float floorTransitionDelay = 1.5f;

    /// <summary>새 층이 만들어진 직후.</summary>
    public event Action<int, int, FloorProfile> OnFloorStarted;
    /// <summary>전투를 완료한 스테이지의 출구를 사용한 순간.</summary>
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
    private StageTransitionUI transitionUI;

    public int ChapterIndex => chapterIndex;
    public int FloorIndex => floorIndex;
    public GeneratedFloor CurrentFloor => currentFloor;
    public FloorProfile CurrentProfile => currentProfile;
    public bool IsRunOver => runOver;
    public bool IsTransitioning => transitioning;
    public int ChapterStageCount => runProfile != null ? runProfile.GetChapter(chapterIndex)?.FloorCount ?? 0 : 0;
    public bool IsBossStage => currentProfile != null && currentProfile.IsBossStage;
    public bool IsCurrentFloorCleared => currentFloor != null && currentFloor.IsCombatCleared;
    public string ExitDestinationLabel => IsFinalFloor ? "런 완료" : IsBossStage ? "다음 맵" :
        runProfile.GetFloor(chapterIndex, floorIndex + 1)?.IsBossStage == true ? "보스 선택" : "다음 스테이지";
    public bool IsFinalFloor => runProfile != null &&
        runProfile.GetFloor(chapterIndex, floorIndex + 1) == null &&
        runProfile.GetFloor(chapterIndex + 1, 0) == null;

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
        if (runProfile == null || spawner == null || runProfile.FloorExitPrefab == null ||
            runProfile.BossChoicePrefab == null || runProfile.StageTransitionUIPrefab == null ||
            !runProfile.StageTransitionUIPrefab.IsConfigured)
        {
            Debug.LogError("RunManager에 Run Profile, Spawn Director, 출구·보스 선택·스테이지 로딩 UI 프리팹이 필요합니다.", this);
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
        transitionUI = Instantiate(runProfile.StageTransitionUIPrefab, transform);

        context = new RoomRuntimeContext
        {
            RunManager = this,
            BossChoicePrefab = runProfile.BossChoicePrefab,
            ExitPrefab = runProfile.FloorExitPrefab,
            Spawner = spawner,
            PedestalPrefab = runProfile.RewardPedestalPrefab,
            DropPrefab = runProfile.RewardDropPrefab,
            Wallet = player != null ? player.GetComponentInChildren<PlayerWallet>() : null,
            WeaponEquipment = player != null ? player.GetComponentInChildren<PlayerWeaponEquipment>() : null,
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
        if (transitionUI != null) transitionUI.End(false);
        transitioning = false;
        StopAllCoroutines();
        CancelInvoke();
        OnRunFailed?.Invoke();
    }

    public void BeginRun()
    {
        if (runProfile == null || context == null) return;
        for (int index = 0; index < runProfile.ChapterCount; index++)
        {
            ChapterProfile chapter = runProfile.GetChapter(index);
            string error = "맵 프로필이 비어 있습니다.";
            if (chapter == null || !chapter.IsValid(out error))
            {
                Debug.LogError($"맵 {index + 1}: {error}", this);
                return;
            }
        }
        StopAllCoroutines();
        if (transitionUI != null) transitionUI.End(!runOver);
        transitioning = false;
        chapterIndex = 0;
        floorIndex = 0;
        runOver = false;
        BuildCurrentFloor();
    }

    private bool BuildCurrentFloor()
    {
        FloorProfile profile = runProfile.GetFloor(chapterIndex, floorIndex);
        if (profile == null)
        {
            Debug.LogError($"맵 {chapterIndex + 1}, 스테이지 {floorIndex + 1}의 프로필이 없습니다.", this);
            return false;
        }
        FloorSpawnPlan previousSpawnPlan = context.SpawnPlan;
        FloorRewardPlan previousRewardPlan = context.RewardPlan;
        var previousBossCandidates = context.BossCandidates;
        context.SpawnPlan = profile.SpawnPlan;
        context.RewardPlan = profile.RewardPlan;
        context.BossCandidates = profile.IsBossStage ? runProfile.GetChapter(chapterIndex).BossCandidates : null;
        // 배치 실패 시 현재 스테이지를 유지해 출구에서 다시 시도할 수 있게 한다.
        GeneratedFloor generated = FloorGenerator.Generate(profile, context, floorRoot);
        if (generated == null)
        {
            context.SpawnPlan = previousSpawnPlan;
            context.RewardPlan = previousRewardPlan;
            context.BossCandidates = previousBossCandidates;
            return false;
        }
        ClearCurrentFloor();
        currentProfile = profile;
        currentFloor = generated;
        foreach (RoomInstance room in currentFloor.Rooms) room.OnRoomCleared += HandleRoomCleared;
        MovePlayerToStart();
        currentFloor.Exit.Configure(this, player);
        OnFloorStarted?.Invoke(chapterIndex, floorIndex, profile);
        if (player != null) currentFloor.StartRoom.EnterFromStageTransition(player.position);
        return true;
    }

    private void MovePlayerToStart()
    {
        if (player == null || currentFloor == null || currentFloor.StartRoom == null) return;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector3 target = currentFloor.StartRoom.transform.position;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.StopAttackMovement();
        foreach (MeleeWeaponAttack weapon in player.GetComponentsInChildren<MeleeWeaponAttack>()) weapon.CancelForStageTransition();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = target;
        }
        player.position = target;
        Physics2D.SyncTransforms();
        foreach (CameraFollow camera in FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))
            if (camera.gameObject.scene == gameObject.scene) camera.SnapToTarget();
    }

    private void HandleRoomCleared(RoomInstance room)
    {
        if (room == null || runOver) return;

        OnRoomCleared?.Invoke(room);
    }

    /// <summary>현재 층의 유일한 출구만 전환을 요청할 수 있다. 남은 보상은 플레이어가 선택한다.</summary>
    public bool TryUseFloorExit(FloorExit exit)
    {
        if (!isActiveAndEnabled || runOver || transitioning || currentFloor == null ||
            exit == null || exit != currentFloor.Exit || !exit.CanUse ||
            transitionUI == null || !transitionUI.Begin()) return false;
        transitioning = true;
        StartCoroutine(AdvanceFromExit());
        return true;
    }

    private IEnumerator AdvanceFromExit()
    {
        try
        {
            yield return transitionUI.FadeOut();
            // 알파 1인 검은 화면을 먼저 렌더링한 뒤 동기 생성 작업을 수행한다.
            yield return null;
            float blackStartedAt = Time.realtimeSinceStartup;
            if (!runOver) AdvanceFloor();
            // 이전 스테이지의 지연 Destroy와 UI 갱신도 검은 화면 안에서 마친다.
            yield return null;
            float remaining = floorTransitionDelay - (Time.realtimeSinceStartup - blackStartedAt);
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);
            yield return transitionUI.FadeIn();
        }
        finally
        {
            if (transitionUI != null) transitionUI.End(!runOver);
            transitioning = false;
        }
    }

    private void AdvanceFloor()
    {
        int previousChapter = chapterIndex;
        int previousFloor = floorIndex;
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
            OnFloorCleared?.Invoke(previousChapter, previousFloor);
            OnRunCompleted?.Invoke();
            return;
        }
        if (BuildCurrentFloor()) OnFloorCleared?.Invoke(previousChapter, previousFloor);
        else
        {
            chapterIndex = previousChapter;
            floorIndex = previousFloor;
        }
    }

    private void ClearCurrentFloor()
    {
        if (currentFloor == null) return;

        // 다음 스테이지도 같은 월드를 쓰므로 이전 전투의 투사체가 넘어가지 않게 한다.
        foreach (EnemyProjectile projectile in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
            if (projectile.gameObject.scene == gameObject.scene) projectile.Cancel();
        foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            if (projectile.gameObject.scene == gameObject.scene)
            {
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }

        foreach (RoomInstance room in currentFloor.Rooms)
        {
            if (room != null) room.OnRoomCleared -= HandleRoomCleared;
        }
        if (currentFloor.Root != null)
        {
            currentFloor.Root.gameObject.SetActive(false);
            Destroy(currentFloor.Root.gameObject);
        }
        currentFloor = null;
    }

    private void OnDisable()
    {
        if (transitionUI != null) transitionUI.End(!runOver);
        transitioning = false;
        CancelInvoke();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }
}
