using System;
using UnityEngine;

/// <summary>
/// 강공격 같은 소모 행동에 쓰이는 플레이어 자원.
/// 일정 시간 쓰지 않으면 스스로 차오른다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Player/Player Energy")]
public class PlayerEnergy : MonoBehaviour
{
    [Header("에너지")]
    [SerializeField, Min(1)] private int maxEnergy = 20;

    [Header("자동 회복")]
    [Tooltip("마지막으로 에너지를 소모한 뒤 이 시간이 지나야 다시 차오르기 시작한다.")]
    [SerializeField, Min(0f)] private float regenerationDelay = 1f;
    [Tooltip("회복이 시작된 뒤 초당 차오르는 양.")]
    [SerializeField, Min(0f)] private float regenerationPerSecond = 4f;

    /// <summary>소모나 회복으로 값이 바뀔 때마다 호출된다.</summary>
    public event Action OnEnergyChanged;

    private float currentEnergy;
    private float regenerationStartTime;
    private int bonusMaxEnergy;
    private float bonusRegeneration;

    /// <summary>인스펙터 기준값에 장비·코어 보드 보너스를 더한 실제 최대치.</summary>
    public int MaxEnergy => Mathf.Max(1, maxEnergy + bonusMaxEnergy);
    public float CurrentEnergy => currentEnergy;
    /// <summary>UI 표기용. 실제 판정은 CurrentEnergy를 쓴다.</summary>
    public int CurrentEnergyDisplay => Mathf.FloorToInt(currentEnergy);
    public bool IsRegenerating =>
        currentEnergy < MaxEnergy && Time.time >= regenerationStartTime;

    private float RegenerationPerSecond =>
        Mathf.Max(0f, regenerationPerSecond + bonusRegeneration);

    /// <summary>바깥에서 최대치·회복량을 더해 준다. 값은 덮어쓰기이므로 누적되지 않는다.</summary>
    public void SetBonuses(int maxEnergyBonus, float regenerationBonus)
    {
        int previousMax = MaxEnergy;
        bonusMaxEnergy = maxEnergyBonus;
        bonusRegeneration = regenerationBonus;

        // 최대치가 늘어난 만큼은 그대로 채워 주고, 줄었으면 넘치지 않게 깎는다.
        int delta = MaxEnergy - previousMax;
        if (delta > 0) currentEnergy += delta;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, MaxEnergy);
        OnEnergyChanged?.Invoke();
    }

    private void Awake()
    {
        currentEnergy = MaxEnergy;
    }

    private void Update()
    {
        if (currentEnergy >= MaxEnergy || Time.time < regenerationStartTime) return;

        currentEnergy = Mathf.Min(
            MaxEnergy,
            currentEnergy + RegenerationPerSecond * Time.deltaTime);
        OnEnergyChanged?.Invoke();
    }

    public bool HasEnergy(int amount) => currentEnergy >= amount;

    /// <summary>충분하면 소모하고 true, 모자라면 아무것도 하지 않고 false를 돌려준다.</summary>
    public bool TryConsume(int amount)
    {
        if (amount <= 0) return true;
        if (currentEnergy < amount) return false;

        currentEnergy -= amount;
        regenerationStartTime = Time.time + regenerationDelay;
        OnEnergyChanged?.Invoke();
        return true;
    }

    public void Restore(float amount)
    {
        if (amount <= 0f) return;
        currentEnergy = Mathf.Min(MaxEnergy, currentEnergy + amount);
        OnEnergyChanged?.Invoke();
    }

    public void RefillToMax()
    {
        currentEnergy = MaxEnergy;
        regenerationStartTime = 0f;
        OnEnergyChanged?.Invoke();
    }

    private void OnValidate()
    {
        maxEnergy = Mathf.Max(1, maxEnergy);
        regenerationDelay = Mathf.Max(0f, regenerationDelay);
        regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
        if (Application.isPlaying) currentEnergy = Mathf.Min(currentEnergy, MaxEnergy);
    }
}
