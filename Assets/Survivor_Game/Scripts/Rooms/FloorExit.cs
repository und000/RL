using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>스테이지당 하나인 출구. 전투방을 모두 클리어한 뒤 직접 사용한다.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Rooms/Floor Exit")]
public class FloorExit : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Renderer markerRenderer;
    [SerializeField, Min(0.1f)] private float interactionRadius = 2.5f;
    [SerializeField] private Color lockedColor = new Color(.35f, .35f, .4f, 1f);
    [SerializeField] private Color unlockedColor = new Color(.2f, .9f, 1f, 1f);

    private RunManager manager;
    private Transform player;
    private bool lastUnlocked;
    private bool lastTransitioning;
    private MaterialPropertyBlock propertyBlock;

    public bool IsUnlocked => manager != null && manager.IsCurrentFloorCleared;
    public bool CanUse => isActiveAndEnabled && IsUnlocked && manager != null &&
        manager.isActiveAndEnabled && !manager.IsRunOver && !manager.IsTransitioning && player != null &&
        Time.timeScale > 0f && !GameInputKeys.IsGameplayBlocked &&
        Vector2.Distance(player.position, transform.position) <= interactionRadius;

    public void Configure(RunManager runManager, Transform playerTransform)
    {
        manager = runManager;
        player = playerTransform;
        if (labelText != null)
        {
            GameFontManager.ApplyFont(labelText);
            labelText.gameObject.SetActive(true);
        }
        RefreshVisual();
    }

    private void Update()
    {
        if (lastUnlocked != IsUnlocked || lastTransitioning != (manager != null && manager.IsTransitioning)) RefreshVisual();
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[GameInputKeys.Interact].wasPressedThisFrame) TryUse();
    }

    public bool TryUse()
    {
        if (!CanUse || !manager.TryUseFloorExit(this)) return false;
        RefreshVisual();
        return true;
    }

    private void RefreshVisual()
    {
        lastUnlocked = IsUnlocked;
        lastTransitioning = manager != null && manager.IsTransitioning;
        if (labelText != null)
        {
            labelText.text = lastTransitioning ? "이동 중…" : IsUnlocked
                ? GameInputKeys.InteractPrompt + " " + manager.ExitDestinationLabel
                : manager != null && manager.IsBossStage ? "출구 · 선택 보스 처치 후 개방" : "출구 · 모든 전투방 클리어 후 개방";
        }
        if (markerRenderer == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        markerRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", IsUnlocked ? unlockedColor : lockedColor);
        markerRenderer.SetPropertyBlock(propertyBlock);
    }
}
