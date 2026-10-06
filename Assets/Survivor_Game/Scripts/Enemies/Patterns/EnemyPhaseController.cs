using System;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
[AddComponentMenu("Enemies/Patterns/Enemy Phase Controller")]
public class EnemyPhaseController : MonoBehaviour, IEnemyPoolLifecycle
{
    [Tooltip("1페이즈 이후의 시작 체력 비율을 높은 값부터 입력하세요.")]
    [SerializeField] private float[] nextPhaseHealthRatios = { 0.7f, 0.4f };

    private EnemyHealth enemyHealth;
    public int CurrentPhase { get; private set; } = 1;
    public event Action<int> OnPhaseChanged;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        ResetPhase();
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    public void OnEnemySpawned()
    {
        ResetPhase();
    }

    public void OnEnemyDespawned()
    {
        ResetPhase();
    }

    private void ResetPhase()
    {
        CurrentPhase = 1;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0) return;
        float healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        int newPhase = 1;

        if (nextPhaseHealthRatios != null)
        {
            foreach (float threshold in nextPhaseHealthRatios)
            {
                if (healthRatio <= Mathf.Clamp01(threshold))
                {
                    newPhase++;
                }
            }
        }

        if (newPhase <= CurrentPhase)
        {
            return;
        }

        CurrentPhase = newPhase;
        OnPhaseChanged?.Invoke(CurrentPhase);
    }
}
