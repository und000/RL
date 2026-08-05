using UnityEngine;

public class BossRangedAttack : MonoBehaviour
{
    [Header("발사 주기")]
    [SerializeField, Min(0.1f)] private float attackInterval = 2f;
    [SerializeField, Min(0f)] private float firstAttackDelay = 1f;

    [Header("탄환")]
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Color projectileColor = new Color(0.75f, 0.2f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float projectileScale = 0.35f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 5f;
    [SerializeField, Min(1)] private int projectileDamage = 1;
    [SerializeField, Min(0.1f)] private float projectileLifeTime = 6f;

    private Transform target;
    private float nextAttackTime;

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }

        nextAttackTime = Time.time + firstAttackDelay;
    }

    private void Update()
    {
        if (target == null || Time.time < nextAttackTime)
        {
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        FireProjectile(direction);
        nextAttackTime = Time.time + attackInterval;
    }

    public void FireProjectile(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return;
        }

        GameObject projectileObject = new GameObject("BossProjectile");
        projectileObject.transform.SetPositionAndRotation(
            transform.position,
            Quaternion.identity
        );
        projectileObject.transform.localScale = Vector3.one * projectileScale;

        SpriteRenderer projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
        projectileRenderer.sprite = projectileSprite;
        projectileRenderer.color = projectileColor;
        projectileRenderer.sortingOrder = 5;

        Rigidbody2D projectileBody = projectileObject.AddComponent<Rigidbody2D>();
        projectileBody.gravityScale = 0f;
        projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        projectileBody.freezeRotation = true;

        CircleCollider2D projectileCollider = projectileObject.AddComponent<CircleCollider2D>();
        projectileCollider.isTrigger = true;

        BossProjectile projectile = projectileObject.AddComponent<BossProjectile>();
        projectile.Initialize(
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime
        );
    }
}
