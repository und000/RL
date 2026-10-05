using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>출발 준비 화면. 선택 확정 전에는 저장된 무기군을 바꾸지 않는다.</summary>
[DisallowMultipleComponent]
public class StartingWeaponSelectionUI : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public Button button;
        public TMP_Text title;
        public TMP_Text description;
        public Image icon;
        public GameObject selectedMarker;
    }
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text subtitle;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Slot[] slots = Array.Empty<Slot>();
    private StartingWeaponCatalog catalog;
    private WeaponStatsProfile selected;
    private Action onConfirm;
    public bool IsConfigured => panel != null && confirmButton != null && cancelButton != null &&
        slots != null && slots.Length == 6 && Array.TrueForAll(slots, s => s != null && s.button != null);

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            if (slots[i]?.button != null) slots[i].button.onClick.AddListener(() => Select(index));
        }
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) GameFontManager.ApplyFont(text);
        SetLabel(confirmButton, "이 무기로 출발");
        SetLabel(cancelButton, "돌아가기");
    }

    public bool Show(StartingWeaponCatalog choices, Action confirmed)
    {
        if (!IsConfigured || choices == null || !choices.IsValid) return false;
        catalog = choices;
        selected = catalog.Resolve(MetaProgress.SelectedWeaponFamily);
        onConfirm = confirmed;
        if (heading != null) heading.text = "출발 무기 선택";
        if (subtitle != null) subtitle.text = "이번 탐험에서 사용할 무기군을 선택하세요";
        for (int i = 0; i < slots.Length; i++)
        {
            WeaponStatsProfile weapon = catalog.Weapons[i];
            if (slots[i].title != null) slots[i].title.text = WeaponFamilyUtility.Label(weapon.Family);
            if (slots[i].description != null) slots[i].description.text = weapon.DisplayName;
            if (slots[i].icon != null)
            {
                slots[i].icon.sprite = weapon.Icon;
                slots[i].icon.preserveAspect = true;
                slots[i].icon.enabled = weapon.Icon != null;
            }
        }
        RefreshSelection();
        panel.SetActive(true);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            for (int i = 0; i < slots.Length; i++)
                if (catalog.Weapons[i] == selected)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(slots[i].button.gameObject);
        return true;
    }

    private void Select(int index)
    {
        if (catalog == null || index < 0 || index >= catalog.Weapons.Count) return;
        selected = catalog.Weapons[index];
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].selectedMarker != null) slots[i].selectedMarker.SetActive(catalog.Weapons[i] == selected);
        confirmButton.interactable = selected != null;
    }

    private void Confirm()
    {
        if (panel == null || !panel.activeSelf || selected == null ||
            !MetaProgress.SelectWeaponFamily(selected.Family)) return;
        Action callback = onConfirm;
        Hide();
        callback?.Invoke();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        onConfirm = null;
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame) Hide();
    }

    private static void SetLabel(Button button, string value)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (label != null) label.text = value;
    }
}
