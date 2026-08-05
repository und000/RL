using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    public static bool IsPopupOpen { get; private set; }

    [SerializeField]
    private PlayerLevel playerLevel;

    [Header("레벨업 시간 연출")]
    [SerializeField, Min(0f)] private float slowDownDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minimumTimeScale;

    private GameObject levelUpPanel;
    private TMP_Text titleText;
    private int previousLevel;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
        IsPopupOpen = false;
        CreateLevelUpPanel();
        levelUpPanel.SetActive(false);
    }

    private void Start()
    {
        if (playerLevel == null)
        {
            Debug.LogError("Level Up UI에 Player Level이 연결되지 않았습니다.");
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
        BeginLevelUpSequence(currentLevel);
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
        {
            playerLevel.OnProgressChanged -= HandleProgressChanged;
        }

        if (IsPopupOpen)
        {
            Time.timeScale = timeScaleBeforePause;
        }

        IsPopupOpen = false;
    }

    private void BeginLevelUpSequence(int currentLevel)
    {
        if (IsPopupOpen)
        {
            return;
        }

        IsPopupOpen = true;
        titleText.text = $"LEVEL UP!  Lv. {currentLevel}";
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
        levelUpPanel.SetActive(true);
    }

    private void ContinueGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = timeScaleBeforePause;
        IsPopupOpen = false;
    }

    private void CreateLevelUpPanel()
    {
        levelUpPanel = CreateUIObject("LevelUpPanel", transform);
        StretchToParent(levelUpPanel.GetComponent<RectTransform>());

        Image panelImage = levelUpPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject window = CreateUIObject("Window", levelUpPanel.transform);
        RectTransform windowTransform = window.GetComponent<RectTransform>();
        windowTransform.anchorMin = new Vector2(0.5f, 0.5f);
        windowTransform.anchorMax = new Vector2(0.5f, 0.5f);
        windowTransform.sizeDelta = new Vector2(520f, 300f);

        Image windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.12f, 0.16f, 0.24f, 1f);

        titleText = CreateText(
            "TitleText", windowTransform, new Vector2(0f, 80f),
            new Vector2(460f, 70f), 36f
        );

        TMP_Text messageText = CreateText(
            "MessageText", windowTransform, new Vector2(0f, 15f),
            new Vector2(440f, 60f), 22f
        );
        messageText.text = "레벨이 올랐습니다!";

        CreateContinueButton(windowTransform);
    }

    private void CreateContinueButton(Transform parent)
    {
        GameObject buttonObject = CreateUIObject("ContinueButton", parent);
        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonTransform.anchoredPosition = new Vector2(0f, -85f);
        buttonTransform.sizeDelta = new Vector2(220f, 60f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.65f, 0.95f, 1f);

        Button continueButton = buttonObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        continueButton.onClick.AddListener(ContinueGame);

        TMP_Text buttonText = CreateText(
            "ButtonText", buttonTransform, Vector2.zero,
            buttonTransform.sizeDelta, 24f
        );
        buttonText.text = "계속";
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = position;
        textTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        GameFontManager.ApplyFont(text);
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchToParent(RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}
