using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫 화면에서 개조 패널을 띄우고 닫는 껍데기. 패널 자체는 결과 화면과 같은
/// 프리팹을 안에 품고 있어, 손보면 두 곳에 함께 반영된다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Meta Upgrade Screen")]
public class MetaUpgradeScreen : MonoBehaviour
{
    [Tooltip("띄우고 감출 대상. 비워 두면 이 오브젝트 자체를 켜고 끈다.")]
    [SerializeField] private GameObject content;
    [Tooltip("닫는 버튼.")]
    [SerializeField] private Button closeButton;
    [SerializeField] private string closeLabel = "돌아가기";

    private void Awake()
    {
        if (content == null) content = gameObject;

        if (closeButton != null)
        {
            TMPro.TMP_Text label = closeButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            GameFontManager.ApplyFont(label);
            if (label != null) label.text = closeLabel;
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }

    public bool IsOpen => content != null && content.activeSelf;

    public void Show()
    {
        if (content != null) content.SetActive(true);
    }

    public void Hide()
    {
        if (content != null) content.SetActive(false);
    }
}
