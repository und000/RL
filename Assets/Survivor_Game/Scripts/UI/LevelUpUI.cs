using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField]
    private PlayerLevel playerLevel;

    private GameObject levelUpPanel;
    private TMP_Text titleText;
    private int previousLevel;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
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
    }

    private void Update()
    {
        int currentLevel = playerLevel.GetCurrentLevel();

        if (currentLevel <= previousLevel)
        {
            return;
        }

        previousLevel = currentLevel;
        ShowLevelUpPanel(currentLevel);
    }

    private void ShowLevelUpPanel(int currentLevel)
    {
        if (levelUpPanel.activeSelf)
        {
            return;
        }

        titleText.text = $"LEVEL UP!  Lv. {currentLevel}";
        levelUpPanel.SetActive(true);

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void ContinueGame()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = timeScaleBeforePause;
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
