using UnityEngine;

/// <summary>보상이 실제로 무엇을 해 주는가.</summary>
public enum RoomRewardKind
{
    /// <summary>체력을 정해진 수치만큼 회복한다.</summary>
    Heal,
    /// <summary>체력을 최대 체력의 비율만큼 회복한다.</summary>
    HealRatio,
    /// <summary>경험치를 정해진 수치만큼 준다.</summary>
    Experience,
    /// <summary>다음 레벨까지 필요한 경험치의 비율만큼 준다.</summary>
    ExperienceRatio,
    /// <summary>에너지를 가득 채운다.</summary>
    EnergyRefill,
    /// <summary>코어 보드에 꽂을 칩을 준다.</summary>
    Chip,
    /// <summary>코어 보드를 넓힌다. amount만큼 행이 늘어난다.</summary>
    BoardExpand,
    /// <summary>손상 셀 하나를 수리해 쓸 수 있게 만든다.</summary>
    BoardRepair,
    /// <summary>빈 칸 하나를 전원 단자로 바꿔 회로를 하나 더 굴리게 한다.</summary>
    BoardPowerRail,
    /// <summary>빈 칸 하나를 버스 칸으로 바꿔 전류가 멀리 뻗게 한다.</summary>
    BoardBusCell = 10, // 기존 보상 에셋의 직렬화 값을 유지한다.
    /// <summary>무기를 바꿔 준다.</summary>
    Weapon,
    /// <summary>의체 부품을 바꿔 준다.</summary>
    Equipment,
    SpecialAttack = 13
}

/// <summary>
/// 방을 클리어했을 때 받을 수 있는 보상 한 종류.
/// 받침대에 무엇을 띄우고 무엇을 지급할지를 이 에셋 하나에 담는다.
/// 비율 보상은 층이 올라가도 값이 따라 커지므로, 층마다 다른 에셋을 만들지 않아도 된다.
/// </summary>
[CreateAssetMenu(
    fileName = "Reward_New",
    menuName = "Survivor/Rooms/Room Reward")]
