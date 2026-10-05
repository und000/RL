using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[AddComponentMenu("Player Movement")]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float walkSpeed = 6f;
    [SerializeField, Min(0f)] private float runSpeed = 12f;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isSprinting;
    private bool movementLocked;
    private float bonusSpeedRate;
    private Vector2 attackDirection;
    private float attackRemainingTime, attackSpeed;
    private readonly RaycastHit2D[] attackCastHits = new RaycastHit2D[32];
    private Collider2D movementCollider;
    public bool CanAttack => isActiveAndEnabled && !movementLocked;

    public void BeginAttackMovement(Vector2 direction, float distance, float duration)
    {
        StopAttackMovement();
        if (!CanAttack || Mathf.Approximately(distance, 0f) || direction.sqrMagnitude < 0.001f) return;
        attackDirection = direction.normalized * Mathf.Sign(distance);
        attackRemainingTime = Mathf.Max(0.01f, duration);
        attackSpeed = Mathf.Abs(distance) / attackRemainingTime;
    }

    public void StopAttackMovement()
    {
        if (attackRemainingTime > 0f && body != null) body.linearVelocity = Vector2.zero;
        attackRemainingTime = 0f;
    }

    public Vector2 MoveInput => moveInput;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public bool IsSprinting => isSprinting;

    /// <summary>
    /// 코어 보드 같은 바깥 장비가 이동 속도를 비율로 올려 준다. 0.05면 +5%.
    /// 값은 덮어쓰기이므로 누적되지 않는다.
    /// </summary>
    public void SetBonusSpeedRate(float rate)
    {
        bonusSpeedRate = Mathf.Max(-0.9f, rate);
    }

    /// <summary>
    /// 전력 질주 상태를 켜고 끈다. 회피·달리기 입력 판정은 PlayerDodge가 소유하므로
    /// 여기서는 키를 직접 읽지 않는다.
    /// </summary>
    public void SetSprinting(bool value)
    {
        isSprinting = value;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movementCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        RefreshMoveInput();
    }

    /// <summary>회피와 방향키를 같은 프레임에 눌러도 Update 순서에 관계없이 새 방향을 사용한다.</summary>
    public void RefreshMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || GameInputKeys.IsGameplayBlocked)
        {
            moveInput = Vector2.zero;
            isSprinting = false;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal--;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal++;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical--;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical++;
        }

        moveInput = Vector2.ClampMagnitude(
            new Vector2(horizontal, vertical),
            1f
        );

        if (moveInput.sqrMagnitude > 0f)
        {
            lastMoveDirection = moveInput.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            return;
        }

        if (attackRemainingTime > 0f)
        {
            float dt = Mathf.Min(Time.fixedDeltaTime, attackRemainingTime);
            float distance = attackSpeed * dt;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("WorldObstacle"));
            filter.useTriggers = false;
            if (movementCollider != null)
            {
                int count = movementCollider.Cast(attackDirection, filter, attackCastHits, distance + 0.02f);
                for (int i = 0; i < count; i++)
                    distance = Mathf.Min(distance, Mathf.Max(0f, attackCastHits[i].distance - 0.02f));
            }
            body.linearVelocity = Vector2.zero;
            body.MovePosition(body.position + attackDirection * distance);
            attackRemainingTime = Mathf.Max(0f, attackRemainingTime - dt);
            return;
        }
        float currentSpeed = (isSprinting ? runSpeed : walkSpeed) * (1f + bonusSpeedRate);
        body.linearVelocity = moveInput * currentSpeed;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked) StopAttackMovement();
        if (locked && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void OnDisable()
    {
        StopAttackMovement();
        movementLocked = false;
        isSprinting = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
