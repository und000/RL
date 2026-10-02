using TMPro;
using UnityEngine;

/// <summary>가진 재화를 화면에 띄운다. 값이 바뀔 때만 갱신한다.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Player Wallet UI")]
public class PlayerWalletUI : MonoBehaviour
{
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private string format = "{0} C";

    private int lastDisplayed = -1;

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(creditsText);
    }

    private void Start()
    {
        if (playerWallet == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            playerWallet = player != null
                ? player.GetComponentInChildren<PlayerWallet>() : null;
        }

        if (playerWallet == null || creditsText == null)
        {
            Debug.LogError("Player Wallet UI에 지갑과 텍스트가 필요합니다.", this);
            enabled = false;
            return;
        }

        playerWallet.OnCreditsChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerWallet != null) playerWallet.OnCreditsChanged -= Refresh;
    }

    private void Refresh()
    {
        int credits = playerWallet.Credits;
        if (credits == lastDisplayed) return;

        lastDisplayed = credits;
        creditsText.text = string.Format(format, credits);
    }
}
