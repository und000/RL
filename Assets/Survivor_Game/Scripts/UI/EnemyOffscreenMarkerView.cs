using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class EnemyOffscreenMarkerView : MonoBehaviour
{
    [SerializeField] private RectTransform renderRoot;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 baseRenderScale = Vector3.one;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (renderRoot != null) baseRenderScale = renderRoot.localScale;
    }

    public void SetVisual(Vector2 anchoredPosition, float rotation, float scale, float alpha)
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        rectTransform.anchoredPosition = anchoredPosition;
        if (renderRoot != null)
        {
            renderRoot.localRotation = Quaternion.Euler(0f, 0f, rotation);
            renderRoot.localScale = baseRenderScale * scale;
        }
        canvasGroup.alpha = Mathf.Clamp01(alpha);
    }
}
