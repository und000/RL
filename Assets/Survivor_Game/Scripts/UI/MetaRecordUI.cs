using TMPro;
using UnityEngine;

/// <summary>
/// 여태 남긴 기록을 첫 화면에 걸어 둔다. 줄은 프리팹에 미리 만들어 두고
/// 여기서는 값만 채운다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Meta Record UI")]
public class MetaRecordUI : MonoBehaviour
{
    [Header("구성 요소")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("지금 가진 잔존 코드.")]
    [SerializeField] private TMP_Text salvageText;
    [Tooltip("가장 멀리 간 지점.")]
    [SerializeField] private TMP_Text bestText;
    [Tooltip("시도와 돌파 횟수.")]
    [SerializeField] private TMP_Text countText;

    [Header("문구")]
    [SerializeField] private string titleLabel = "회수 기록";
    [SerializeField] private string salvageFormat = "잔존 코드 {0}";
    [SerializeField] private string bestFormat = "최고 도달 {0}";
    [SerializeField] private string countFormat = "시도 {0}회 · 돌파 {1}회";
    [SerializeField] private string noRecordLabel = "기록 없음";

    private void Awake()
    {
        // 프리팹에 한글을 저장해 두면 폰트가 붙기 전 한 프레임 네모로 그려진다.
        GameFontManager.ApplyFont(titleText);
        GameFontManager.ApplyFont(salvageText);
        GameFontManager.ApplyFont(bestText);
        GameFontManager.ApplyFont(countText);
        if (titleText != null) titleText.text = titleLabel;
    }

    private void OnEnable()
    {
        MetaProgress.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        MetaProgress.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        MetaProgressState state = MetaProgress.State;

        if (salvageText != null)
        {
            salvageText.text = string.Format(salvageFormat, state.salvage);
        }
        if (bestText != null)
        {
            string best = state.bestChapter > 0
                ? state.bestChapter + "장 " + state.bestFloor + "층"
                : noRecordLabel;
            bestText.text = string.Format(bestFormat, best);
        }
        if (countText != null)
        {
            countText.text = string.Format(
                countFormat, state.runCount, state.clearCount);
        }
    }
}
