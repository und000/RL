using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
[AddComponentMenu("UI/Enemy Offscreen Marker Manager")]
public class EnemyOffscreenMarkerManager : MonoBehaviour
{
    [Header("마커 프리팹")]
    [SerializeField] private EnemyOffscreenMarkerView normalMarkerPrefab;
    [SerializeField] private EnemyOffscreenMarkerView bossMarkerPrefab;

    [Header("화면 배치")]
    [SerializeField] private Camera worldCamera;
    [SerializeField, Min(0f)] private float edgePadding = 80f;

    [Header("거리별 표시")]
    [SerializeField, Min(0f)] private float nearestDistance = 8f;
    [SerializeField, Min(0.1f)] private float farthestDistance = 32f;
    [SerializeField, Min(0f)] private float nearScale = 1f;
    [SerializeField, Min(0f)] private float farScale = 0.55f;
    [SerializeField, Range(0f, 1f)] private float nearAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float farAlpha = 0.2f;

    private readonly Dictionary<EnemyOffscreenMarkerTarget, EnemyOffscreenMarkerView> views =
        new Dictionary<EnemyOffscreenMarkerTarget, EnemyOffscreenMarkerView>();
    private readonly Stack<EnemyOffscreenMarkerView> normalPool =
        new Stack<EnemyOffscreenMarkerView>();
    private readonly Stack<EnemyOffscreenMarkerView> bossPool =
        new Stack<EnemyOffscreenMarkerView>();
    private readonly List<EnemyOffscreenMarkerTarget> releaseBuffer =
        new List<EnemyOffscreenMarkerTarget>();
    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect = (RectTransform)transform;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
            if (worldCamera == null) return;
        }

        releaseBuffer.Clear();
        foreach (KeyValuePair<EnemyOffscreenMarkerTarget, EnemyOffscreenMarkerView> pair in views)
        {
            if (pair.Key == null || !pair.Key.isActiveAndEnabled)
            {
                releaseBuffer.Add(pair.Key);
            }
        }
        foreach (EnemyOffscreenMarkerTarget target in releaseBuffer) Release(target);

        foreach (EnemyOffscreenMarkerTarget target in EnemyOffscreenMarkerTarget.ActiveTargets)
        {
            if (target == null) continue;
            Vector3 viewport = worldCamera.WorldToViewportPoint(target.transform.position);
            bool inside = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f;
            float distance = Vector2.Distance(worldCamera.transform.position,
                target.transform.position);
            if (inside || distance > farthestDistance)
            {
                Release(target);
                continue;
            }

            EnemyOffscreenMarkerView view = Acquire(target);
            UpdateView(view, target.transform.position, distance);
        }
    }

    private void UpdateView(
        EnemyOffscreenMarkerView view,
        Vector2 worldPosition,
        float distance)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 targetScreen = worldCamera.WorldToScreenPoint(worldPosition);
        Vector2 direction = targetScreen - screenCenter;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;

        float halfWidth = Mathf.Max(1f, screenCenter.x - edgePadding);
        float halfHeight = Mathf.Max(1f, screenCenter.y - edgePadding);
        float horizontalScale = Mathf.Abs(direction.x) > 0.001f
            ? halfWidth / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float verticalScale = Mathf.Abs(direction.y) > 0.001f
            ? halfHeight / Mathf.Abs(direction.y) : float.PositiveInfinity;
        Vector2 edgeScreenPosition = screenCenter + direction *
            Mathf.Min(horizontalScale, verticalScale);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, edgeScreenPosition, null, out Vector2 anchoredPosition);
        Vector2 worldDirection = worldPosition - (Vector2)worldCamera.transform.position;
        float rotation = Vector2.SignedAngle(Vector2.down, worldDirection);
        float distanceRatio = Mathf.InverseLerp(
            nearestDistance, Mathf.Max(nearestDistance + 0.01f, farthestDistance), distance);
        float scale = Mathf.Lerp(nearScale, farScale, distanceRatio);
        float alpha = Mathf.Lerp(nearAlpha, farAlpha, distanceRatio);
        view.SetVisual(anchoredPosition, rotation, scale, alpha);
    }

    private EnemyOffscreenMarkerView Acquire(EnemyOffscreenMarkerTarget target)
    {
        if (views.TryGetValue(target, out EnemyOffscreenMarkerView existing)) return existing;
        bool boss = target.MarkerType == EnemyMarkerType.Boss;
        Stack<EnemyOffscreenMarkerView> pool = boss ? bossPool : normalPool;
        EnemyOffscreenMarkerView prefab = boss ? bossMarkerPrefab : normalMarkerPrefab;
        EnemyOffscreenMarkerView view = pool.Count > 0 ? pool.Pop() : Instantiate(prefab, transform);
        view.gameObject.SetActive(true);
        views.Add(target, view);
        return view;
    }

    private void Release(EnemyOffscreenMarkerTarget target)
    {
        if (ReferenceEquals(target, null) ||
            !views.TryGetValue(target, out EnemyOffscreenMarkerView view)) return;
        views.Remove(target);
        view.gameObject.SetActive(false);
        if (target.MarkerType == EnemyMarkerType.Boss) bossPool.Push(view);
        else normalPool.Push(view);
    }
}
