using UnityEngine;

public class TeleportTargetRangeVisual : MonoBehaviour
{
    [SerializeField] private LineRenderer ring;
    [SerializeField, Range(16, 128)] private int segments = 64;
    private float radius;

    public void Initialize(float newRadius)
    {
        radius = newRadius;
        segments = Mathf.Clamp(segments, 16, 128);
        if (ring == null)
        {
            Debug.LogError("TargetRange 프리팹의 Render에 LineRenderer가 필요합니다.", this);
            return;
        }
        ring.positionCount = segments;
    }

    public void SetCenter(Vector2 center)
    {
        if (ring == null) return;
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            ring.SetPosition(index, center +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
