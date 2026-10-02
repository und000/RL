using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[AddComponentMenu("Player/Upperbody Cursor Facing")]
public class UpperbodyCursorFacing : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float angleOffset;
    [SerializeField, Min(0f)] private float maximumTurnSpeed;
    [Tooltip("상체 전체를 커서 쪽으로 꺾는다. 팔만 움직이는 리그를 쓸 때는 끈다.")]
    [SerializeField] private bool applyRotation;

    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public bool HasAimDirection { get; private set; }

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.0001f) return;
        AimDirection = direction.normalized;
        HasAimDirection = true;
        if (!applyRotation) return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
        float nextAngle = maximumTurnSpeed <= 0f
            ? targetAngle
            : Mathf.MoveTowardsAngle(
                transform.eulerAngles.z,
                targetAngle,
                maximumTurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, nextAngle);
    }
}
