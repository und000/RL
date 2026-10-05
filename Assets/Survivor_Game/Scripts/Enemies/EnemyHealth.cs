using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IEnemyPoolLifecycle
{
    [Header("Enemy Data")]
    [SerializeField] private EnemyProfile profile;
    private int currentHealth;
    private bool dying;
    private EnemyScaling scaling = EnemyScaling.None;
    private EnemyStagger stagger;
    private EnemyRank? encounterRank;

    public EnemyProfile Profile => profile;
    public EnemyRank Rank => encounterRank ?? (profile != null ? profile.Rank : EnemyRank.Normal);
    public void SetEncounterRank(EnemyRank? rank) => encounterRank = rank;
    /// <summary>지금 이 적에게 걸려 있는 층 난이도 보정.</summary>
    public EnemyScaling Scaling => scaling;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDied;

    private void Awake()
    {
        stagger = GetComponent<EnemyStagger>();
        if (profile == null)
        {
            Debug.LogError("EnemyHealth에 Enemy Profile이 필요합니다.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        ResetHealth();
    }

    public void TakeDamage(DamageData damageData)
    {
        if (!isActiveAndEnabled || currentHealth <= 0)
        {
            return;
        }

        float remainingDefense = Mathf.Max(
            0f,
            GetDefense() - damageData.FlatArmorPenetration
        );
        float effectiveDefense = remainingDefense *
            (1f - damageData.ArmorPenetrationRate);
        int normalDamage = Mathf.Max(
            damageData.MinimumDamage,
            Mathf.RoundToInt(damageData.NormalDamage - effectiveDefense)
        );
        int trueDamage = Mathf.Max(0, Mathf.RoundToInt(damageData.TrueDamage));
        int finalDamage = normalDamage + trueDamage;
        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);
        if (finalDamage > 0)
        {
            OnDamaged?.Invoke();
        }
        if (currentHealth > 0 && stagger != null) stagger.ApplyImpact(damageData.StaggerImpact);
        OnHealthChanged?.Invoke(currentHealth, GetMaxHealth());
        if (currentHealth == 0)
        {
            Die();
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// 층 난이도 보정을 건다. 스폰 직전에 불러야 하며, 체력을 새 최대치로 되돌린다.
    /// </summary>
    public void ApplyScaling(EnemyScaling enemyScaling)
    {
        scaling = enemyScaling;
        ResetHealth();
    }

    public int GetBaseMaxHealth() => profile != null ? profile.MaxHealth : 1;

    public int GetMaxHealth()
    {
        return Mathf.Max(1,
            Mathf.RoundToInt(GetBaseMaxHealth() * scaling.HealthMultiplier));
    }

    public int GetDefense()
    {
        int baseDefense = profile != null ? profile.Defense : 0;
        return baseDefense + scaling.DefenseBonus;
    }

    public void RegisterZeroDamageHit()
    {
        if (currentHealth > 0)
        {
            OnDamaged?.Invoke();
        }
    }

    private void Die()
    {
        if (dying) return;
        dying = true;
        OnDied?.Invoke();

        if (TryGetComponent(out EnemyLifecycleVisual lifecycleVisual) &&
            lifecycleVisual.TryPlayDeath(CompleteDeath))
        {
            return;
        }

        CompleteDeath();
    }

    private void CompleteDeath()
    {
        GameObject gemPrefab = profile != null
            ? profile.ExperienceGemPrefab : null;
        int baseExperience = profile != null ? profile.Experience : 0;
        int experience = Mathf.Max(0,
            Mathf.RoundToInt(baseExperience * scaling.ExperienceMultiplier));
        if (gemPrefab != null)
        {
            GameObject gemObject = Instantiate(
                gemPrefab,
                transform.position,
                Quaternion.identity
            );

            if (gemObject.TryGetComponent(out ExperienceGem gem))
            {
                gem.Initialize(experience);
            }
        }
        else
        {
            Debug.LogWarning($"{name}의 Experience Gem Prefab이 연결되지 않았습니다.");
        }

        if (TryGetComponent(out PooledEnemy pooledEnemy) && pooledEnemy.IsConfigured)
        {
            pooledEnemy.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnEnemySpawned()
    {
        ResetHealth();
    }

    public void OnEnemyDespawned()
    {
        dying = false;
        // 풀에 돌아간 적이 다음 층에서 이전 층 보정을 들고 나오지 않게 되돌린다.
        scaling = EnemyScaling.None;
        encounterRank = null;
    }

    private void ResetHealth()
    {
        currentHealth = GetMaxHealth();
        dying = false;
    }
}
