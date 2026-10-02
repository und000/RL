using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[AddComponentMenu("Player Movement")]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float runSpeed = 10f;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isSprinting;
    private bool movementLocked;
    private float bonusSpeedRate;

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
    /// 전력 질주(대쉬) 상태를 켜고 끈다. Space를 꾹 누르는 판정은 PlayerDodge가 소유하므로
    /// 여기서는 키를 직접 읽지 않는다.
    /// </summary>
    public void SetSprinting(bool value)
    {
        isSprinting = value;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
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

        float currentSpeed = (isSprinting ? runSpeed : walkSpeed) * (1f + bonusSpeedRate);
        body.linearVelocity = moveInput * currentSpeed;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void OnDisable()
    {
        movementLocked = false;
        isSprinting = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
