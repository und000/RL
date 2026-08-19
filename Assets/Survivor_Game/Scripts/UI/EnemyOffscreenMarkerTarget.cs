using System.Collections.Generic;
using UnityEngine;

public enum EnemyMarkerType
{
    Normal,
    Boss
}

[DisallowMultipleComponent]
[AddComponentMenu("UI/Enemy Offscreen Marker Target")]
public class EnemyOffscreenMarkerTarget : MonoBehaviour
{
    private static readonly List<EnemyOffscreenMarkerTarget> activeTargets =
        new List<EnemyOffscreenMarkerTarget>();

    [SerializeField] private EnemyMarkerType markerType = EnemyMarkerType.Normal;
    private EnemyHealth enemyHealth;
    public static IReadOnlyList<EnemyOffscreenMarkerTarget> ActiveTargets => activeTargets;
    public EnemyMarkerType MarkerType => enemyHealth != null && enemyHealth.Profile != null
        ? enemyHealth.Profile.MarkerType : markerType;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (!activeTargets.Contains(this)) activeTargets.Add(this);
    }

    private void OnDisable()
    {
        activeTargets.Remove(this);
    }
}
