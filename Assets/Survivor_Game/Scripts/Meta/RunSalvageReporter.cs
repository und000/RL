using UnityEngine;

/// <summary>
/// 런이 얼마나 나아갔는지 세어 두었다가, 끝나는 순간 잔존 코드로 바꿔 넣는다.
/// 성공이든 실패든 남는 것이 있어야 다음 런을 시작할 이유가 된다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Meta/Run Salvage Reporter")]
public class RunSalvageReporter : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private RunManager runManager;
    [SerializeField] private MetaSalvageProfile profile;
    [Tooltip("남은 크레딧을 잔존 코드로 바꿀 때 쓴다. 비워 두면 Player 태그에서 찾는다.")]
    [SerializeField] private PlayerWallet wallet;

    [Header("확인")]
    [Tooltip("켜면 런이 끝날 때 내역을 콘솔에 적는다.")]
    [SerializeField] private bool logAward = true;

    /// <summary>마지막 런이 남긴 내역. 결과 화면이 읽어 간다.</summary>
    public SalvageBreakdown LastAward { get; private set; }
    public bool HasAward { get; private set; }

    private int clearedRooms;
    private int clearedFloors;
    private int clearedChapters;
    private int reachedChapter;
    private int reachedFloor;
    private bool banked;

    private void Awake()
    {
        if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        if (wallet == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            wallet = player != null
                ? player.GetComponentInChildren<PlayerWallet>(true) : null;
        }
    }

    private void OnEnable()
    {
        if (runManager == null) return;

        runManager.OnRoomCleared += HandleRoomCleared;
        runManager.OnFloorStarted += HandleFloorStarted;
        runManager.OnFloorCleared += HandleFloorCleared;
        runManager.OnRunCompleted += HandleRunCompleted;
        runManager.OnRunFailed += HandleRunFailed;
    }

    private void OnDisable()
    {
        if (runManager == null) return;

        runManager.OnRoomCleared -= HandleRoomCleared;
        runManager.OnFloorStarted -= HandleFloorStarted;
        runManager.OnFloorCleared -= HandleFloorCleared;
        runManager.OnRunCompleted -= HandleRunCompleted;
        runManager.OnRunFailed -= HandleRunFailed;
    }

    private void HandleRoomCleared(RoomInstance room)
    {
        if (room != null) clearedRooms++;
    }

    /// <summary>어디까지 갔는지는 층이 열릴 때마다 갱신해 둔다. 1부터 세어 기록한다.</summary>
    private void HandleFloorStarted(int chapter, int floor, FloorProfile floorProfile)
    {
        reachedChapter = chapter + 1;
        reachedFloor = floor + 1;
    }

    private void HandleFloorCleared(int chapter, int floor)
    {
        clearedFloors++;
    }

    private void HandleRunCompleted()
    {
        // 마지막 챕터까지 끝냈으므로 지금 챕터까지 넣어 센다.
        clearedChapters = reachedChapter;
        Bank(true);
    }

    private void HandleRunFailed()
    {
        // 지금 챕터는 끝내지 못했으므로 그 앞의 것들만 넘긴 것으로 친다.
        clearedChapters = Mathf.Max(0, reachedChapter - 1);
        Bank(false);
    }

    /// <summary>모은 값을 잔존 코드로 바꿔 저장한다. 한 런에 한 번만 들어간다.</summary>
    private void Bank(bool cleared)
    {
        if (banked) return;
        banked = true;

        SalvageBreakdown award = Calculate(cleared);
        LastAward = award;
        HasAward = true;

        MetaProgress.AddSalvage(award.Total);
        MetaProgress.RecordRun(reachedChapter, reachedFloor, cleared);

        if (!logAward) return;
        Debug.Log(
            "[영구 개조] 잔존 코드 +" + award.Total +
            " (방 " + award.Rooms + " · 층 " + award.Floors +
            " · 챕터 " + award.Chapters + ")", this);
    }

    private SalvageBreakdown Calculate(bool cleared)
    {
        SalvageBreakdown award = new SalvageBreakdown
        {
            Rooms = clearedRooms,
            Floors = clearedFloors,
            Chapters = clearedChapters
        };

        if (profile == null)
        {
            // 표가 없어도 진행이 멈추지는 않게, 방 수만큼만 남긴다.
            award.FromRooms = clearedRooms;
            award.Total = Mathf.Max(1, clearedRooms);
            return award;
        }

        award.FromRooms = clearedRooms * profile.PerRoom;
        award.FromFloors = clearedFloors * profile.PerFloor;
        award.FromChapters = clearedChapters * profile.PerChapter;
        award.FromCredits = wallet != null
            ? Mathf.FloorToInt(wallet.Credits * profile.CreditConversion)
            : 0;
        award.ClearBonus = cleared ? profile.ClearBonus : 0;

        float rate = MetaProgressRuntime.Bonuses.SalvageRate;
        int scaled = Mathf.RoundToInt(award.Subtotal * Mathf.Max(0f, rate));
        award.RateBonus = scaled - award.Subtotal;
        award.Total = Mathf.Max(profile.MinimumAward, scaled);
        return award;
    }
}
