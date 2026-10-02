using UnityEngine;

/// <summary>
/// 코어 보드가 계산한 스탯을 플레이어 컴포넌트에 실제로 반영한다.
/// 인스펙터에 적힌 값을 기준값으로 한 번 기억해 두고, 배치가 바뀔 때마다
/// "기준값 + 보드 보너스"를 통째로 다시 써 주므로 값이 누적되지 않는다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Core Board/Core Board Stat Applier")]
public class CoreBoardStatApplier : MonoBehaviour
{
    [Header("보드")]
    [Tooltip("비워 두면 씬에서 찾는다.")]
    [SerializeField] private CoreBoardController board;

    [Header("반영 대상")]
    [Tooltip("전부 비워 두면 이 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerCombatStats combatStats;
    [SerializeField] private PlayerEnergy energy;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerPickupRange pickupRange;

    [Header("함께 더할 것")]
    [Tooltip("장비 보너스도 같은 스탯 항목을 쓴다. 따로 반영하면 서로 덮어쓰므로 " +
        "여기서 보드 값과 합쳐 한 번에 써 준다. 비워 두면 이 오브젝트에서 찾는다.")]
    [SerializeField] private PlayerEquipment equipment;

    private int baseFlatAttack;
    private float baseDamageIncreaseRate;
    private float baseArmorPenetrationRate;
    private int baseFlatArmorPenetration;
    private float baseTrueDamageRate;
    private float basePickupRange;
    private bool baseCaptured;
    private CoreBoardStats latestBoardStats;
    private CoreBoardStats latestGear;

    /// <summary>
    /// 보드가 모아 준 쿨다운 감소율. 스킬 쪽에 아직 연결하지 않았으므로
    /// 값만 들고 있는다. 3단계에서 스킬 컨트롤러가 여기서 읽어 가면 된다.
    /// </summary>
    public float CooldownReductionRate { get; private set; }

    private void Awake()
    {
        if (combatStats == null) combatStats = GetComponent<PlayerCombatStats>();
        if (energy == null) energy = GetComponent<PlayerEnergy>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (pickupRange == null) pickupRange = GetComponent<PlayerPickupRange>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
    }

    private void Start()
    {
        if (board == null) board = FindFirstObjectByType<CoreBoardController>();
        if (board == null)
        {
            Debug.LogWarning("씬에 CoreBoardController가 없어 보드 스탯을 반영하지 않습니다.", this);
            enabled = false;
            return;
        }

        CaptureBaseValues();
        board.OnBoardChanged += Apply;
        if (equipment != null) equipment.OnEquipmentChanged += ReapplyLatest;
        Apply(board.Stats);
    }

    private void OnDestroy()
    {
        if (board != null) board.OnBoardChanged -= Apply;
        if (equipment != null) equipment.OnEquipmentChanged -= ReapplyLatest;
    }

    /// <summary>장비만 바뀐 경우. 보드 값은 그대로 두고 합산만 다시 한다.</summary>
    private void ReapplyLatest()
    {
        if (latestBoardStats != null) Apply(latestBoardStats);
    }

    /// <summary>인스펙터에 적힌 값을 기준값으로 한 번만 기억한다.</summary>
    private void CaptureBaseValues()
    {
        if (baseCaptured) return;
        baseCaptured = true;

        if (combatStats != null)
        {
            baseFlatAttack = combatStats.GetEquipmentFlatAttack();
            baseDamageIncreaseRate = combatStats.GetDamageIncreaseRate();
            baseArmorPenetrationRate = combatStats.GetArmorPenetrationRate();
            baseFlatArmorPenetration = combatStats.GetFlatArmorPenetration();
            baseTrueDamageRate = combatStats.GetTrueDamageRate();
        }
        if (pickupRange != null) basePickupRange = pickupRange.GetPickupRange();
    }

    private void Apply(CoreBoardStats stats)
    {
        if (stats == null) return;
        latestBoardStats = stats;

        // 보드와 장비가 같은 스탯 항목을 쓰므로 여기서 한 번에 더한다.
        latestGear = equipment != null ? equipment.BuildStats() : null;

        if (combatStats != null)
        {
            combatStats.SetEquipmentFlatAttack(
                baseFlatAttack + TotalInt(ChipStatKind.FlatAttack));
            combatStats.SetDamageIncreaseRate(
                baseDamageIncreaseRate + Total(ChipStatKind.DamageIncreaseRate));
            combatStats.SetArmorPenetrationRate(
                baseArmorPenetrationRate + Total(ChipStatKind.ArmorPenetrationRate));
            combatStats.SetFlatArmorPenetration(
                baseFlatArmorPenetration + TotalInt(ChipStatKind.FlatArmorPenetration));
            combatStats.SetTrueDamageRate(
                baseTrueDamageRate + Total(ChipStatKind.TrueDamageRate));
        }

        if (energy != null)
        {
            energy.SetBonuses(
                TotalInt(ChipStatKind.MaxEnergy),
                Total(ChipStatKind.EnergyRegeneration));
        }

        if (health != null) health.SetBonusMaxHealth(TotalInt(ChipStatKind.MaxHealth));
        if (movement != null) movement.SetBonusSpeedRate(Total(ChipStatKind.MoveSpeedRate));
        if (pickupRange != null)
        {
            pickupRange.SetPickupRange(basePickupRange + Total(ChipStatKind.PickupRange));
        }

        CooldownReductionRate = Total(ChipStatKind.CooldownReductionRate);
    }

    /// <summary>
    /// 보드·장비·영구 개조를 합친 값. 없는 쪽은 0으로 친다.
    /// 세삷이 같은 항목을 쓰므로 따로 반영하면 서로 덮어쓴다.
    /// </summary>
    private float Total(ChipStatKind kind)
    {
        float value = latestBoardStats != null ? latestBoardStats.Get(kind) : 0f;
        if (latestGear != null) value += latestGear.Get(kind);
        value += MetaProgressRuntime.Bonuses.Stats.Get(kind);
        return value;
    }

    private int TotalInt(ChipStatKind kind) => Mathf.RoundToInt(Total(kind));
}
