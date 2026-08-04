using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelUI : MonoBehaviour
{
    [SerializeField]
    private PlayerLevel playerLevel;

    [SerializeField]
    private Slider experienceSlider;

    [SerializeField]
    private TMP_Text levelText;

    private void Awake()
    {
        if (levelText == null)
        {
            CreateLevelText();
        }
    }

    private void Start()
    {
        if (playerLevel == null ||
            experienceSlider == null)
        {
            Debug.LogError("Player Level UI에 필요한 오브젝트가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        UpdateLevelUI();
    }

    private void Update()
    {
        UpdateLevelUI();
    }

    private void UpdateLevelUI()
    {
        int currentLevel = playerLevel.GetCurrentLevel();
        int currentExperience = playerLevel.GetCurrentExperience();
        int experienceToNextLevel = playerLevel.GetExperienceToNextLevel();

        experienceSlider.maxValue = experienceToNextLevel;
        experienceSlider.value = currentExperience;
        levelText.text = $"Lv. {currentLevel}";
    }

    private void CreateLevelText()
    {
        GameObject levelTextObject = new GameObject(
            "LevelText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        RectTransform levelTextTransform =
            levelTextObject.GetComponent<RectTransform>();

        levelTextTransform.SetParent(transform, false);
        levelTextTransform.anchorMin = new Vector2(0f, 1f);
        levelTextTransform.anchorMax = new Vector2(0f, 1f);
        levelTextTransform.pivot = new Vector2(0.5f, 0.5f);
        levelTextTransform.anchoredPosition = new Vector2(80f, -110f);
        levelTextTransform.sizeDelta = new Vector2(120f, 40f);

        levelText = levelTextObject.GetComponent<TextMeshProUGUI>();
        levelText.fontSize = 28f;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.color = Color.white;
        levelText.raycastTarget = false;
    }
}
