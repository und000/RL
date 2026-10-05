using UnityEngine;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

[CreateAssetMenu(fileName = "EnemyProfile_", menuName = "Survivor/Enemies/Enemy Profile")]
public class EnemyProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string enemyId = "Enemy1";
    [SerializeField] private string displayName = "Enemy";
    [SerializeField] private EnemyRank rank = EnemyRank.Normal;
    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(0)] private int defense;
    [Header("충격 / 붕괴 (적 전용)")]
    [Tooltip("충격 게이지에 이만큼 충격이 쌓이면 붕괴가 발생합니다.")]
    [SerializeField, InspectorName("충격 한계"), Min(1f)] private float staggerThreshold = 30f;
    [Tooltip("붕괴 지속 시간(게임 시간). 모든 적의 기본값은 4초이며 보스 프로필별로 조정할 수 있습니다.")]
    [SerializeField, InspectorName("붕괴 지속 시간"), Min(0.01f)] private float staggerDuration = 4f;
    [SerializeField, InspectorName("충격 감소 대기 시간"), Min(0f)] private float staggerDecayDelay = 2f;
    [SerializeField, InspectorName("초당 충격 감소량"), Min(0f)] private float staggerDecayPerSecond = 10f;
    [SerializeField, InspectorName("붕괴 재발동 보호 시간"), Min(0f)] private float staggerRecoveryImmunity = 0.5f;
    [Tooltip("일반 프리팹이 엘리트 방에서 생성될 때만 적용되는 충격 한계 배율.")]
    [SerializeField, InspectorName("엘리트 충격 한계 배율"), Min(1f)] private float eliteStaggerMultiplier = 2f;
    [Header("Reward")]
    [SerializeField, Min(0)] private int experience = 1;
    [SerializeField] private GameObject experienceGemPrefab;
    [Header("UI")]
    [SerializeField] private HealthBarType healthBarType = HealthBarType.Normal;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(0f)] private float healthBarVisibleDuration = 5f;
    [SerializeField] private EnemyMarkerType markerType = EnemyMarkerType.Normal;

    public string EnemyId => enemyId;
    public string DisplayName => displayName;
    public EnemyRank Rank => rank;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int Defense => Mathf.Max(0, defense);
    public float StaggerThreshold => Mathf.Max(1f, staggerThreshold);
    public float StaggerDuration => Mathf.Max(0.01f, staggerDuration);
    public float StaggerDecayDelay => Mathf.Max(0f, staggerDecayDelay);
    public float StaggerDecayPerSecond => Mathf.Max(0f, staggerDecayPerSecond);
    public float StaggerRecoveryImmunity => Mathf.Max(0f, staggerRecoveryImmunity);
    public float EliteStaggerMultiplier => Mathf.Max(1f, eliteStaggerMultiplier);
    public int Experience => Mathf.Max(0, experience);
    public GameObject ExperienceGemPrefab => experienceGemPrefab;
    public HealthBarType HealthBarType => healthBarType;
    public Vector3 HealthBarOffset => healthBarOffset;
    public float HealthBarVisibleDuration => Mathf.Max(0f, healthBarVisibleDuration);
    public EnemyMarkerType MarkerType => markerType;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        defense = Mathf.Max(0, defense);
        experience = Mathf.Max(0, experience);
        healthBarVisibleDuration = Mathf.Max(0f, healthBarVisibleDuration);
    }
}
