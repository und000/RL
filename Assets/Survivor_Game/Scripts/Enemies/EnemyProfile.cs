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
