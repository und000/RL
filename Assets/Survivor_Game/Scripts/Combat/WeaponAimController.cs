using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[AddComponentMenu("Combat/Weapon Aim Controller")]
public class WeaponAimController : MonoBehaviour
{
    public enum AttackAimMode
    {
        LockOnAttackStart,
        FollowCursor
    }

    [Header("Aim Hierarchy")]
    [Tooltip("Rotate this object toward the cursor. Keep the Animator on a child SwingRoot.")]
    [SerializeField] private Transform aimRoot;
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private Camera worldCamera;

    [Header("Attack Aim")]
    [SerializeField] private AttackAimMode attackAimMode = AttackAimMode.LockOnAttackStart;
    [Tooltip("Maximum turning speed while Follow Cursor is selected. Degrees per second.")]
    [SerializeField, Min(0f)] private float maximumTrackingSpeed = 720f;
    [SerializeField] private float angleOffset;

    public AttackAimMode AimMode => attackAimMode;
    public float MaximumTrackingSpeed => maximumTrackingSpeed;
    public bool IsAttacking { get; private set; }
    public Vector2 AimDirection => aimRoot == null
        ? Vector2.right
        : (Vector2)aimRoot.right;
    private float lockedWorldAngle;

    private void Awake()
    {
        if (aimRoot == null) aimRoot = transform;
        if (aimOrigin == null) aimOrigin = aimRoot;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    public void BeginAttack()
    {
        IsAttacking = true;
        if (TryGetCursorAngle(out float targetAngle))
        {
            lockedWorldAngle = targetAngle;
            SetAimAngle(targetAngle, true);
        }
    }

    public void EndAttack()
    {
        IsAttacking = false;
    }

    public void CancelAttack()
    {
        IsAttacking = false;
    }

    private void LateUpdate()
    {
        if (!IsAttacking) return;
        if (attackAimMode == AttackAimMode.LockOnAttackStart)
        {
            SetAimAngle(lockedWorldAngle, true);
            return;
        }
        if (!TryGetCursorAngle(out float targetAngle)) return;
        SetAimAngle(targetAngle, false);
    }

    private bool TryGetCursorAngle(out float targetAngle)
    {
        targetAngle = 0f;
        Mouse mouse = Mouse.current;
        if (mouse == null) return false;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null || aimOrigin == null) return false;

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 direction = (Vector2)mouseWorld - (Vector2)aimOrigin.position;
        if (direction.sqrMagnitude <= 0.0001f) return false;

        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
        return true;
    }

    private void SetAimAngle(float targetAngle, bool snap)
    {
        if (aimRoot == null) return;
        float currentAngle = aimRoot.eulerAngles.z;
        float nextAngle = snap || maximumTrackingSpeed <= 0f
            ? targetAngle
            : Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                maximumTrackingSpeed * Time.deltaTime);
        aimRoot.rotation = Quaternion.Euler(0f, 0f, nextAngle);
    }

    private void OnDisable()
    {
        IsAttacking = false;
    }
}
