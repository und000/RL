using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨이 오르면 시간을 늦추고 축하 창을 띄운다. 창은 프리팹에 미리 만들어 두고,
/// 여기서는 켜고 끄며 문구만 채운다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Level Up UI")]
public class LevelUpUI : MonoBehaviour
{
    /// <summary>창이 떠 있는 동안에는 공격·회피 같은 조작을 막는다.</summary>
    public static bool IsPopupOpen { get; private set; }

    [Header("연결")]
    [Tooltip("비워 두면 Player 태그가 붙은 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerLevel playerLevel;
    [Tooltip("켜고 끄는 창. 프리팹 안에 미리 놓아 둔 것을 연결한다. " +
        "이 오브젝트만 꺼지고 스크립트가 붙은 뿌리는 켜져 있어야 한다.")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button continueButton;

    [Header("레벨업 시간 연출")]
    [SerializeField, Min(0f)] private float slowDownDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minimumTimeScale;

    [Header("문구")]
    [Tooltip("프리팹에는 폰트 경고를 피하려고 영문 자리표시가 들어 있다. " +
        "여기 적은 것이 실제로 화면에 뜬다.")]
    [SerializeField] private string titleFormat = "LEVEL UP!  Lv. {0}";
    [SerializeField] private string messageLabel = "레벨이 올랐습니다!";
    [SerializeField] private string continueLabel = "계속";

    private int previousLevel;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
        IsPopupOpen = false;

        // 프리팹에 한글을 저장해 두면 폰트가 붙기 전 한 프레임 네모로 그려진다.
        // 그래서 문구는 여기서 채운다.
        GameFontManager.ApplyFont(titleText);
        GameFontManager.ApplyFont(messageText);
        if (messageText != null) messageText.text = messageLabel;

        if (continueButton != null)
        {
            TMP_Text label = continueButton.GetComponentInChildren<TMP_Text>(true);
            GameFontManager.ApplyFont(label);
            if (label != null) label.text = continueLabel;
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    private void Start()
    {
        if (playerLevel == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            playerLevel = player != null
                ? player.GetComponentInChildren<PlayerLevel>(true) : null;
        }

        if (playerLevel == null || levelUpPanel == null)
        {
            Debug.LogError("Level Up UI에 Player Level과 창이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        previousLevel = playerLevel.GetCurrentLevel();
        playerLevel.OnProgressChanged += HandleProgressChanged;
    }

    private void HandleProgressChanged()
    {
        int currentLevel = playerLevel.GetCurrentLevel();

        if (currentLevel <= previousLevel)
        {
            return;
        }

        previousLevel = currentLevel;
        BeginLevelUpSequence();
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
        {
            playerLevel.OnProgressChanged -= HandleProgressChanged;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueGame);
        }

        if (IsPopupOpen)
        {
            Time.timeScale = timeScaleBeforePause;
        }

        IsPopupOpen = false;
    }

    private void BeginLevelUpSequence()
    {
        if (IsPopupOpen)
        {
            return;
        }

        IsPopupOpen = true;
        timeScaleBeforePause = Time.timeScale;
        StartCoroutine(SlowDownAndShowPopup());
    }

    private IEnumerator SlowDownAndShowPopup()
    {
        float startTimeScale = timeScaleBeforePause;
        float targetTimeScale = Mathf.Clamp(minimumTimeScale, 0f, startTimeScale);

        if (slowDownDuration > 0f)
        {
            float elapsedTime = 0f;
            while (elapsedTime < slowDownDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / slowDownDuration);
                Time.timeScale = Mathf.Lerp(startTimeScale, targetTimeScale, progress);
                yield return null;
            }
        }

        Time.timeScale = targetTimeScale;
        // 느려지는 동안 레벨이 더 올랐을 수 있으므로 뜨는 순간의 레벨을 적는다.
        if (titleText != null)
        {
            titleText.text = string.Format(titleFormat, playerLevel.GetCurrentLevel());
        }
        levelUpPanel.SetActive(true);
    }

    private void ContinueGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = timeScaleBeforePause;
        IsPopupOpen = false;
    }
}
