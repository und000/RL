using TMPro;
using UnityEngine;

/// <summary>
/// 지금 어느 챕터 몇 층에 있고, 이 층의 방을 얼마나 정리했는지 보여 준다.
/// 층이 바뀌면 층 이름을 잠깐 크게 띄운다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Run Hud UI")]
public class RunHudUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private RunManager runManager;
    [Tooltip("'1장 · 2F'처럼 현재 위치를 항상 띄우는 텍스트.")]
    [SerializeField] private TMP_Text locationText;
    [Tooltip("출구 개방에 필요한 남은 전투방 수를 띄우는 텍스트.")]
    [SerializeField] private TMP_Text progressText;

    [Header("층 진입 배너")]
    [Tooltip("층이 바뀔 때 잠깐 켜지는 오브젝트. 비워 둬도 된다.")]
    [SerializeField] private GameObject floorBanner;
    [SerializeField] private TMP_Text floorBannerText;
    [SerializeField, Min(0f)] private float bannerDuration = 2f;

    [Header("문구")]
    [SerializeField] private string locationFormat = "{0} · {1}";
    [SerializeField] private string progressFormat = "남은 전투방 {0}개";

    private float bannerHideTime;

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        ApplyFont();
    }

    private void Start()
    {
        if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        if (runManager == null)
        {
            Debug.LogError("씬에 Run Manager가 없어 층 표시를 띄우지 못합니다.", this);
            enabled = false;
            return;
        }

        runManager.OnFloorStarted += HandleFloorStarted;
        runManager.OnRoomCleared += HandleRoomCleared;

        if (floorBanner != null) floorBanner.SetActive(false);
        Refresh();

        // RunManager가 먼저 Start를 돌았다면 첫 층의 이벤트는 이미 지나갔다.
        if (runManager.CurrentFloor != null) ShowBanner();
    }

    /// <summary>한글이 네모로 나오지 않도록 게임 폰트를 입힌다.</summary>
    private void ApplyFont()
    {
        GameFontManager.ApplyFont(locationText);
        GameFontManager.ApplyFont(progressText);
        GameFontManager.ApplyFont(floorBannerText);
    }

    private void OnDestroy()
    {
        if (runManager == null) return;
        runManager.OnFloorStarted -= HandleFloorStarted;
        runManager.OnRoomCleared -= HandleRoomCleared;
    }

    private void Update()
    {
        if (floorBanner == null || !floorBanner.activeSelf) return;
        if (runManager != null && runManager.IsTransitioning)
        {
            bannerHideTime = Time.unscaledTime + bannerDuration;
            return;
        }
        if (Time.unscaledTime < bannerHideTime) return;

        floorBanner.SetActive(false);
    }

    private void HandleFloorStarted(int chapterIndex, int floorIndex, FloorProfile profile)
    {
        Refresh();
        ShowBanner();
    }

    private void HandleRoomCleared(RoomInstance room)
    {
        Refresh();
    }

    private void Refresh()
    {
        string chapter = runManager.ChapterDisplayName;
        string floor = runManager.FloorDisplayName;

        if (locationText != null)
        {
            locationText.text = string.IsNullOrEmpty(chapter)
                ? floor
                : string.Format(locationFormat, chapter, floor);
        }
        if (progressText != null)
        {
            if (runManager.CurrentFloor == null) { progressText.text = "스테이지 준비 중"; return; }
            int remaining = runManager.CurrentFloor != null ? runManager.CurrentFloor.RemainingCombatRooms : 0;
            progressText.text = remaining > 0 ? string.Format(progressFormat, remaining, runManager.TotalRoomCount) : "출구 개방 · " + runManager.ExitDestinationLabel;
        }
    }

    private void ShowBanner()
    {
        if (floorBanner == null) return;

        if (floorBannerText != null)
        {
            string chapter = runManager.ChapterDisplayName;
            string floor = runManager.FloorDisplayName;
            floorBannerText.text = string.IsNullOrEmpty(chapter)
                ? floor
                : string.Format(locationFormat, chapter, floor);
        }

        floorBanner.SetActive(true);
        // 층 전환 중에는 시간이 느려질 수 있으므로 실제 시간으로 잰다.
        bannerHideTime = Time.unscaledTime + bannerDuration;
    }
}
