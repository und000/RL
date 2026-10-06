using System.Collections.Generic;
using UnityEngine;

/// <summary>방향 안내선. 폭은 시각 설정이며 피해 판정은 기존 투사체/접촉 충돌이 담당한다.</summary>
public class EnemyAttackTelegraph : MonoBehaviour
{
    [SerializeField] private LineRenderer lineTemplate;
    [SerializeField] private Color readyColor = new Color(1f, .25f, .12f, .3f);
    [SerializeField] private Color fireColor = new Color(1f, .65f, .15f, .85f);
    private readonly List<LineRenderer> lines = new List<LineRenderer>();

    private void Awake()
    {
        if (lineTemplate != null) { lines.Add(lineTemplate); lineTemplate.enabled = false; }
    }

    public void Show(Vector3 origin, Vector2 direction, int count, float spread,
        float offset, float length, float progress)
    {
        if (lineTemplate == null) return;
        count = Mathf.Max(1, count);
        while (lines.Count < count) lines.Add(Instantiate(lineTemplate, transform));
        Color color = Color.Lerp(readyColor, fireColor, Mathf.Clamp01(progress));
        for (int i = 0; i < lines.Count; i++)
        {
            LineRenderer line = lines[i];
            line.enabled = i < count;
            if (i >= count) continue;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startColor = line.endColor = color;
            Vector2 ray = EnemyAttackGeometry.VolleyDirection(direction, i, count, spread, offset);
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + (Vector3)(ray * Mathf.Max(0f, length)));
        }
    }

    public void Hide()
    {
        foreach (LineRenderer line in lines) if (line != null) line.enabled = false;
    }

    private void OnDisable() => Hide();
}

public static class EnemyAttackGeometry
{
    // 360도 산탄은 양 끝을 중복하지 않는다. 예고와 발사가 반드시 같은 계산을 쓴다.
    public static Vector2 VolleyDirection(Vector2 direction, int index, int count, float spread, float offset)
    {
        count = Mathf.Max(1, count);
        float step = count == 1 ? 0f : spread / (spread >= 360f ? count : count - 1);
        float angle = (offset + (count == 1 ? 0f : -spread * .5f + index * step)) * Mathf.Deg2Rad;
        direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.down;
        float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
        return new Vector2(direction.x * cosine - direction.y * sine,
            direction.x * sine + direction.y * cosine).normalized;
    }
}
