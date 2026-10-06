using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerHealth))]
[AddComponentMenu("Player/Player Dodge")]
public class PlayerDodge : MonoBehaviour
{
    public event System.Action OnDodgeStarted;
    [Header("즉시 회피 입력")]
    [Tooltip("회피는 항상 Space를 누른 순간 한 번 발동합니다. 켜면 Shift로 달리고, 끄면 Space를 계속 눌러 달립니다.")]
    [SerializeField] private bool separateSprintInput;
    [Header("입력 (Space)")]
    [Tooltip("Space 유지 달리기의 시작 시간입니다. 최초 회피는 이 시간을 기다리지 않고 즉시 발동합니다.")]
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
        if (keyboard == null || GameInputKeys.IsGameplayBlocked)
        {
            ResetSpaceInput();
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            spaceHeld = true;
            spacePressedTime = Time.time;
            // 유지/키 해제로는 다시 요청하지 않는다. 쿨다운 중 누른 입력도 예약하지 않는다.
            TryStartDodge();
        }

        if (!keyboard.spaceKey.isPressed) spaceHeld = false;
        bool wantsSprint = separateSprintInput ? keyboard.leftShiftKey.isPressed :
            spaceHeld && keyboard.spaceKey.isPressed && Time.time - spacePressedTime >= holdThreshold;
        playerMovement.SetSprinting(!isDodging && wantsSprint);
    }

    private void TryStartDodge()
    {
        if (isDodging || Time.time < nextDodgeTime) return;
        playerMovement.RefreshMoveInput();
        Vector2 direction = playerMovement.MoveInput.sqrMagnitude > 0f
            ? playerMovement.MoveInput.normalized
            : playerMovement.LastMoveDirection;
        StartCoroutine(Dodge(direction));
    }

    private void ResetSpaceInput()
    {
        spaceHeld = false;
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
        OnDodgeStarted?.Invoke();

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

    public void CancelForStageTransition()
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

    private void OnDisable() => CancelForStageTransition();
}
