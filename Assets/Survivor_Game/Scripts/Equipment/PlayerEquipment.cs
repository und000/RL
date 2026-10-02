using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 부위마다 달고 있는 장비. 새로 달면 원래 것을 돌려주므로,
/// 부르는 쪽에서 그것을 바닥에 떨구든 버리든 정할 수 있다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Player/Player Equipment")]
public class PlayerEquipment : MonoBehaviour
{
    [Tooltip("런을 시작할 때 달고 있는 장비. 같은 부위가 겹치면 뒤쪽이 이긴다.")]
    [SerializeField] private List<EquipmentDefinition> startingEquipment =
        new List<EquipmentDefinition>();

    /// <summary>달거나 뗄 때마다 호출된다. 스탯을 다시 계산하는 쪽이 구독한다.</summary>
    public event Action OnEquipmentChanged;

    private readonly Dictionary<EquipmentSlot, EquipmentDefinition> equipped =
        new Dictionary<EquipmentSlot, EquipmentDefinition>();

    private void Awake()
    {
        foreach (EquipmentDefinition definition in startingEquipment)
        {
            if (definition != null) equipped[definition.Slot] = definition;
        }
    }

    public EquipmentDefinition GetEquipped(EquipmentSlot slot) =>
        equipped.TryGetValue(slot, out EquipmentDefinition definition) ? definition : null;

    public bool IsEquipped(EquipmentDefinition definition) =>
        definition != null && GetEquipped(definition.Slot) == definition;

    /// <summary>
    /// 이 장비를 단다. 같은 것을 이미 달고 있으면 아무 일도 하지 않고 false.
    /// 성공하면 밀려난 장비를 previous로 돌려준다(없으면 null).
    /// </summary>
    public bool TryEquip(EquipmentDefinition definition, out EquipmentDefinition previous)
    {
        previous = null;
        if (definition == null) return false;

        EquipmentDefinition current = GetEquipped(definition.Slot);
        if (current == definition) return false;

        previous = current;
        equipped[definition.Slot] = definition;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>그 부위를 비운다. 떼어낸 장비를 돌려준다.</summary>
    public EquipmentDefinition Unequip(EquipmentSlot slot)
    {
        EquipmentDefinition current = GetEquipped(slot);
        if (current == null) return null;

        equipped.Remove(slot);
        OnEquipmentChanged?.Invoke();
        return current;
    }

    /// <summary>달고 있는 장비 전체의 스탯을 합산한다.</summary>
    public CoreBoardStats BuildStats()
    {
        CoreBoardStats stats = new CoreBoardStats();
        foreach (EquipmentDefinition definition in equipped.Values)
        {
            if (definition != null) definition.ApplyModifiers(stats);
        }
        return stats;
    }
}
