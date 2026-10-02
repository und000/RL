using TMPro;
using UnityEngine;

/// <summary>
/// 달고 있는 장비 네 부위를 화면에 띄운다. 줄 자체는 프리팹에 미리 만들어 두고,
/// 여기서는 장비가 바뀔 때 값만 다시 넣는다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Player Equipment UI")]
public class PlayerEquipmentUI : MonoBehaviour
{
    [Tooltip("비워 두면 Player 태그가 붙은 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerEquipment playerEquipment;
    [Tooltip("부위별 줄. 프리팹 안에 미리 놓아 둔 것들을 연결한다.")]
    [SerializeField] private EquipmentSlotView[] slotViews;
    [Tooltip("패널 제목. 프리팹에는 폰트 경고를 피하려고 영문 자리표시가 들어 있다.")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string titleLabel = "장비";

    private void Awake()
    {
        // 프리팹에 한글을 저장해 두면 폰트가 붙기 전 한 프레임 네모로 그려진다.
        // 그래서 문구는 여기서 채운다.
        GameFontManager.ApplyFont(titleText);
        if (titleText != null) titleText.text = titleLabel;
    }

    private void Start()
    {
        if (playerEquipment == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            playerEquipment = player != null
                ? player.GetComponentInChildren<PlayerEquipment>(true) : null;
        }

        if (playerEquipment == null)
        {
            Debug.LogWarning("Player Equipment를 찾지 못해 장비 표시가 비어 있습니다.", this);
            Refresh();
            return;
        }

        playerEquipment.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerEquipment != null) playerEquipment.OnEquipmentChanged -= Refresh;
    }

    private void Refresh()
    {
        if (slotViews == null) return;

        foreach (EquipmentSlotView view in slotViews)
        {
            if (view == null) continue;
            view.Bind(playerEquipment != null
                ? playerEquipment.GetEquipped(view.Slot) : null);
        }
    }
}
