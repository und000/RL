using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Player/Player Weapon Equipment")]
public class PlayerWeaponEquipment : MonoBehaviour
{
    [Header("Equipped Weapon")]
    [SerializeField] private WeaponStatsProfile equippedWeapon;
    [SerializeField] private Transform weaponContainer;

    public WeaponStatsProfile EquippedWeapon => equippedWeapon;
    public GameObject WeaponInstance { get; private set; }

    private void Awake()
    {
        if (weaponContainer == null) weaponContainer = transform;
        SpawnEquippedWeapon();
    }

    public void Equip(WeaponStatsProfile weapon)
    {
        if (equippedWeapon == weapon && WeaponInstance != null) return;
        equippedWeapon = weapon;
        SpawnEquippedWeapon();
    }

    public void Unequip()
    {
        equippedWeapon = null;
        ClearWeaponInstance();
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

        SpriteFractureDissolveVFX[] dissolveEffects =
            WeaponInstance.GetComponentsInChildren<SpriteFractureDissolveVFX>(true);
        foreach (SpriteFractureDissolveVFX dissolveEffect in dissolveEffects)
        {
            if (dissolveEffect != null) dissolveEffect.Configure(equippedWeapon.DissolveVfx);
        }
    }

    private void ClearWeaponInstance()
    {
        if (WeaponInstance == null) return;
        Destroy(WeaponInstance);
        WeaponInstance = null;
    }
}
