using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경험치 막대와 레벨 표시를 갱신한다. 막대와 글자는 프리팹에 미리 만들어 두고,
/// 여기서는 값만 다시 넣는다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Player Level UI")]
public class PlayerLevelUI : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 Player 태그가 붙은 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerLevel playerLevel;
    [SerializeField] private Slider experienceSlider;
    [Tooltip("'Lv. 3'처럼 레벨을 적는 글자. 프리팹에 미리 놓아 둔 것을 연결한다.")]
    [SerializeField] private TMP_Text levelText;

    [Header("문구")]
    [SerializeField] private string levelFormat = "Lv. {0}";

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(levelText);
    }

    private void Start()
    {
        if (playerLevel == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            playerLevel = player != null
                ? player.GetComponentInChildren<PlayerLevel>(true) : null;
        }

        if (playerLevel == null || experienceSlider == null)
        {
            Debug.LogError("Player Level UI에 필요한 오브젝트가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }
        if (levelText == null)
        {
            Debug.LogWarning(
                "Level Text가 비어 있어 레벨 표시가 뜨지 않습니다. 프리팹에서 연결해 주세요.",
                this);
        }

        DisplayOnlyUI.Configure(experienceSlider);
        playerLevel.OnProgressChanged += UpdateLevelUI;
        UpdateLevelUI();
    }

    private void OnDestroy()
    {
        if (playerLevel != null)
        {
            playerLevel.OnProgressChanged -= UpdateLevelUI;
        }
    }

    private void UpdateLevelUI()
    {
        experienceSlider.maxValue = playerLevel.GetExperienceToNextLevel();
        experienceSlider.value = playerLevel.GetCurrentExperience();

        if (levelText == null) return;
        levelText.text = string.Format(levelFormat, playerLevel.GetCurrentLevel());
    }
}
