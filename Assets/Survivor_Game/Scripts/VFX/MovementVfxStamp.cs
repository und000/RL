using UnityEngine;

// Shared pooled snapshot/puff. Rendering components are authored on the prefab.
public sealed class MovementVfxStamp : MonoBehaviour, IPrefabPoolLifecycle
{
    [SerializeField] private SpriteRenderer image;
    private Color tint;
    private Vector3 initialScale;
    private Vector3 velocity;
    private float duration, elapsed, growth;
    private bool playing;

    public void Play(Sprite sprite, Vector3 scale, Color color, float lifetime,
        float expansion, Vector3 drift, int layer, int order, bool flipX, bool flipY)
    {
        if (image == null) { PrefabPool.Release(gameObject); return; }
        if (sprite != null) image.sprite = sprite;
        image.flipX = flipX;
        image.flipY = flipY;
        image.sortingLayerID = layer;
        image.sortingOrder = order;
        image.color = tint = color;
        transform.localScale = initialScale = scale;
        duration = Mathf.Max(.02f, lifetime);
        growth = expansion;
        velocity = drift;
        elapsed = 0;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position += velocity * Time.deltaTime;
        transform.localScale = initialScale * Mathf.Lerp(1, growth, t);
        image.color = new Color(tint.r, tint.g, tint.b, tint.a * (1-t));
        if (t >= 1) PrefabPool.Release(gameObject);
    }

    public void OnPrefabSpawned() { playing = false; }
    public void OnPrefabDespawned() { playing = false; }
}