public class RoomRewardDefinition : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string displayName = "보상";
    [SerializeField, TextArea(2, 3)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color tintColor = Color.white;

    [Tooltip("이 보상의 등급. 바닥에 놓였을 때의 연출 색과 세기를 정한다.")]
    [SerializeField] private ItemGrade grade = ItemGrade.Standard;

    [Header("효과")]
    [SerializeField] private RoomRewardKind kind = RoomRewardKind.HealRatio;
    [Tooltip("Heal·Experience처럼 고정 수치를 쓰는 보상에서만 읽는다.")]
    [SerializeField, Min(0)] private int amount = 10;
    [Tooltip("비율 보상에서 읽는다. 0.25는 25%.")]
    [SerializeField, Range(0f, 3f)] private float rate = 0.25f;
    [Tooltip("칩 보상일 때 지급할 칩.")]
    [SerializeField] private ChipDefinition chip;
    [Tooltip("무기 보상일 때 장착시킬 무기.")]
    [SerializeField] private WeaponStatsProfile weapon;
    [Tooltip("장비 보상일 때 달아 줄 부품.")]
    [SerializeField] private EquipmentDefinition equipment;

    [Tooltip("Transferable special attack item. Bound unique skills cannot be granted as items.")]
    [SerializeField] private WeaponSkillProfile specialAttack;
    private WeaponLoadout runtimeWeaponLoadout;

    [Header("상점")]
    [Tooltip("상점 방에 놓였을 때의 기본 가격. 층 배율이 여기에 곱해진다.")]
    [SerializeField, Min(0)] private int shopPrice = 60;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
            if (kind == RoomRewardKind.Chip && chip != null) return chip.DisplayName;
            if (kind == RoomRewardKind.Weapon && weapon != null) return weapon.DisplayName;
            if (kind == RoomRewardKind.SpecialAttack && specialAttack != null) return specialAttack.DisplayName;
            if (kind == RoomRewardKind.Equipment && equipment != null)
            {
                return equipment.BuildLabel();
            }
            return name;
        }
    }

    public string Description => description;
    public string ReactiveDescription
    {
        get
        {
            ReactiveItemEffect effect = kind == RoomRewardKind.Equipment && equipment != null
                ? equipment.ReactiveEffect : kind == RoomRewardKind.Chip && chip != null ? chip.ReactiveEffect : null;
            if (effect == null || !effect.IsConfigured) return string.Empty;
            return effect.Describe() + (kind == RoomRewardKind.Chip ? "\n활성 배치 필요 · 동일 칩 발동 중복 없음" : string.Empty);
        }
    }
    public Sprite Icon => icon != null ? icon : kind == RoomRewardKind.Weapon && weapon != null
        ? weapon.Icon : kind == RoomRewardKind.SpecialAttack && specialAttack != null
        ? specialAttack.Icon : kind == RoomRewardKind.Equipment && equipment != null
        ? equipment.Icon : chip != null ? chip.Icon : null;
    public WeaponSkillProfile SpecialAttack => specialAttack;
    public Color TintColor => tintColor;
    public RoomRewardKind Kind => kind;
    public int Amount => amount;
    public float Rate => rate;
    public ChipDefinition Chip => chip;
    public WeaponStatsProfile Weapon => weapon;
    public EquipmentDefinition Equipment => equipment;

    /// <summary>
    /// 실제로 쓰일 등급. 무기 보상은 무기 자신의 등급을 따라가고,
    /// 칩 보상은 칩 희귀도를 등급으로 옮긴다. 그래야 등급을 한 곳에만 적어 둔다.
    /// </summary>
    public ItemGrade Grade
    {
        get
        {
            if (kind == RoomRewardKind.Weapon && weapon != null) return weapon.Grade;
            if (kind == RoomRewardKind.Equipment && equipment != null) return equipment.Grade;
            if (kind == RoomRewardKind.Chip && chip != null) return FromChipRarity(chip.Rarity);
            return grade;
        }
    }

    public Color GradeColor => ItemGradeInfo.Color(Grade);

    /// <summary>칩은 자기 희귀도 등급을 따로 쓰므로 여기서 맞춰 준다.</summary>
    private static ItemGrade FromChipRarity(ChipRarity rarity)
    {
        switch (rarity)
        {
            case ChipRarity.Rare: return ItemGrade.Rare;
            case ChipRarity.Epic: return ItemGrade.Classified;
            case ChipRarity.Unique: return ItemGrade.TopSecret;
            default: return ItemGrade.Reinforced;
        }
    }

    /// <summary>에셋이 아니라 코드에서 즉석으로 만든 보상인가. 쓰고 나면 버려야 한다.</summary>
    public bool IsRuntimeCopy { get; private set; }

    /// <summary>
    /// 교체로 밀려난 무기를 바닥에 떨구려면 그 무기만 담은 보상이 필요한데,
    /// 무기마다 보상 에셋이 있으리라는 보장이 없으므로 즉석에서 하나 만든다.
    /// </summary>
    public static RoomRewardDefinition CreateRuntimeWeaponReward(
        WeaponStatsProfile weapon)
    {
        if (weapon == null) return null;

        RoomRewardDefinition created = CreateInstance<RoomRewardDefinition>();
        created.name = "RuntimeWeaponReward_" + weapon.name;
        created.kind = RoomRewardKind.Weapon;
        created.weapon = weapon;
        created.displayName = weapon.DisplayName;
        created.tintColor = new Color(0.85f, 0.85f, 0.9f);
        created.IsRuntimeCopy = true;
        return created;
    }

    public static RoomRewardDefinition CreateRuntimeWeaponReward(WeaponLoadout loadout)
    {
        if (loadout == null || loadout.Weapon == null) return null;
        RoomRewardDefinition created = CreateRuntimeWeaponReward(loadout.Weapon);
        created.runtimeWeaponLoadout = loadout;
        return created;
    }

    public static RoomRewardDefinition CreateRuntimeSpecialAttackReward(WeaponSkillProfile skill)
    {
        if (skill == null || !skill.IsTransferable) return null;
        RoomRewardDefinition created = CreateInstance<RoomRewardDefinition>();
        created.name = "RuntimeSpecialAttack_" + skill.name;
        created.kind = RoomRewardKind.SpecialAttack;
        created.specialAttack = skill;
        created.displayName = skill.DisplayName;
        created.IsRuntimeCopy = true;
        return created;
    }

    /// <summary>밀려난 장비를 바닥에 떨구기 위해 그 부품만 담은 보상을 즉석에서 만든다.</summary>
    public static RoomRewardDefinition CreateRuntimeEquipmentReward(
        EquipmentDefinition equipment)
    {
        if (equipment == null) return null;

        RoomRewardDefinition created = CreateInstance<RoomRewardDefinition>();
        created.name = "RuntimeEquipmentReward_" + equipment.name;
        created.kind = RoomRewardKind.Equipment;
        created.equipment = equipment;
        created.displayName = equipment.BuildLabel();
        created.tintColor = new Color(0.85f, 0.85f, 0.9f);
        created.IsRuntimeCopy = true;
        return created;
    }

    public int ShopPrice => Mathf.Max(0, shopPrice);

    /// <summary>받침대에 띄울 한 줄. 수치가 있는 보상은 수치까지 보여 준다.</summary>
    public string BuildLabel()
    {
        switch (kind)
        {
            case RoomRewardKind.Heal:
                return $"{DisplayName}  +{amount}";
            case RoomRewardKind.HealRatio:
                return $"{DisplayName}  +{Mathf.RoundToInt(rate * 100f)}% HP";
            case RoomRewardKind.Experience:
                return $"{DisplayName}  +{amount} EXP";
            case RoomRewardKind.ExperienceRatio:
                return $"{DisplayName}  +{Mathf.RoundToInt(rate * 100f)}% EXP";
            case RoomRewardKind.SpecialAttack:
                return specialAttack != null ? $"{DisplayName} · {specialAttack.MpCost} MP · {specialAttack.AllowedWeaponFamiliesLabel}" : DisplayName;
            case RoomRewardKind.BoardExpand:
                return $"{DisplayName}  보드 +{Mathf.Max(1, amount)}행";
            default:
                return DisplayName;
        }
    }

    /// <summary>보드 지형을 건드리는 보상인가.</summary>
    private bool IsBoardUpgrade =>
        kind == RoomRewardKind.BoardExpand ||
        kind == RoomRewardKind.BoardRepair ||
        kind == RoomRewardKind.BoardPowerRail ||
        kind == RoomRewardKind.BoardBusCell;

    /// <summary>
    /// 지금 이 보상이 실제로 효과가 있는지 본다. 체력이 가득 찬 상태의 회복처럼
    /// 효과가 없는 경우를 미리 알려 주기 위한 것이다.
    /// </summary>
    public bool CanGrant(GameObject player)
    {
        if (player == null) return false;

        switch (kind)
        {
            case RoomRewardKind.Heal:
            case RoomRewardKind.HealRatio:
            {
                PlayerHealth health = player.GetComponentInParent<PlayerHealth>();
                return health != null && !health.IsDead &&
                    health.GetCurrentHealth() < health.GetMaxHealth();
            }
            case RoomRewardKind.EnergyRefill:
            {
                PlayerEnergy energy = player.GetComponentInParent<PlayerEnergy>();
                return energy != null && energy.CurrentEnergy < energy.MaxEnergy;
            }
            case RoomRewardKind.SpecialAttack:
            {
                PlayerWeaponEquipment holder = FindWeaponHolder(player);
                if (holder == null || !holder.CanEquipSpecialAttack(specialAttack)) return false;
                ItemDropSpawner drops = FindDropSpawner(player);
                return holder.EquippedSpecialAttack == null || (drops != null && drops.CanDrop);
            }
            case RoomRewardKind.Chip:
                return chip != null && player.GetComponentInParent<ChipInventory>() != null;
            case RoomRewardKind.Equipment:
            {
                PlayerEquipment slots = FindEquipmentSlots(player);
                // 같은 부품을 이미 달고 있으면 바꿀 것이 없다.
                return equipment != null && slots != null && !slots.IsEquipped(equipment);
            }
            case RoomRewardKind.Weapon:
            {
                PlayerWeaponEquipment holder = FindWeaponHolder(player);
                if (holder == null || !holder.CanEquipWeapon(weapon)) return false;
                if (holder.EquippedWeapon == weapon && (runtimeWeaponLoadout == null || holder.Loadout == runtimeWeaponLoadout)) return false;
                ItemDropSpawner drops = FindDropSpawner(player);
                return holder.Loadout == null || (drops != null && drops.CanDrop);
            }
            default:
                if (IsBoardUpgrade) return CanUpgradeBoard();
                return player.GetComponentInParent<PlayerLevel>() != null;
        }
    }

    /// <summary>
    /// 보드 보상은 실제로 바꿀 자리가 남아 있을 때만 효과가 있다.
    /// 고칠 손상 셀이 없는데 수리를 집어 들면 헛것을 쓰는 셈이라 미리 막는다.
    /// </summary>
    private bool CanUpgradeBoard()
    {
        CoreBoardController board = FindBoard();
        if (board == null || !board.IsReady) return false;

        CoreBoardLayout layout = board.State.Layout;
        switch (kind)
        {
            case RoomRewardKind.BoardExpand:
                return true;
            case RoomRewardKind.BoardRepair:
                return HasFreeCellOfType(board, layout, BoardCellType.Blocked);
            default:
                return HasFreeCellOfType(board, layout, BoardCellType.Empty);
        }
    }

    private static bool HasFreeCellOfType(
        CoreBoardController board, CoreBoardLayout layout, BoardCellType type)
    {
        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (layout.GetCell(cell) != type) continue;
                if (board.State.GetChipAt(cell) != null) continue;
                return true;
            }
        }
        return false;
    }

    /// <summary>보드는 플레이어가 아니라 씬에 따로 있으므로 여기서 찾는다.</summary>
    private static CoreBoardController FindBoard() =>
        Object.FindFirstObjectByType<CoreBoardController>();

    /// <summary>플레이어에게 지급한다. 지급할 수 없으면 false를 돌려주고 아무것도 하지 않는다.</summary>
    public bool Grant(GameObject player)
    {
        if (player == null) return false;

        switch (kind)
        {
            case RoomRewardKind.Heal:
            case RoomRewardKind.HealRatio:
                return GrantHeal(player);
            case RoomRewardKind.Experience:
            case RoomRewardKind.ExperienceRatio:
                return GrantExperience(player);
            case RoomRewardKind.EnergyRefill:
                return GrantEnergy(player);
            case RoomRewardKind.Chip:
                return GrantChip(player);
            case RoomRewardKind.SpecialAttack:
                return GrantSpecialAttack(player);
            case RoomRewardKind.Weapon:
                return GrantWeapon(player);
            case RoomRewardKind.Equipment:
                return GrantEquipment(player);
            default:
                return IsBoardUpgrade && GrantBoardUpgrade();
        }
    }

    private bool GrantBoardUpgrade()
    {
        CoreBoardController board = FindBoard();
        if (board == null || !board.IsReady)
        {
            Debug.LogWarning(
                $"보드 보상 '{DisplayName}'을 줄 CoreBoardController가 씬에 없습니다.", this);
            return false;
        }

        switch (kind)
        {
            case RoomRewardKind.BoardExpand:
                return board.ExpandBoard(0, Mathf.Max(1, amount));
            case RoomRewardKind.BoardRepair:
                return board.RepairDamagedCell();
            case RoomRewardKind.BoardPowerRail:
                return board.AddPowerRail();
            case RoomRewardKind.BoardBusCell:
                return board.AddBusCell();
            default:
                return false;
        }
    }

    private bool GrantHeal(GameObject player)
    {
        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();
        if (health == null) return false;

        int healAmount = kind == RoomRewardKind.HealRatio
            ? Mathf.Max(1, Mathf.RoundToInt(health.GetMaxHealth() * rate))
            : amount;
        return healAmount > 0 && health.Heal(healAmount);
    }

    private bool GrantExperience(GameObject player)
    {
        PlayerLevel level = player.GetComponentInParent<PlayerLevel>();
        if (level == null) return false;

        int experience = kind == RoomRewardKind.ExperienceRatio
            ? Mathf.Max(1, Mathf.RoundToInt(level.GetExperienceToNextLevel() * rate))
            : amount;
        if (experience <= 0) return false;

        level.AddExperience(experience);
        return true;
    }

    private bool GrantEnergy(GameObject player)
    {
        PlayerEnergy energy = player.GetComponentInParent<PlayerEnergy>();
        if (energy == null) return false;
        energy.RefillToMax();
        return true;
    }

    private bool GrantChip(GameObject player)
    {
        if (chip == null) return false;
        ChipInventory inventory = player.GetComponentInParent<ChipInventory>();
        if (inventory == null)
        {
            Debug.LogWarning(
                $"칩 보상 '{DisplayName}'을 줄 Chip Inventory가 플레이어에 없습니다.", this);
            return false;
        }
        inventory.Add(chip);
        return true;
    }

    private bool GrantSpecialAttack(GameObject player)
    {
        if (!CanGrant(player)) return false;
        PlayerWeaponEquipment holder = FindWeaponHolder(player);
        if (!holder.TryEquipSpecialAttack(specialAttack, out WeaponSkillProfile previous)) return false;
        if (previous != null) FindDropSpawner(player).DropSpecialAttack(previous);
        return true;
    }

    public string GetUnavailableReason(GameObject player)
    {
        if (kind == RoomRewardKind.Weapon && player != null && FindWeaponHolder(player)?.IsRunWeaponLocked == true)
            return "출발할 때 선택한 무기 유지";
        if (kind != RoomRewardKind.SpecialAttack) return "효과 없음";
        PlayerWeaponEquipment holder = player != null ? FindWeaponHolder(player) : null;
        if (holder == null || holder.EquippedWeapon == null) return "무기 필요";
        if (holder.EquippedWeapon.LockSpecialAttack) return "유니크 무기 · 교체 불가";
        if (specialAttack == null || !specialAttack.IsTransferable) return "전용 기술 · 이전 불가";
        if (!specialAttack.CanUseOn(holder.EquippedWeapon)) return $"사용 가능: {specialAttack.AllowedWeaponFamiliesLabel}";
        if (holder.EquippedSpecialAttack == specialAttack) return "이미 장착됨";
        if (!holder.CanChangeSpecialAttack) return "행동 종료 후 교체 가능";
        return "장착 불가";
    }

    private bool GrantWeapon(GameObject player)
    {
        if (weapon == null) return false;
        PlayerWeaponEquipment holder = FindWeaponHolder(player);
        if (holder == null || !CanGrant(player)) return false;

        WeaponLoadout previous = holder.Loadout;
        holder.Equip(runtimeWeaponLoadout ?? new WeaponLoadout(weapon));

        // 들고 있던 무기는 사라지지 않고 바닥에 남아, 마음이 바뀌면 되돌릴 수 있다.
        if (previous != null)
        {
            ItemDropSpawner spawner = FindDropSpawner(player);
            if (spawner != null) spawner.Drop(previous);
        }
        return true;
    }

    /// <summary>
    /// 무기 장착 컴포넌트는 플레이어 뿌리가 아니라 Body 같은 자식 오브젝트에 달려 있다.
    /// 부모 방향으로만 찾으면 놓치므로 뿌리에서 자식까지 훑는다.
    /// </summary>
    private static PlayerWeaponEquipment FindWeaponHolder(GameObject player)
    {
        PlayerWeaponEquipment holder =
            player.GetComponentInParent<PlayerWeaponEquipment>();
        if (holder != null) return holder;

        Transform root = player.transform.root;
        return root != null
            ? root.GetComponentInChildren<PlayerWeaponEquipment>(true)
            : null;
    }

    private bool GrantEquipment(GameObject player)
    {
        if (equipment == null) return false;
        PlayerEquipment slots = FindEquipmentSlots(player);
        if (slots == null) return false;
        if (!slots.TryEquip(equipment, out EquipmentDefinition previous)) return false;

        // 밀려난 부품은 사라지지 않고 바닥에 남아, 마음이 바뀌면 되돌릴 수 있다.
        if (previous != null)
        {
            ItemDropSpawner spawner = FindDropSpawner(player);
            if (spawner != null) spawner.Drop(previous);
        }
        return true;
    }

    private static PlayerEquipment FindEquipmentSlots(GameObject player)
    {
        PlayerEquipment slots = player.GetComponentInParent<PlayerEquipment>();
        if (slots != null) return slots;

        Transform root = player.transform.root;
        return root != null ? root.GetComponentInChildren<PlayerEquipment>(true) : null;
    }

    private static ItemDropSpawner FindDropSpawner(GameObject player)
    {
        ItemDropSpawner spawner = player.GetComponentInParent<ItemDropSpawner>();
        if (spawner != null) return spawner;

        Transform root = player.transform.root;
        return root != null ? root.GetComponentInChildren<ItemDropSpawner>(true) : null;
    }

    private void OnValidate()
    {
        amount = Mathf.Max(0, amount);
        rate = Mathf.Clamp(rate, 0f, 3f);
        shopPrice = Mathf.Max(0, shopPrice);
    }
}
