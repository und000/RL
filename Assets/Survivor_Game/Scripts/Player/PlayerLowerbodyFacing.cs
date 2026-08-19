using UnityEngine;

public enum LowerbodyFacingSource
{
    MovementDirection,
    MouseQuadrant
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(-90)]
[AddComponentMenu("Player/Player Lowerbody Facing")]
public class PlayerLowerbodyFacing : MonoBehaviour
{
    [Header("Direction Source")]
    [SerializeField] private LowerbodyFacingSource facingSource =
        LowerbodyFacingSource.MovementDirection;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private UpperbodyCursorFacing upperbodyFacing;
    [SerializeField, Min(0f)] private float movementThreshold = 0.01f;

    [Header("Lowerbody Visual")]
    [Tooltip("Optional until a lowerbody Animator Controller is ready.")]
    [SerializeField] private Animator lowerbodyAnimator;
    [Tooltip("The current root renderer can remain assigned until the character art is split.")]
    [SerializeField] private SpriteRenderer lowerbodyRenderer;
    [SerializeField] private bool flipHorizontally = true;

    [Header("Animator Parameters")]
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string facingXParameter = "FacingX";
    [SerializeField] private string facingYParameter = "FacingY";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string movingParameter = "IsMoving";

    [Header("Animation States")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string forwardState = "Forward";
    [SerializeField] private string backwardState = "Backward";
    [SerializeField] private string dashState = "Dash";
    [SerializeField] private string hitState = "Hit";
    [SerializeField, Min(0f)] private float hitStateDuration = 0.2f;

    private Vector2 facingDirection = Vector2.down;
    private bool hasMoveX;
    private bool hasMoveY;
    private bool hasFacingX;
    private bool hasFacingY;
    private bool hasSpeed;
    private bool hasMoving;
    private bool dashActive;
    private float hitStateUntil;
    private int currentStateHash;

    public LowerbodyFacingSource FacingSource => facingSource;
    public Vector2 FacingDirection => facingDirection;
    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();
        if (upperbodyFacing == null)
        {
            PlayerMovement owner = GetComponentInParent<PlayerMovement>();
            upperbodyFacing = owner != null
                ? owner.GetComponentInChildren<UpperbodyCursorFacing>(true)
                : null;
        }
        CacheAnimatorParameters();
    }

    private void LateUpdate()
    {
        if (movement == null) return;

        Vector2 move = movement.MoveInput;
        IsMoving = move.sqrMagnitude > movementThreshold * movementThreshold;
        Vector2 sourceDirection = GetSourceDirection(move);
        if (sourceDirection.sqrMagnitude > 0.0001f)
        {
            facingDirection = ToFourWayDirection(sourceDirection);
        }

        UpdateRendererFacing();
        UpdateAnimator(move);
        UpdateAnimationState(move);
    }

    public void SetFacingSource(LowerbodyFacingSource source)
    {
        facingSource = source;
    }

    public void BeginDash()
    {
        dashActive = true;
    }

    public void EndDash()
    {
        dashActive = false;
    }

    public void PlayHit()
    {
        hitStateUntil = Mathf.Max(hitStateUntil, Time.time + hitStateDuration);
    }

    private Vector2 GetSourceDirection(Vector2 move)
    {
        if (facingSource == LowerbodyFacingSource.MouseQuadrant &&
            upperbodyFacing != null && upperbodyFacing.HasAimDirection)
        {
            return upperbodyFacing.AimDirection;
        }

        return IsMoving ? move : movement.LastMoveDirection;
    }

    private static Vector2 ToFourWayDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }
        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }

    private void UpdateRendererFacing()
    {
        if (!flipHorizontally || lowerbodyRenderer == null ||
            Mathf.Approximately(facingDirection.x, 0f)) return;

        lowerbodyRenderer.flipX = facingDirection.x < 0f;
    }

    private void UpdateAnimator(Vector2 move)
    {
        if (lowerbodyAnimator == null) return;

        if (hasMoveX) lowerbodyAnimator.SetFloat(moveXParameter, move.x);
        if (hasMoveY) lowerbodyAnimator.SetFloat(moveYParameter, move.y);
        if (hasFacingX) lowerbodyAnimator.SetFloat(facingXParameter, facingDirection.x);
        if (hasFacingY) lowerbodyAnimator.SetFloat(facingYParameter, facingDirection.y);
        if (hasSpeed) lowerbodyAnimator.SetFloat(speedParameter, move.magnitude);
        if (hasMoving) lowerbodyAnimator.SetBool(movingParameter, IsMoving);
    }

    private void UpdateAnimationState(Vector2 move)
    {
        if (lowerbodyAnimator == null || lowerbodyAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        string stateName;
        if (Time.time < hitStateUntil)
        {
            stateName = hitState;
        }
        else if (dashActive)
        {
            stateName = dashState;
        }
        else if (!IsMoving)
        {
            stateName = idleState;
        }
        else
        {
            float facingDot = Vector2.Dot(move.normalized, facingDirection);
            stateName = facingDot >= 0f ? forwardState : backwardState;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (stateHash == currentStateHash || !lowerbodyAnimator.HasState(0, stateHash)) return;
        currentStateHash = stateHash;
        lowerbodyAnimator.Play(stateHash, 0, 0f);
    }

    private void CacheAnimatorParameters()
    {
        hasMoveX = HasParameter(moveXParameter, AnimatorControllerParameterType.Float);
        hasMoveY = HasParameter(moveYParameter, AnimatorControllerParameterType.Float);
        hasFacingX = HasParameter(facingXParameter, AnimatorControllerParameterType.Float);
        hasFacingY = HasParameter(facingYParameter, AnimatorControllerParameterType.Float);
        hasSpeed = HasParameter(speedParameter, AnimatorControllerParameterType.Float);
        hasMoving = HasParameter(movingParameter, AnimatorControllerParameterType.Bool);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (lowerbodyAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in lowerbodyAnimator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName) return true;
        }
        return false;
    }

    private void OnValidate()
    {
        movementThreshold = Mathf.Max(0f, movementThreshold);
        hitStateDuration = Mathf.Max(0f, hitStateDuration);
    }

    private void OnDisable()
    {
        dashActive = false;
        hitStateUntil = 0f;
        currentStateHash = 0;
    }
}
