using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("UI/Player Energy UI")]
public class PlayerEnergyUI : MonoBehaviour
{
    [Tooltip("비워 두면 Player 태그가 붙은 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerEnergy playerEnergy;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energyText;

    private int lastDisplayedEnergy = -1;
    private int lastDisplayedMax = -1;

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(energyText);
    }

    private void Start()
    {
        if (playerEnergy == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            playerEnergy = player != null
                ? player.GetComponentInChildren<PlayerEnergy>(true) : null;
        }

        if (playerEnergy == null || energySlider == null)
        {
            Debug.LogError("Player Energy UI에 필요한 오브젝트가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        DisplayOnlyUI.Configure(energySlider);
        RefreshSlider();
        RefreshText(true);
    }

    private void Update()
    {
        if (playerEnergy == null) return;

        // 회복 중에는 값이 매 프레임 바뀌므로 슬라이더는 항상 갱신하고,
        // 텍스트는 정수 표기가 바뀔 때만 갱신한다.
        RefreshSlider();
        RefreshText(false);
    }

    private void RefreshSlider()
    {
        energySlider.maxValue = playerEnergy.MaxEnergy;
        energySlider.value = playerEnergy.CurrentEnergy;
    }

    private void RefreshText(bool force)
    {
        if (energyText == null) return;

        int current = playerEnergy.CurrentEnergyDisplay;
        int max = playerEnergy.MaxEnergy;
        if (!force && current == lastDisplayedEnergy && max == lastDisplayedMax) return;

        lastDisplayedEnergy = current;
        lastDisplayedMax = max;
        energyText.text = $"{current} / {max}";
    }
}
