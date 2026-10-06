using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 의체에 달 수 있는 부품 하나. 칩과 같은 스탯 어휘를 쓰므로
/// 보드 보너스와 장비 보너스가 같은 길로 플레이어에 반영된다.
/// </summary>
[CreateAssetMenu(
    fileName = "Equipment_New",
    menuName = "Survivor/Equipment/Equipment")]
public class EquipmentDefinition : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string displayName = "부품";
    [SerializeField, TextArea(2, 3)] private string description;
    [SerializeField] private Sprite icon;

    [Header("분류")]
    [Tooltip("달리는 부위. 부위마다 하나씩만 달 수 있다.")]
    [SerializeField] private EquipmentSlot slot = EquipmentSlot.Frame;
    [Tooltip("장비 등급. 바닥에 떨어져 있을 때의 연출 색이 여기서 나온다.")]
    [SerializeField] private ItemGrade grade = ItemGrade.Standard;

    [Header("효과")]
    [Tooltip("칩과 같은 스탯 항목을 쓴다. 여러 줄을 넣으면 모두 더해진다.")]
    [SerializeField] private ChipStatModifier[] modifiers = Array.Empty<ChipStatModifier>();
    [Header("행동 반응 효과")]
    [SerializeField] private ReactiveItemEffect reactiveEffect;
    public ReactiveItemEffect ReactiveEffect => reactiveEffect;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public EquipmentSlot Slot => slot;
    public ItemGrade Grade => grade;
    public IReadOnlyList<ChipStatModifier> Modifiers => modifiers;

    /// <summary>합산 결과에 이 장비의 효과를 더한다.</summary>
    public void ApplyModifiers(CoreBoardStats target)
    {
        if (target == null || modifiers == null) return;
        foreach (ChipStatModifier modifier in modifiers)
        {
            target.Add(modifier);
        }
    }

    /// <summary>"희귀 반응 장갑"처럼 등급까지 붙인 한 줄.</summary>
    public string BuildLabel() =>
        EquipmentSlotInfo.Name(slot) + " · " + DisplayName;
}
