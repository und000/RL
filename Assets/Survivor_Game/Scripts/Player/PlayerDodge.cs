using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerHealth))]
[AddComponentMenu("Player/Player Dodge")]
public class PlayerDodge : MonoBehaviour
{
    [Header("입력 (Space)")]
    [Tooltip("이 시간보다 짧게 눌렀다 떼면 회피가 나가고, 이 시간을 넘겨 계속 누르고 있으면 " +
        "전력 질주(대쉬)만 켜지고 회피는 나가지 않는다.")]
    [SerializeField, Min(0.01f)] private float holdThreshold = 0.18f;

    [Header("회피 이동")]
    [SerializeField, Min(0.1f)] private float dodgeDistance = 4f;
    [SerializeField, Min(0.1f)] private float dodgeSpeed = 30f;
    [SerializeField, Min(0f)] private float dodgeCooldown = 0.2f;
    public bool IsDodgeMoving { get; private set; }

    [Header("충돌 보정")]
    [SerializeField, Min(0f)] private float collisionSkin = 0.03f;
    [SerializeField, Min(0f)] private float enemyCollisionRestoreDelay = 0.2f;

    private readonly List<RaycastHit2D> castResults = new List<RaycastHit2D>(8);
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private PlayerLowerbodyFacing lowerbodyFacing;
    private ContactFilter2D obstacleFilter;
    private ContactFilter2D enemyFilter;
    private int playerLayer;
    private int enemyLayer;
    private bool isDodging;
    private bool enemyCollisionIgnored;
    private float nextDodgeTime;
    private float spacePressedTime;
    private bool spaceHeld;
    private bool holdBecameSprint;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        lowerbodyFacing = GetComponentInChildren<PlayerLowerbodyFacing>(true);

        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        int obstacleLayer = LayerMask.NameToLayer("WorldObstacle");

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(1 << obstacleLayer);
        obstacleFilter.useTriggers = false;

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(1 << enemyLayer);
        enemyFilter.useTriggers = false;

        if (playerLayer < 0 || enemyLayer < 0 || obstacleLayer < 0)
        {
            Debug.LogError("Player, Enemy, WorldObstacle 레이어가 필요합니다.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || LevelUpUI.IsPopupOpen)
        {
            ResetSpaceInput();
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            spaceHeld = true;
            holdBecameSprint = false;
            spacePressedTime = Time.time;
        }

        // 임계 시간을 넘겨 계속 누르고 있으면 전력 질주로 확정한다.
        // 이 경우 떼도 회피는 나가지 않는다.
        if (spaceHeld && keyboard.spaceKey.isPressed &&
            !holdBecameSprint && Time.time - spacePressedTime >= holdThreshold)
        {
            holdBecameSprint = true;
            playerMovement.SetSprinting(true);
        }

        if (spaceHeld && keyboard.spaceKey.wasReleasedThisFrame)
        {
            spaceHeld = false;
            if (holdBecameSprint) playerMovement.SetSprinting(false);
            else TryStartDodge();
            holdBecameSprint = false;
        }
    }

    private void TryStartDodge()
    {
        if (isDodging || Time.time < nextDodgeTime) return;

        Vector2 direction = playerMovement.MoveInput.sqrMagnitude > 0f
            ? playerMovement.MoveInput.normalized
            : playerMovement.LastMoveDirection;
        StartCoroutine(Dodge(direction));
    }

    private void ResetSpaceInput()
    {
        spaceHeld = false;
        holdBecameSprint = false;
        if (playerMovement != null) playerMovement.SetSprinting(false);
    }

    private IEnumerator Dodge(Vector2 direction)
    {
        isDodging = true;
        IsDodgeMoving = true;
        playerMovement.SetSprinting(false);
        lowerbodyFacing?.BeginDash();
        nextDodgeTime = Time.time + dodgeCooldown;
        playerMovement.SetMovementLocked(true);
        playerHealth.SetDodgeInvulnerable(true);
        SetEnemyCollisionIgnored(true);

        float remainingDistance = dodgeDistance;
        while (remainingDistance > 0f)
        {
            float stepDistance = Mathf.Min(
                dodgeSpeed * Time.fixedDeltaTime,
                remainingDistance
            );
            float allowedDistance = GetAllowedDistance(direction, stepDistance);

            if (allowedDistance <= 0f)
            {
                break;
            }

            body.MovePosition(body.position + direction * allowedDistance);
            remainingDistance -= allowedDistance;

            if (allowedDistance + Mathf.Epsilon < stepDistance)
            {
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        IsDodgeMoving = false;
        playerHealth.SetDodgeInvulnerable(false);
        playerMovement.SetMovementLocked(false);
        yield return RestoreEnemyCollisionWhenSafe();
        isDodging = false;
        lowerbodyFacing?.EndDash();
    }

    private float GetAllowedDistance(Vector2 direction, float stepDistance)
    {
        castResults.Clear();
        int hitCount = playerCollider.Cast(
            direction,
            obstacleFilter,
            castResults,
            stepDistance + collisionSkin
        );

        if (hitCount == 0)
        {
            return stepDistance;
        }

        float nearestDistance = stepDistance;
        for (int index = 0; index < hitCount; index++)
        {
            nearestDistance = Mathf.Min(nearestDistance, castResults[index].distance);
        }

        return Mathf.Max(0f, nearestDistance - collisionSkin);
    }

    private IEnumerator RestoreEnemyCollisionWhenSafe()
    {
        float restoreDeadline = Time.time + enemyCollisionRestoreDelay;
        while (Time.time < restoreDeadline && IsOverlappingEnemy())
        {
            yield return new WaitForFixedUpdate();
        }

        SetEnemyCollisionIgnored(false);
    }

    private bool IsOverlappingEnemy()
    {
        overlapResults.Clear();
        return playerCollider.Overlap(enemyFilter, overlapResults) > 0;
    }

    private void SetEnemyCollisionIgnored(bool ignored)
    {
        if (playerLayer < 0 || enemyLayer < 0 || enemyCollisionIgnored == ignored)
        {
            return;
        }

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, ignored);
        enemyCollisionIgnored = ignored;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsDodgeMoving = false;
        ResetSpaceInput();
        if (playerMovement != null)
        {
            playerMovement.SetMovementLocked(false);
        }
        if (playerHealth != null)
        {
            playerHealth.SetDodgeInvulnerable(false);
        }

        SetEnemyCollisionIgnored(false);
        isDodging = false;
        lowerbodyFacing?.EndDash();
    }
}
