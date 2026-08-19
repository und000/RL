using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public sealed class EnemyProjectileEmissionScheduler : MonoBehaviour
{
    private struct Emission
    {
        public float executeAt;
        public GameObject prefab;
        public Vector3 position;
        public Vector2 direction;
        public int count;
        public float spread;
        public float angleOffset;
        public float speed;
        public int damage;
        public float lifetime;
        public bool alignToDirection;
    }

    private static EnemyProjectileEmissionScheduler instance;
    private readonly List<Emission> emissions = new List<Emission>(16);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void Schedule(
        float delay,
        GameObject prefab,
        Vector3 position,
        Vector2 direction,
        int count,
        float spread,
        float angleOffset,
        float speed,
        int damage,
        float lifetime,
        bool alignToDirection)
    {
        if (prefab == null) return;
        Emission emission = new Emission
        {
            executeAt = Time.time + Mathf.Max(0f, delay),
            prefab = prefab,
            position = position,
            direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized : Vector2.down,
            count = Mathf.Max(1, count),
            spread = Mathf.Clamp(spread, 0f, 360f),
            angleOffset = angleOffset,
            speed = Mathf.Max(0f, speed),
            damage = Mathf.Max(1, damage),
            lifetime = Mathf.Max(0.01f, lifetime),
            alignToDirection = alignToDirection
        };
        if (delay <= 0f)
        {
            Emit(emission);
            return;
        }

        GetOrCreate().emissions.Add(emission);
    }

    private void Update()
    {
        for (int index = emissions.Count - 1; index >= 0; index--)
        {
            Emission emission = emissions[index];
            if (Time.time < emission.executeAt) continue;
            emissions.RemoveAt(index);
            Emit(emission);
        }
    }

    private static void Emit(Emission emission)
    {
        for (int index = 0; index < emission.count; index++)
        {
            float spreadOffset = emission.count == 1
                ? 0f
                : Mathf.Lerp(-emission.spread * 0.5f, emission.spread * 0.5f,
                    (float)index / (emission.count - 1));
            Vector2 direction = Rotate(
                emission.direction,
                emission.angleOffset + spreadOffset);
            EnemyProjectile.Spawn(
                emission.prefab,
                emission.position,
                direction,
                emission.speed,
                emission.damage,
                emission.lifetime,
                emission.alignToDirection);
        }
    }

    private static EnemyProjectileEmissionScheduler GetOrCreate()
    {
        if (instance != null) return instance;
        GameObject schedulerObject = new GameObject("EnemyProjectileEmissionScheduler");
        instance = schedulerObject.AddComponent<EnemyProjectileEmissionScheduler>();
        return instance;
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        return new Vector2(
            direction.x * cosine - direction.y * sine,
            direction.x * sine + direction.y * cosine).normalized;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
