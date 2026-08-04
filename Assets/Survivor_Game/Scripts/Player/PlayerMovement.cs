using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[AddComponentMenu("Player Movement")]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float walkSpeed = 5f;

    [SerializeField]
    [Min(0f)]
    private float runSpeed = 10f;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private bool isSprinting; // 달리기 여부

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
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }
        
        moveInput = new Vector2(horizontal, vertical);
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        isSprinting = keyboard.shiftKey.isPressed; // 쉬프트 누르면 대시
    }

    private void FixedUpdate()
    {
        float currentSpeed = walkSpeed;
        if (isSprinting)
        {
            currentSpeed = runSpeed; // 속도를 대시 속도로 변경
        }

        body.linearVelocity = moveInput * currentSpeed;
    }

    private void OnDisable()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}