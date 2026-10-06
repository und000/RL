using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>결과 이벤트 시점의 문자열을 만든다. 결과창 표시 지연 중 변경된 상태를 다시 읽지 않는다.</summary>
public static class RunResultSummary
{
    public static string PlayerStatus(Transform player, bool failed)
    {
        if (player == null) return "플레이어 기록 없음";
        var text = new StringBuilder();
        PlayerHealth health = player.GetComponentInChildren<PlayerHealth>();
        if (health != null)
        {
            if (failed) text.AppendLine("사망 원인: " + health.DamageHistory.DescribeLastHit());
            text.AppendLine($"피격 {health.DamageHistory.HitCount}회 · 누적 체력 손실 {health.DamageHistory.TotalHealthLost}");
        }
        PlayerLevel level = player.GetComponentInChildren<PlayerLevel>();
        if (level != null) text.AppendLine("도달 레벨 " + level.GetCurrentLevel());
        PlayerWeaponEquipment weapons = player.GetComponentInChildren<PlayerWeaponEquipment>();
        if (weapons != null)
        {
            text.AppendLine("무기: " + (weapons.EquippedWeapon != null ? weapons.EquippedWeapon.DisplayName : "없음"));
            text.AppendLine("특수공격: " + (weapons.EquippedSpecialAttack != null ? weapons.EquippedSpecialAttack.DisplayName : "없음"));
        }
        return text.ToString().TrimEnd();
    }

    private sealed class ChipCount
    {
        public int placed;
        public int active;
    }

    public static string Loadout(PlayerEquipment equipment, CoreBoardController board, ChipInventory inventory)
    {
        var text = new StringBuilder("최종 장비 · 휠/드래그로 목록 이동\n");
        foreach (EquipmentSlot slot in EquipmentSlotInfo.All)
        {
            EquipmentDefinition item = equipment != null ? equipment.GetEquipped(slot) : null;
            text.Append(EquipmentSlotInfo.Name(slot)).Append(": ").AppendLine(item != null ? item.DisplayName : "없음");
            if (item != null && item.ReactiveEffect != null && item.ReactiveEffect.IsConfigured)
                text.AppendLine("  " + item.ReactiveEffect.Describe());
        }
        int active = 0, total = 0;
        var counts = new Dictionary<ChipDefinition, ChipCount>();
        if (board != null && board.IsReady)
            foreach (PlacedChip placed in board.State.Placements)
            {
                if (placed.Chip == null) continue;
                if (!counts.TryGetValue(placed.Chip, out ChipCount count))
                    counts.Add(placed.Chip, count = new ChipCount());
                count.placed++;
                total++;
                if (!placed.Chip.NeedsCurrent || board.State.IsEnergized(placed)) { count.active++; active++; }
            }
        text.AppendLine($"\n보드 칩: 활성 {active} / 배치 {total}");
        text.AppendLine("미장착 칩 " + (inventory != null ? inventory.Count : 0) + "개");
        var chips = new List<ChipDefinition>(counts.Keys);
        chips.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
        foreach (ChipDefinition chip in chips)
        {
            ChipCount count = counts[chip];
            text.AppendLine($"{chip.DisplayName} ×{count.placed} (활성 {count.active})");
        }
        if (chips.Count == 0) text.AppendLine("배치한 칩 없음");
        return text.ToString().TrimEnd();
    }
}
