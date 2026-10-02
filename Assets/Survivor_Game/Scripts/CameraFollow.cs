using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Camera/Player Camera Follow")]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.16f;
    [Tooltip("일반 추적 반응 속도 비율입니다. 0.9는 기존보다 10% 느리게 따라갑니다.")]
    [SerializeField, Range(0.1f, 1f)] private float normalFollowSpeedRatio = 0.9f;
    [Header("Normal Follow Curve")]
    [Tooltip("커브 X=카메라와 플레이어 거리 / 기준 거리(0~1), Y=추적 반응 속도 배율. 1은 기존 속도, 작으면 부드럽고 느리게, 크면 빠르게 따라갑니다. 포인터 추적에는 적용하지 않습니다.")]
    [SerializeField] private AnimationCurve followSpeedCurve = AnimationCurve.EaseInOut(0f, 0.65f, 1f, 1.5f);
    [Tooltip("이 거리 이상에서는 커브의 X=1 값을 사용합니다.")]
    [SerializeField, Min(0.01f)] private float followCurveDistance = 3f;
    [SerializeField, Min(0.01f)] private float pointerFocusSmoothTime = 0.06f;
    [SerializeField, Min(0f)] private float pointerSnapDistance = 0.15f;

    private Vector3 velocity;
    private Vector2 pointerFocus;
    private bool usePointerFocus;
    private bool pointerFocusSnapped;

    public bool HasReachedPointerFocus => usePointerFocus && pointerFocusSnapped;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            target = player != null ? player.transform : null;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Vector2 focus = usePointerFocus ? pointerFocus : (Vector2)target.position;
        Vector3 destination = new Vector3(focus.x, focus.y, transform.position.z);
        if (usePointerFocus && pointerFocusSnapped)
        {
            transform.position = destination;
            velocity = Vector3.zero;
            return;
        }

        float smoothTime = usePointerFocus ? pointerFocusSmoothTime : followSmoothTime;
        if (!usePointerFocus)
        {
            float distanceRatio = Mathf.Clamp01(
                Vector2.Distance(transform.position, destination) / Mathf.Max(0.01f, followCurveDistance));
            float response = followSpeedCurve == null || followSpeedCurve.length == 0
                ? 1f : followSpeedCurve.Evaluate(distanceRatio);
            // Keep convergence even if a curve key is zero or negative.
            smoothTime /= Mathf.Max(0.01f, response);
        }
        float followDeltaTime = usePointerFocus
            ? Time.unscaledDeltaTime
            : Time.unscaledDeltaTime * normalFollowSpeedRatio;
        transform.position = Vector3.SmoothDamp(
            transform.position, destination, ref velocity, smoothTime,
            Mathf.Infinity, followDeltaTime);

        if (usePointerFocus &&
            Vector2.Distance(transform.position, destination) <= pointerSnapDistance)
        {
            transform.position = destination;
            velocity = Vector3.zero;
            pointerFocusSnapped = true;
        }
    }

    public void SetPointerFocus(Vector2 worldPosition, bool enabled)
    {
        bool enteringPointerFocus = enabled && !usePointerFocus;
        pointerFocus = worldPosition;
        usePointerFocus = enabled;
        if (enteringPointerFocus)
        {
            velocity = Vector3.zero;
            pointerFocusSnapped = false;
        }
        else if (!enabled)
        {
            pointerFocusSnapped = false;
        }
    }

    private void OnValidate()
    {
        followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
        normalFollowSpeedRatio = Mathf.Clamp(normalFollowSpeedRatio, 0.1f, 1f);
        followCurveDistance = Mathf.Max(0.01f, followCurveDistance);
        pointerFocusSmoothTime = Mathf.Max(0.01f, pointerFocusSmoothTime);
        pointerSnapDistance = Mathf.Max(0f, pointerSnapDistance);
    }
}
