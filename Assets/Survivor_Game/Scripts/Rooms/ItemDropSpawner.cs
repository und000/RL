using UnityEngine;

/// <summary>
/// 무기나 장비를 바꿀 때 밀려난 것을 바닥에 떨군다.
/// 새것이 마음에 들지 않으면 되돌아가 다시 주울 수 있어야 하기 때문이다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Player/Item Drop Spawner")]
public class ItemDropSpawner : MonoBehaviour
{
    [Tooltip("떨어진 물건이 될 드롭 프리팹. 방 보상이 쓰는 것과 같은 것을 넣는다.")]
    [SerializeField] private RewardDrop dropPrefab;
    [Tooltip("발밑에서 얼마나 떨어진 자리에 떨굴지. 벽에 박히지 않게 짧게 둔다.")]
    [SerializeField, Min(0f)] private float dropDistance = 1.8f;

    public bool CanDrop => isActiveAndEnabled && dropPrefab != null;

    public void Drop(WeaponLoadout weapon)
    {
        if (weapon == null || weapon.Weapon == null) return;
        Spawn(RoomRewardDefinition.CreateRuntimeWeaponReward(weapon));
    }

    public void DropSpecialAttack(WeaponSkillProfile skill)
    {
        if (skill == null || !skill.IsTransferable) return;
        Spawn(RoomRewardDefinition.CreateRuntimeSpecialAttackReward(skill));
    }

    /// <summary>이 무기를 바닥에 떨군다.</summary>
    public void Drop(WeaponStatsProfile weapon)
    {
        if (weapon == null) return;
        Spawn(RoomRewardDefinition.CreateRuntimeWeaponReward(weapon));
    }

    /// <summary>이 장비를 바닥에 떨군다.</summary>
    public void Drop(EquipmentDefinition equipment)
    {
        if (equipment == null) return;
        Spawn(RoomRewardDefinition.CreateRuntimeEquipmentReward(equipment));
    }

    private void Spawn(RoomRewardDefinition reward)
    {
        if (reward == null) return;
        if (!CanDrop)
        {
            if (reward.IsRuntimeCopy) Destroy(reward);
            return;
        }

        Vector2 origin = transform.position;
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;

        RewardDrop drop = Instantiate(dropPrefab, origin, Quaternion.identity);
        drop.Configure(reward, origin, origin + direction * dropDistance);
    }
}
