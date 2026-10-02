using System;
using UnityEngine;

/// <summary>
/// 런 동안 모으는 재화. 적을 처치할 때 쌓이고 상점 방에서 쓴다.
/// 런이 끝나면(씬을 다시 불러오면) 0부터 다시 시작한다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Player/Player Wallet")]
public class PlayerWallet : MonoBehaviour
{
    [Tooltip("런을 시작할 때 들고 있는 재화.")]
    [SerializeField, Min(0)] private int startingCredits;

    /// <summary>재화가 늘거나 줄 때마다 호출된다.</summary>
    public event Action OnCreditsChanged;

    private int credits;

    public int Credits => credits;

    private void Awake()
    {
        // 영구 개조로 늘리는 시작 크레딧을 여기서 한 번만 얹는다.
        credits = Mathf.Max(0, startingCredits) +
            Mathf.Max(0, MetaProgressRuntime.Bonuses.StartingCredits);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        credits += amount;
        OnCreditsChanged?.Invoke();
    }

    public bool CanAfford(int amount) => amount <= credits;

    /// <summary>낼 수 있으면 지불하고 true를 돌려준다. 모자라면 아무것도 하지 않는다.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > credits) return false;
        if (amount == 0) return true;

        credits -= amount;
        OnCreditsChanged?.Invoke();
        return true;
    }
}
