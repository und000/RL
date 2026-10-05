using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 첫 화면. 런을 시작하거나, 지난 런이 남긴 잔존 코드로 기체를 손보거나, 끈다.
/// 모양은 프리팹에서 다 짜 두었고 여기서는 눌렀을 때 무엇을 할지만 정한다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Title Screen UI")]
public class TitleScreenUI : MonoBehaviour
{
    [Header("구성 요소")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button quitButton;
    [Tooltip("개조 화면. 비워 두면 개조 버튼이 꺼진다.")]
    [SerializeField] private MetaUpgradeScreen upgradeScreen;
    [Header("출발 무기")]
    [SerializeField] private StartingWeaponCatalog startingWeapons;
    [SerializeField] private StartingWeaponSelectionUI weaponSelectionPrefab;
    private StartingWeaponSelectionUI weaponSelection;

    [Header("이동")]
    [Tooltip("시작을 누르면 불러올 씬. Build Settings에 들어 있어야 한다.")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("문구")]
    [Tooltip("프리팹에는 폰트 경고를 피하려고 영문 자리표시가 들어 있다. " +
        "여기 적은 것이 실제로 화면에 뜬다.")]
    [SerializeField] private string titleLabel = "잔존";
    [SerializeField] private string subtitleLabel = "기체는 멈추고 코드는 남는다";
    [SerializeField] private string startLabel = "런 시작";
    [SerializeField] private string upgradeLabel = "기체 개조";
    [SerializeField] private string quitLabel = "종료";

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(titleText);
        GameFontManager.ApplyFont(subtitleText);
        if (titleText != null) titleText.text = titleLabel;
        if (subtitleText != null) subtitleText.text = subtitleLabel;

        Label(startButton, startLabel);
        Label(upgradeButton, upgradeLabel);
        Label(quitButton, quitLabel);
    }

    private void Start()
    {
        // 앞선 런이 화면을 멈춰 둔 채로 넘어왔을 수 있다.
        Time.timeScale = 1f;

        if (startButton != null) startButton.onClick.AddListener(StartRun);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OpenUpgrade);
            upgradeButton.interactable = upgradeScreen != null;
        }

        if (upgradeScreen != null) upgradeScreen.Hide();
    }

    private void OnDestroy()
    {
        if (weaponSelection != null) Destroy(weaponSelection.gameObject);
        if (startButton != null) startButton.onClick.RemoveListener(StartRun);
        if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OpenUpgrade);
    }

    private void Label(Button button, string text)
    {
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        GameFontManager.ApplyFont(label);
        if (label != null) label.text = text;
    }

    public void StartRun()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("불러올 씬 이름이 비어 있습니다.", this);
            return;
        }

        if (startingWeapons == null || !startingWeapons.IsValid || weaponSelectionPrefab == null ||
            !weaponSelectionPrefab.IsConfigured)
        {
            Debug.LogError("출발 무기 6종과 무기 선택 화면을 연결해야 합니다.", this);
            return;
        }
        if (weaponSelection == null) weaponSelection = Instantiate(weaponSelectionPrefab);
        weaponSelection.Show(startingWeapons, LoadRun);
    }

    private void LoadRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenUpgrade()
    {
        if (upgradeScreen == null) return;
        upgradeScreen.Show();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        // 에디터에서는 종료할 것이 없으니 플레이를 멈추는 것으로 대신한다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
