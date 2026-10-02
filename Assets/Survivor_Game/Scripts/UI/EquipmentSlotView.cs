using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 한 부위를 보여 주는 줄. 프리팹에서 미리 짜 두고, 이 스크립트는
/// 거기 있는 오브젝트에 값만 넣는다. 새로 만들지 않으므로 모양은 프리팹에서 고친다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Equipment Slot View")]
public class EquipmentSlotView : MonoBehaviour
{
    [Header("보여 줄 부위")]
    [SerializeField] private EquipmentSlot slot = EquipmentSlot.Frame;

    [Header("구성 요소")]
    [Tooltip("부위 이름. 골격·장갑처럼 고정된 문구가 들어간다.")]
    [SerializeField] private TMP_Text slotNameText;
    [Tooltip("달고 있는 부품 이름. 비어 있으면 안내 문구가 들어간다.")]
    [SerializeField] private TMP_Text itemNameText;
    [Tooltip("등급 색을 칠할 띠.")]
    [SerializeField] private Image gradeBar;

    [Header("문구")]
    [SerializeField] private string emptyText = "비어 있음";
    [SerializeField] private Color emptyColor = new Color(0.45f, 0.48f, 0.55f);

    public EquipmentSlot Slot => slot;

    private void Awake()
    {
        // 첫 캔버스 갱신 전에 폰트를 바꿔야 한글이 한 프레임 네모로 보이지 않는다.
        GameFontManager.ApplyFont(slotNameText);
        GameFontManager.ApplyFont(itemNameText);
        if (slotNameText != null) slotNameText.text = EquipmentSlotInfo.Name(slot);
    }

    /// <summary>이 부위에 달린 것을 보여 준다. null이면 비어 있는 것으로 그린다.</summary>
    public void Bind(EquipmentDefinition equipment)
    {
        if (slotNameText != null) slotNameText.text = EquipmentSlotInfo.Name(slot);

        if (equipment == null)
        {
            if (itemNameText != null)
            {
                itemNameText.text = emptyText;
                itemNameText.color = emptyColor;
            }
            if (gradeBar != null) gradeBar.color = emptyColor;
            return;
        }

        Color color = ItemGradeInfo.Color(equipment.Grade);
        if (itemNameText != null)
        {
            itemNameText.text = equipment.DisplayName;
            itemNameText.color = color;
        }
        if (gradeBar != null) gradeBar.color = color;
    }
}
