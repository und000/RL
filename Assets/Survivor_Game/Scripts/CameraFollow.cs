using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Camera/Player Camera Follow")]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;
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
        transform.position = Vector3.SmoothDamp(
            transform.position, destination, ref velocity, smoothTime,
            Mathf.Infinity, Time.unscaledDeltaTime);

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
}
