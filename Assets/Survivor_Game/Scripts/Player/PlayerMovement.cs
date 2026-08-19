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

    public Vector2 MoveInput => moveInput;
    public Vector2 LastMoveDirection => lastMoveDirection;

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

        isSprinting = keyboard.shiftKey.isPressed;
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            return;
        }

        float currentSpeed = isSprinting ? runSpeed : walkSpeed;
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
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
