using System.Collections;
using UnityEngine;

/// <summary>스테이지 생성 동안 화면과 게임 시간을 가린다. 실제 시간으로 페이드한다.</summary>
[DisallowMultipleComponent]
public class StageTransitionUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup curtain;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    private static StageTransitionUI active;
    private static int blockedThroughFrame = -1;
    private bool ownsPause;
    private float previousTimeScale;
    public bool IsConfigured => curtain != null;
    public static bool IsBlockingGameplay => active != null || Time.frameCount <= blockedThroughFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { active = null; blockedThroughFrame = -1; }

    private void Awake()
    {
        if (curtain == null) return;
        curtain.alpha = 0f;
        curtain.blocksRaycasts = false;
        curtain.interactable = false;
    }

    public bool Begin()
    {
        if (!isActiveAndEnabled || !IsConfigured || active != null || Time.timeScale <= 0f ||
            LevelUpUI.IsPopupOpen || RoomChoiceUI.IsBlockingGameplay) return false;
        StaggerImpactFeedback.CancelActive();
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        active = this;
        curtain.blocksRaycasts = true;
        Time.timeScale = 0f;
        return true;
    }

    public IEnumerator FadeOut() => Fade(1f, fadeOutDuration);
    public IEnumerator FadeIn() => Fade(0f, fadeInDuration);

    private IEnumerator Fade(float target, float duration)
    {
        float initial = curtain.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            curtain.alpha = Mathf.Lerp(initial, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        curtain.alpha = target;
    }

    public void End(bool resume)
    {
        if (curtain != null) { curtain.alpha = 0f; curtain.blocksRaycasts = false; }
        if (!ownsPause) return;
        if (resume && Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = previousTimeScale;
        ownsPause = false;
        if (active == this) active = null;
        blockedThroughFrame = Time.frameCount;
    }

    private void OnDisable() { End(true); }
}
