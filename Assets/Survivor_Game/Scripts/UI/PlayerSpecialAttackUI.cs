using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("UI/Player Special Attack UI")]
public class PlayerSpecialAttackUI : MonoBehaviour
{
    [SerializeField] private PlayerWeaponEquipment playerWeapons;
    [SerializeField] private TMP_Text statusText;
    private PlayerEnergy energy;
    private string lastText;

    private void Awake()
    {
        GameFontManager.ApplyFont(statusText);
        if (statusText != null) statusText.raycastTarget = false;
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (playerWeapons == null && player != null)
            playerWeapons = player.GetComponentInChildren<PlayerWeaponEquipment>(true);
        if (player != null) energy = player.GetComponentInChildren<PlayerEnergy>(true);
    }

    private void LateUpdate()
    {
        if (statusText == null || playerWeapons == null) return;
        WeaponStatsProfile weapon = playerWeapons.EquippedWeapon;
        WeaponSkillProfile skill = playerWeapons.EquippedSpecialAttack;
        string line;
        if (weapon == null) line = "무기 없음";
        else if (skill == null) line = weapon.DisplayName + "\n특수공격 없음 · 기술 아이템으로 장착";
        else
        {
            line = weapon.DisplayName + " · " + skill.DisplayName + "\n[우클릭] " + skill.MpCost + " MP";
            if (energy == null || !energy.HasEnergy(skill.MpCost)) line += " · MP 부족";
            line += weapon.LockSpecialAttack || !skill.IsTransferable
                ? "\n고유 기술 · 교체 / 추출 불가"
                : "\n" + GameInputKeys.ExtractSpecialAttackPrompt + " 기술 추출";
        }
        if (lastText == line) return;
        lastText = line;
        statusText.text = line;
    }
}
