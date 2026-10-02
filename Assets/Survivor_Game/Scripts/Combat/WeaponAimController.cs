using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
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
    [Header("Idle Render Flip")]
    [SerializeField] private Transform idleRender;
    [SerializeField] private Vector2 flipAngleRange = new Vector2(-120f, 20f);

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
    private float renderYSize;

    private void Awake()
    {
        if (aimRoot == null) aimRoot = transform;
        if (aimOrigin == null) aimOrigin = aimRoot;
        if (worldCamera == null) worldCamera = Camera.main;
        if (idleRender == null) idleRender = aimRoot.Find("SwingRoot/Render");
        if (idleRender != null) renderYSize = Mathf.Abs(idleRender.localScale.y);
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
        if (!IsAttacking)
        {
            if (TryGetCursorAngle(out float idleAngle)) SetAimAngle(idleAngle, true);
            if (idleRender != null)
            {
                float angle = Mathf.DeltaAngle(0f, aimRoot.eulerAngles.z);
                Vector3 scale = idleRender.localScale;
                scale.y = angle >= flipAngleRange.x - .001f && angle <= flipAngleRange.y + .001f
                    ? -renderYSize : renderYSize;
                idleRender.localScale = scale;
            }
            return;
        }
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
