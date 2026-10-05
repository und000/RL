using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>보스방에 들어온 뒤 후보 하나를 선택한다. 선택 성공 시 나머지 후보는 방이 거둔다.</summary>
[DisallowMultipleComponent]
public class BossChoicePedestal : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField, Min(.1f)] private float interactionRadius = 2.5f;
    private RoomInstance room;
    private GameObject bossPrefab;
    private Transform player;

    public void Configure(RoomInstance owner, GameObject candidate, Transform playerTransform)
    {
        room = owner;
        bossPrefab = candidate;
        player = playerTransform;
        EnemyHealth health = candidate != null ? candidate.GetComponent<EnemyHealth>() : null;
        string title = health != null && health.Profile != null ? health.Profile.DisplayName : "보스";
        if (labelText != null)
        {
            GameFontManager.ApplyFont(labelText);
            EnemyProjectileAttackPattern pattern = candidate != null ? candidate.GetComponent<EnemyProjectileAttackPattern>() : null;
            string threat = pattern != null ? "\n" + pattern.DescribeAttack() : string.Empty;
            labelText.text = title + threat + "\n" + GameInputKeys.InteractPrompt + " 전투 시작";
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[GameInputKeys.Interact].wasPressedThisFrame) TryChoose();
    }

    public bool TryChoose()
    {
        return isActiveAndEnabled && room != null && player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRadius &&
            room.TryChooseBoss(bossPrefab);
    }
}
