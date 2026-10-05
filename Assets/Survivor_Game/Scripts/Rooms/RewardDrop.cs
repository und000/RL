using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 바닥에 떨어져 있는 보상. 방을 클리어하면 던져지듯 튀어나오고,
/// 플레이어가 다가가 상호작용 키를 눌러야 들어온다. 지나가다 실수로
/// 주워지지 않으므로, 남겨 두고 나중에 돌아와도 된다. 효과가 없는 상태
/// (체력이 가득 찼을 때의 회복 등)에서는 소모되지 않고 그대로 남는다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Rooms/Reward Drop")]
public class RewardDrop : MonoBehaviour
{
    [Header("구성 요소")]
    [Tooltip("보상 색으로 물들일 렌더러. 공유 머테리얼을 건드리지 않도록 " +
        "MaterialPropertyBlock으로만 색을 넣는다.")]
    [SerializeField] private Renderer tintRenderer;
    [SerializeField] private TMP_Text labelText;
    [Tooltip("플레이어를 감지하는 트리거 콜라이더.")]
    [SerializeField] private Collider2D playerDetector;
    [Tooltip("등급을 내보이는 연출. 비워 두면 등급 연출 없이 동작한다.")]
    [SerializeField] private ItemGradeVisual gradeVisual;

    [Header("던지는 연출")]
    [Tooltip("떨어진 자리까지 날아가는 시간. 이 동안에는 주울 수 없다.")]
    [SerializeField, Min(0f)] private float tossDuration = 0.45f;
    [Tooltip("포물선의 높이.")]
    [SerializeField, Min(0f)] private float tossHeight = 2.5f;

    // 겹친 상태에서 GameInputKeys.Interact를 눌러야 주워진다.
    // 밟는 것만으로는 들어오지 않으며, 키는 그 한 곳에서만 정한다.

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private RoomRewardDefinition reward;
    private GameObject overlappingPlayer;
    private Vector2 tossFrom;
    private Vector2 tossTo;
    private float tossElapsed;
    private bool tossing;
    private string lastLabelLine;

    public RoomRewardDefinition Reward => reward;

    /// <summary>들고 있던 무기를 갈아치우는 경우에는 문구를 달리 보여 준다.</summary>
    private bool ReplacesEquipment =>
        reward != null &&
        (reward.Kind == RoomRewardKind.Weapon ||
            reward.Kind == RoomRewardKind.Equipment || reward.Kind == RoomRewardKind.SpecialAttack);

    /// <summary>무엇을 담을지 정하고, from에서 to로 던져지는 연출을 시작한다.</summary>
    public void Configure(RoomRewardDefinition definition, Vector2 from, Vector2 to)
    {
        reward = definition;
        tossFrom = from;
        tossTo = to;
        tossElapsed = 0f;
        tossing = tossDuration > 0f;

        transform.position = from;
        if (playerDetector != null) playerDetector.enabled = !tossing;

        // 본체도 등급 색으로 물들여, 색만 보고 등급을 알 수 있게 한다.
        Color tint = definition != null ? definition.GradeColor : Color.white;
        ApplyTint(tint);
        if (gradeVisual != null)
        {
            gradeVisual.Apply(definition != null ? definition.Grade : ItemGrade.Standard);
        }

        if (labelText != null)
        {
            GameFontManager.ApplyFont(labelText);
            lastLabelLine = null;
            RefreshLabel();
        }
    }

    private void ApplyTint(Color tint)
    {
        if (tintRenderer == null) return;

        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        tintRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, tint);
        tintRenderer.SetPropertyBlock(propertyBlock);
    }

    private void Update()
    {
        if (tossing)
        {
            AdvanceToss();
            return;
        }

        if (reward == null) return;
        RefreshLabel();

        if (overlappingPlayer == null || GameInputKeys.IsGameplayBlocked) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null ||
            !keyboard[GameInputKeys.Interact].wasPressedThisFrame) return;

        TryClaim();
    }

    /// <summary>포물선을 그리며 떨어진 자리로 날아간다.</summary>
    private void AdvanceToss()
    {
        tossElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(tossElapsed / Mathf.Max(0.01f, tossDuration));

        Vector2 ground = Vector2.Lerp(tossFrom, tossTo, progress);
        float arc = tossHeight * 4f * progress * (1f - progress);
        transform.position = new Vector3(ground.x, ground.y + arc, 0f);

        if (progress < 1f) return;

        tossing = false;
        transform.position = tossTo;
        if (playerDetector != null) playerDetector.enabled = true;
    }

    private void TryClaim()
    {
        // 효과가 없으면 소모하지 않고 그대로 둔다. 나중에 다쳐서 돌아오면 그때 주우면 된다.
        if (!reward.Grant(overlappingPlayer)) return;
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        overlappingPlayer = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (overlappingPlayer == null || other.gameObject != overlappingPlayer) return;
        overlappingPlayer = null;
    }

    /// <summary>문구가 실제로 바뀔 때만 텍스트를 갈아 끼운다.</summary>
    private void RefreshLabel()
    {
        if (labelText == null || reward == null) return;

        // 등급 이름을 앞에 색으로 붙여, 멀리서도 무엇이 떨어졌는지 보이게 한다.
        string line = ItemGradeInfo.ColoredName(reward.Grade) + " " + reward.BuildLabel();
        // 겹쳐야 무엇을 눌러야 하는지 알려 준다. 멀리서는 이름만 보인다.
        if (overlappingPlayer != null)
        {
            line += "\n" + GameInputKeys.InteractPrompt +
                (ReplacesEquipment ? " 교체" : " 획득");
            if (!reward.CanGrant(overlappingPlayer)) line += "   (" + reward.GetUnavailableReason(overlappingPlayer) + ")";
        }
        if (line == lastLabelLine) return;

        lastLabelLine = line;
        labelText.text = line;
    }

    private void OnDestroy()
    {
        // 교체로 떨어진 무기처럼 코드에서 만든 보상은 이 드롭만 쓰므로 함께 버린다.
        if (reward != null && reward.IsRuntimeCopy) Destroy(reward);
    }

    private void OnValidate()
    {
        if (playerDetector != null && !playerDetector.isTrigger)
        {
            Debug.LogWarning(
                "Player Detector는 Is Trigger가 켜져 있어야 줍기를 감지합니다.", this);
        }
    }
}
