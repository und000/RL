using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[AddComponentMenu("Player/Player Weapon Equipment")]
public class PlayerWeaponEquipment : MonoBehaviour
{
    [Header("Equipped Weapon")]
    [SerializeField] private WeaponStatsProfile equippedWeapon;
    [SerializeField] private Transform weaponContainer;
    [Tooltip("Game에서는 마을에서 선택한 시작 무기를 적용하고 런 중 무기 교체를 잠근다. 비워 두면 테스트 씬의 기존 장착을 사용한다.")]
    [SerializeField] private StartingWeaponCatalog startingWeapons;
    public bool IsRunWeaponLocked { get; private set; }

    private WeaponLoadout loadout;
    private MeleeWeaponAttack attack;
    public event Action OnWeaponChanged;
    public WeaponLoadout Loadout => loadout;
    public WeaponSkillProfile EquippedSpecialAttack => loadout != null ? loadout.SpecialAttack : null;
    public bool CanChangeSpecialAttack => attack != null && attack.CanChangeSpecialAttack && Time.timeScale > 0f;
    public bool CanExtractSpecialAttack => CanChangeSpecialAttack && loadout != null && loadout.CanExtract;
    public WeaponStatsProfile EquippedWeapon => equippedWeapon;
    public GameObject WeaponInstance { get; private set; }

    private void Awake()
    {
        if (weaponContainer == null) weaponContainer = transform;
        if (startingWeapons != null)
        {
            if (!startingWeapons.IsValid)
            {
                Debug.LogError("시작 무기 목록이 올바르지 않습니다.", this);
                enabled = false;
                return;
            }
            equippedWeapon = startingWeapons.Resolve(MetaProgress.SelectedWeaponFamily);
            IsRunWeaponLocked = true;
        }
        loadout = equippedWeapon != null ? new WeaponLoadout(equippedWeapon) : null;
        SpawnEquippedWeapon();
    }

    public void Equip(WeaponStatsProfile weapon)
    {
        if (IsRunWeaponLocked) return;
        if (equippedWeapon == weapon && WeaponInstance != null) return;
        Equip(new WeaponLoadout(weapon));
    }

    public void Equip(WeaponLoadout state)
    {
        if (IsRunWeaponLocked) return;
        if (state == loadout && WeaponInstance != null) return;
        loadout = state;
        equippedWeapon = state != null ? state.Weapon : null;
        SpawnEquippedWeapon();
        OnWeaponChanged?.Invoke();
    }

    public bool CanEquipSpecialAttack(WeaponSkillProfile skill)
    {
        return CanChangeSpecialAttack && loadout != null && loadout.CanReplace(skill) && attack.CanSetSpecialAttack(skill);
    }

    public bool TryEquipSpecialAttack(WeaponSkillProfile skill, out WeaponSkillProfile previous)
    {
        previous = null;
        if (!CanEquipSpecialAttack(skill) || !attack.SetSpecialAttack(skill)) return false;
        if (!loadout.TryReplace(skill, out previous)) return false;
        OnWeaponChanged?.Invoke();
        return true;
    }

    public bool TryExtractSpecialAttack(out WeaponSkillProfile extracted)
    {
        extracted = null;
        if (!CanExtractSpecialAttack || !attack.SetSpecialAttack(null)) return false;
        if (!loadout.TryExtract(out extracted)) return false;
        OnWeaponChanged?.Invoke();
        return true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[GameInputKeys.ExtractSpecialAttack].wasPressedThisFrame || !CanExtractSpecialAttack) return;
        ItemDropSpawner spawner = GetComponentInParent<ItemDropSpawner>();
        if (spawner == null) spawner = transform.root.GetComponentInChildren<ItemDropSpawner>(true);
        if (spawner == null || !spawner.CanDrop) return;
        if (TryExtractSpecialAttack(out WeaponSkillProfile extracted)) spawner.DropSpecialAttack(extracted);
    }

    public void Unequip()
    {
        if (IsRunWeaponLocked) return;
        equippedWeapon = null;
        loadout = null;
        ClearWeaponInstance();
        OnWeaponChanged?.Invoke();
    }

    public bool CanEquipWeapon(WeaponStatsProfile weapon) => !IsRunWeaponLocked && weapon != null;

    /// <summary>강화 방식이 정해질 때까지 교체 무기 보상은 런에서 제외한다.</summary>
    public bool CanOfferReward(RoomRewardDefinition reward)
    {
        if (reward == null) return false;
        if (!IsRunWeaponLocked) return true;
        if (reward.Kind == RoomRewardKind.Weapon) return false;
        if (reward.Kind == RoomRewardKind.SpecialAttack)
            return reward.SpecialAttack != null && reward.SpecialAttack.IsTransferable &&
                reward.SpecialAttack.CanUseOn(equippedWeapon);
        return true;
    }

    private void SpawnEquippedWeapon()
    {
        ClearWeaponInstance();
        if (equippedWeapon == null || equippedWeapon.WeaponPrefab == null) return;

        WeaponInstance = Instantiate(equippedWeapon.WeaponPrefab, weaponContainer);
        WeaponInstance.name = equippedWeapon.WeaponPrefab.name;
        Transform instanceTransform = WeaponInstance.transform;
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.one;

        attack = WeaponInstance.GetComponentInChildren<MeleeWeaponAttack>(true);
        foreach (MeleeWeaponAttack receiver in WeaponInstance.GetComponentsInChildren<MeleeWeaponAttack>(true))
        {
            receiver.Configure(equippedWeapon);
            receiver.SetSpecialAttack(loadout != null ? loadout.SpecialAttack : null);
        }

        SpriteFractureDissolveVFX[] dissolveEffects =
            WeaponInstance.GetComponentsInChildren<SpriteFractureDissolveVFX>(true);
        foreach (SpriteFractureDissolveVFX dissolveEffect in dissolveEffects)
        {
            if (dissolveEffect != null) dissolveEffect.Configure(equippedWeapon.DissolveVfx);
        }
    }

    private void ClearWeaponInstance()
    {
        attack = null;
        if (WeaponInstance == null) return;
        // Destroy는 프레임 끝에 처리된다. 먼저 꺼야 이전 무기의 판정/이동이 남지 않는다.
        WeaponInstance.SetActive(false);
        Destroy(WeaponInstance);
        WeaponInstance = null;
    }
}
