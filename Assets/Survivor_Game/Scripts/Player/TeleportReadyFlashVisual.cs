using System.Collections;
using UnityEngine;

public class TeleportReadyFlashVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer overlay;
    [SerializeField] private Color flashColor = new Color(0.1f, 0.55f, 1f, 0.5f);
    [SerializeField, Min(0.01f)] private float duration = 0.25f;

    public void Play(
        SpriteRenderer sourceRenderer)
    {
        StartCoroutine(PlayRoutine(sourceRenderer));
    }

    private IEnumerator PlayRoutine(
        SpriteRenderer sourceRenderer)
    {
        if (overlay == null || sourceRenderer == null)
        {
            Debug.LogError("ReadyFlash 프리팹의 Render에 SpriteRenderer가 필요합니다.", this);
            Destroy(gameObject);
            yield break;
        }
        overlay.sharedMaterial = sourceRenderer.sharedMaterial;
        overlay.sortingLayerID = sourceRenderer.sortingLayerID;
        overlay.sortingOrder = sourceRenderer.sortingOrder + 1;

        float elapsed = 0f;
        while (elapsed < duration && sourceRenderer != null)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.sprite = sourceRenderer.sprite;
            Color color = flashColor;
            color.a *= 1f - Mathf.Clamp01(elapsed / duration);
            overlay.color = color;
            yield return null;
        }
        Destroy(gameObject);
    }
}
