using System;
using UnityEngine;

/// <summary>One physical weapon's skill slot. Empty stays empty after dropping or re-equipping.</summary>
[Serializable]
public sealed class WeaponLoadout
{
    [SerializeField] private WeaponStatsProfile weapon;
    [SerializeField] private WeaponSkillProfile specialAttack;
    public WeaponStatsProfile Weapon => weapon;
    public WeaponSkillProfile SpecialAttack => weapon != null && weapon.LockSpecialAttack
        ? weapon.DefaultSpecialAttack : specialAttack;

    public WeaponLoadout(WeaponStatsProfile profile)
    {
        weapon = profile;
        specialAttack = profile != null ? profile.DefaultSpecialAttack : null;
    }

    public bool CanReplace(WeaponSkillProfile skill)
    {
        return weapon != null && !weapon.LockSpecialAttack && skill != null &&
            skill.IsTransferable && skill.CanUseOn(weapon) && SpecialAttack != skill &&
            (SpecialAttack == null || SpecialAttack.IsTransferable);
    }

    public bool TryReplace(WeaponSkillProfile skill, out WeaponSkillProfile previous)
    {
        previous = null;
        if (!CanReplace(skill)) return false;
        previous = SpecialAttack;
        specialAttack = skill;
        return true;
    }

    public bool CanExtract => weapon != null && !weapon.LockSpecialAttack &&
        SpecialAttack != null && SpecialAttack.IsTransferable;

    public bool TryExtract(out WeaponSkillProfile extracted)
    {
        extracted = null;
        if (!CanExtract) return false;
        extracted = specialAttack;
        specialAttack = null;
        return true;
    }
}
