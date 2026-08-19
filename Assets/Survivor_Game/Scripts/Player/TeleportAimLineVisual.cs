using UnityEngine;

public class TeleportAimLineVisual : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField, Min(0.01f)] private float tilingPerUnit = 0.5f;
    [SerializeField] private float uvScrollSpeed = 1.5f;
    private Material materialInstance;

    private void Awake()
    {
        if (line == null)
        {
            Debug.LogError("AimLine 프리팹의 Render에 LineRenderer가 필요합니다.", this);
            return;
        }
        materialInstance = line.material;
    }

    public void SetPoints(Vector2 start, Vector2 end)
    {
        if (line == null) return;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        if (materialInstance == null) return;
        float length = Vector2.Distance(start, end);
        materialInstance.mainTextureScale = new Vector2(
            Mathf.Max(0.01f, length * tilingPerUnit), 1f);
        materialInstance.mainTextureOffset = new Vector2(
            Time.unscaledTime * uvScrollSpeed, 0f);
    }

    private void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }
}
