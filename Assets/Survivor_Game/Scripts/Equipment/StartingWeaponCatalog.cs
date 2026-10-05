using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>마을/출발 준비에서 선택하는 무기군별 시작 무기. 강화 규칙은 별도로 정한다.</summary>
[CreateAssetMenu(fileName = "StartingWeapons", menuName = "Survivor/Weapons/Starting Weapon Catalog")]
public class StartingWeaponCatalog : ScriptableObject
{
    [SerializeField] private WeaponStatsProfile[] weapons = Array.Empty<WeaponStatsProfile>();
    [SerializeField] private WeaponFamily defaultFamily = WeaponFamily.Katana;
    public IReadOnlyList<WeaponStatsProfile> Weapons => weapons;

    public WeaponStatsProfile Find(WeaponFamily family)
    {
        if (weapons == null) return null;
        foreach (WeaponStatsProfile weapon in weapons)
            if (weapon != null && weapon.WeaponPrefab != null && weapon.Family == family) return weapon;
        return null;
    }

    public WeaponStatsProfile Resolve(WeaponFamily savedFamily) => Find(savedFamily) ?? Find(defaultFamily);

    public bool IsValid
    {
        get
        {
            if (weapons == null || weapons.Length != 6 || Find(defaultFamily) == null) return false;
            var families = new HashSet<WeaponFamily>();
            foreach (WeaponStatsProfile weapon in weapons)
                if (weapon == null || weapon.WeaponPrefab == null ||
                    WeaponFamilyUtility.NumberBase(weapon.Family) == 0 || !families.Add(weapon.Family)) return false;
            return true;
        }
    }
}
