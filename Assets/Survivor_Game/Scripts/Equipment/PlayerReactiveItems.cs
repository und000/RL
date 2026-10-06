using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerReactiveItems : MonoBehaviour
{
    private PlayerCombatStats combat;
    private PlayerDodge dodge;
    private PlayerHealth health;
    private PlayerEnergy energy;
    private PlayerEquipment equipment;
    private CoreBoardController board;
    private RunManager run;
    private bool processing;
    // 같은 정의의 칩 여러 개는 공유한다. 재장착/배치/스테이지 전환으로 초기화하지 않는다.
    private readonly Dictionary<ScriptableObject, float> readyAt = new Dictionary<ScriptableObject, float>();
    private readonly HashSet<ScriptableObject> visited = new HashSet<ScriptableObject>();

    public static void Create(Transform player, RunManager owner)
    {
        if (player == null) return;
        PlayerReactiveItems items = player.GetComponent<PlayerReactiveItems>();
        if (items == null) items = player.gameObject.AddComponent<PlayerReactiveItems>();
        items.run = owner;
    }

    private void Awake()
    {
        combat = GetComponentInChildren<PlayerCombatStats>();
        dodge = GetComponentInChildren<PlayerDodge>();
        health = GetComponentInChildren<PlayerHealth>();
        energy = GetComponentInChildren<PlayerEnergy>();
        equipment = GetComponentInChildren<PlayerEquipment>();
        board = FindFirstObjectByType<CoreBoardController>();
    }

    private void OnEnable()
    {
        if (combat != null) combat.OnEnemyHit += HandleHit;
        if (dodge != null) dodge.OnDodgeStarted += HandleDodge;
    }

    private void OnDisable()
    {
        if (combat != null) combat.OnEnemyHit -= HandleHit;
        if (dodge != null) dodge.OnDodgeStarted -= HandleDodge;
    }

    private void HandleHit(bool killed, bool staggered)
    {
        if (killed) Trigger(ReactiveItemTrigger.Kill);
        if (staggered) Trigger(ReactiveItemTrigger.Stagger);
    }

    private void HandleDodge()
    {
        if (run == null || run.CurrentFloor == null) return;
        foreach (RoomInstance room in run.CurrentFloor.Rooms)
            if (room != null && room.IsCombatActive) { Trigger(ReactiveItemTrigger.CombatDodge); return; }
    }

    private void Trigger(ReactiveItemTrigger trigger)
    {
        if (processing || !isActiveAndEnabled || health == null || health.IsDead || Time.timeScale <= 0f ||
            (run != null && (run.IsRunOver || run.IsTransitioning))) return;
        processing = true;
        try { ApplyActiveItems(trigger); }
        finally { processing = false; }
    }

    private void ApplyActiveItems(ReactiveItemTrigger trigger)
    {
        visited.Clear();
        if (equipment != null)
            foreach (EquipmentSlot slot in EquipmentSlotInfo.All)
            {
                EquipmentDefinition item = equipment.GetEquipped(slot);
                if (item != null) Apply(item, item.ReactiveEffect, trigger);
            }
        if (board == null || !board.IsReady) return;
        foreach (PlacedChip placed in board.State.Placements)
        {
            ChipDefinition chip = placed.Chip;
            if (chip != null && (!chip.NeedsCurrent || board.State.IsEnergized(placed)))
                Apply(chip, chip.ReactiveEffect, trigger);
        }
    }

    private void Apply(ScriptableObject item, ReactiveItemEffect effect, ReactiveItemTrigger trigger)
    {
        if (effect == null || !effect.IsConfigured || effect.trigger != trigger || !visited.Add(item)) return;
        if (readyAt.TryGetValue(item, out float deadline) && Time.time < deadline) return;
        if (effect.action == ReactiveItemAction.RestoreEnergy)
        {
            if (energy == null || energy.CurrentEnergy >= energy.MaxEnergy) return;
        }
        else if (health.GetCurrentHealth() >= health.GetMaxHealth()) return;
        // 회복 이벤트 구독자의 재진입 전에 쿨다운을 기록한다.
        readyAt[item] = Time.time + Mathf.Max(0f, effect.cooldown);
        if (effect.action == ReactiveItemAction.RestoreEnergy) energy.Restore(effect.amount);
        else health.Heal(effect.amount);
    }
}
