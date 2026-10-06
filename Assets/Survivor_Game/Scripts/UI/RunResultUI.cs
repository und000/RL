using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 런이 끝났을 때 결과를 띄우고 다시 시작할 수 있게 한다.
/// 마지막 챕터까지 끝내면 성공, 플레이어가 쓰러지면 실패로 본다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Run Result UI")]
public class RunResultUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private RunManager runManager;
    [Tooltip("결과 화면 전체를 담은 오브젝트. 평소에는 꺼 둔다.")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text buildText;
    [SerializeField] private Button restartButton;
    [Tooltip("첫 화면으로 돌아가는 버튼. 비워 두면 다시 시작만 남는다.")]
    [SerializeField] private Button titleButton;

    [Header("연출")]
    [Tooltip("결과 화면이 뜨기까지 두는 여유 시간. 죽는 연출을 보여 주기 위한 것이다.")]
    [SerializeField, Min(0f)] private float showDelay = 1.2f;
    [Tooltip("켜면 결과 화면이 떠 있는 동안 게임을 멈춘다.")]
    [SerializeField] private bool pauseOnShow = true;

    [Header("문구")]
    [SerializeField] private string clearTitle = "탈출 성공";
    [SerializeField] private string failTitle = "기체 정지";
    [SerializeField] private string clearDetail = "모든 챕터를 돌파했습니다.";
    [SerializeField] private string failDetailFormat = "{0} · {1}에서 멈췄습니다.";
    [Tooltip("다시 시작 버튼 문구. 씬에는 폰트 경고를 피하려고 영문 자리표시가 들어 있다.")]
    [SerializeField] private string restartLabel = "다시 시작";
    [SerializeField] private string titleButtonLabel = "타이틀로";

    [Header("이동")]
    [Tooltip("타이틀로를 누를 때 불러올 씬. Build Settings에 들어 있어야 한다.")]
    [SerializeField] private string titleSceneName = "Title";

    private float showTime;
    private bool pending;
    private bool shown;

    private void Awake()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        ApplyFont();
        ConfigureBuildScroll();
    }

    private void Start()
    {
        if (runManager == null) runManager = FindFirstObjectByType<RunManager>();
        if (runManager == null || resultPanel == null)
        {
            Debug.LogError("Run Result UI에 Run Manager와 결과 패널이 필요합니다.", this);
            enabled = false;
            return;
        }

        runManager.OnRunCompleted += HandleRunCompleted;
        runManager.OnRunFailed += HandleRunFailed;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (titleButton != null) titleButton.onClick.AddListener(GoToTitle);
    }

    /// <summary>한글이 네모로 나오지 않도록 게임 폰트를 입힌다.</summary>
    private void ApplyFont()
    {
        GameFontManager.ApplyFont(titleText);
        GameFontManager.ApplyFont(detailText);
        GameFontManager.ApplyFont(buildText);
        ApplyButtonFont(restartButton, restartLabel);
        ApplyButtonFont(titleButton, titleButtonLabel);
    }

    private void ApplyButtonFont(Button button, string text)
    {
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        GameFontManager.ApplyFont(label);
        if (label != null) label.text = text;
    }

    private void OnDestroy()
    {
        if (runManager != null)
        {
            runManager.OnRunCompleted -= HandleRunCompleted;
            runManager.OnRunFailed -= HandleRunFailed;
        }
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (titleButton != null) titleButton.onClick.RemoveListener(GoToTitle);
    }

    private void Update()
    {
        if (!pending || shown || Time.unscaledTime < showTime) return;
        Show();
    }

    private void HandleRunCompleted()
    {
        Prepare(clearTitle, clearDetail, false);
    }

    private void HandleRunFailed()
    {
        string detail = string.Format(
            failDetailFormat, runManager.ChapterDisplayName, runManager.FloorDisplayName);
        Prepare(failTitle, detail, true);
    }

    private void Prepare(string title, string detail, bool failed)
    {
        if (pending || shown) return;

        if (titleText != null) titleText.text = title;
        if (detailText != null) detailText.text = detail + "\n완료한 전투방 " + runManager.CompletedCombatRooms + "개" +
            "\n전투 " + FormatTime(runManager.CombatSeconds) + " · 이동 " + FormatTime(runManager.ExplorationSeconds) +
            "\n보상/보드 " + FormatTime(runManager.RewardSeconds) + "\n\n" +
            RunResultSummary.PlayerStatus(runManager.Player, failed);
        if (buildText != null)
        {
            Transform player = runManager.Player;
            PlayerEquipment equipment = player != null ? player.GetComponentInChildren<PlayerEquipment>() : null;
            CoreBoardController board = FindFirstObjectByType<CoreBoardController>();
            ChipInventory inventory = FindFirstObjectByType<ChipInventory>();
            buildText.text = RunResultSummary.Loadout(equipment, board, inventory);
        }

        pending = true;
        // 게임이 멈춘 뒤에도 대기 시간이 흘러야 하므로 실제 시간으로 잰다.
        showTime = Time.unscaledTime + showDelay;
    }

    private void Show()
    {
        StaggerImpactFeedback.CancelActive();
        shown = true;
        pending = false;
        resultPanel.SetActive(true);
        if (pauseOnShow) Time.timeScale = 0f;
    }

    private void ConfigureBuildScroll()
    {
        if (buildText == null) return;
        RectTransform content = buildText.rectTransform;
        var viewport = new GameObject("BuildSummaryScroll", typeof(RectTransform), typeof(Image),
            typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        viewport.SetParent(content.parent, false);
        viewport.anchorMin = content.anchorMin;
        viewport.anchorMax = content.anchorMax;
        viewport.pivot = content.pivot;
        viewport.anchoredPosition = content.anchoredPosition;
        viewport.sizeDelta = content.sizeDelta;
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, .06f);
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        buildText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = viewport.GetComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return (total / 60) + ":" + (total % 60).ToString("00");
    }

    /// <summary>런에서 남긴 것을 가지고 첫 화면으로 돌아가 기체를 손보게 한다.</summary>
    public void GoToTitle()
    {
        StaggerImpactFeedback.CancelActive();
        // 씬을 바꾸기 전에 반드시 시간을 되돌린다.
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogError("타이틀 씬 이름이 비어 있습니다.", this);
            return;
        }
        SceneManager.LoadScene(titleSceneName);
    }

    public void Restart()
    {
        StaggerImpactFeedback.CancelActive();
        // 씬을 다시 불러오기 전에 반드시 시간을 되돌린다.
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex >= 0 ? active.buildIndex : 0);
    }
}
