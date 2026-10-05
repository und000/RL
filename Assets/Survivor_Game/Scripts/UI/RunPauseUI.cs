using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>진행 중인 런의 일시정지. 다른 선택창과 시간 소유권을 겹치지 않는다.</summary>
[DefaultExecutionOrder(-200)]
public class RunPauseUI : MonoBehaviour
{
    private static RunPauseUI active;
    private static int blockedThroughFrame = -1;
    public static bool IsBlockingGameplay => active != null || Time.frameCount <= blockedThroughFrame;
    private GameObject panel;
    private RunManager run;
    private float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { active = null; blockedThroughFrame = -1; }

    public static void Create(RunManager owner)
    {
        var obj = new GameObject("RunPauseUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        obj.transform.SetParent(owner.transform, false);
        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15000;
        CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        var ui = obj.AddComponent<RunPauseUI>();
        ui.run = owner;
        ui.Build();
    }

    private void Build()
    {
        RectTransform background = Rect("PausePanel", transform, Vector2.zero, Vector2.zero);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.gameObject.AddComponent<Image>().color = new Color(.02f, .03f, .05f, .95f);
        panel = background.gameObject;
        Label(background, "일시정지", 150f, 40f);
        Label(background, "Space 회피 · Shift 달리기 · Tab 보드\nF 상호작용 · Esc 계속", 40f, 24f);
        RectTransform buttonRect = Rect("Resume", background, new Vector2(0f, -100f), new Vector2(280f, 65f));
        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(.12f, .3f, .4f, 1f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Close);
        Label(buttonRect, "계속하기", 0f, 26f);
        panel.SetActive(false);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Label(Transform parent, string text, float y, float size)
    {
        RectTransform rect = Rect("Label", parent, new Vector2(0f, y), new Vector2(800f, 90f));
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        GameFontManager.ApplyFont(label);
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private void Update()
    {
        if (run == null || run.IsRunOver) { Close(); return; }
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (active == this) { Close(); return; }
        if (GameInputKeys.IsGameplayBlocked || Time.timeScale <= 0f || run.IsTransitioning) return;
        StaggerImpactFeedback.CancelActive();
        previousTimeScale = Time.timeScale;
        active = this;
        Time.timeScale = 0f;
        panel.SetActive(true);
    }

    public void Close()
    {
        if (active != this) return;
        panel.SetActive(false);
        if (run != null && !run.IsRunOver && !StageTransitionUI.IsBlockingGameplay && Mathf.Approximately(Time.timeScale, 0f))
            Time.timeScale = previousTimeScale;
        active = null;
        blockedThroughFrame = Time.frameCount;
    }

    private void OnDisable() { Close(); }
}
