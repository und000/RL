using System;
using UnityEngine;

public enum ReactiveItemTrigger { Kill = 0, Stagger = 1, CombatDodge = 2 }
public enum ReactiveItemAction { RestoreEnergy = 0, Heal = 1 }

/// <summary>아이템의 조건부 효과. 런의 쿨다운은 에셋이 아닌 플레이어가 보관한다.</summary>
[Serializable]
public sealed class ReactiveItemEffect
{
    public ReactiveItemTrigger trigger;
    public ReactiveItemAction action;
    [Min(0)] public int amount;
    [Min(0f)] public float cooldown;
    public bool IsConfigured => amount > 0 && Enum.IsDefined(typeof(ReactiveItemTrigger), trigger) &&
        Enum.IsDefined(typeof(ReactiveItemAction), action);

    public string Describe()
    {
        if (!IsConfigured) return string.Empty;
        string condition = trigger == ReactiveItemTrigger.Kill ? "직접 처치" :
            trigger == ReactiveItemTrigger.Stagger ? "붕괴 유발" : "전투 중 회피 시작";
        string resource = action == ReactiveItemAction.Heal ? "체력" : "MP";
        return $"{condition} 시 {resource} +{amount} · 재발동 {Mathf.Max(0f, cooldown):0.#}초";
    }
}
