using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 방 보상이 놓이는 받침대. 플레이어가 겹친 상태에서 획득 키를 누르면 지급한다.
/// 같은 방의 받침대는 서로 배타적이라, 하나를 가져가면 나머지는 사라진다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Rooms/Reward Pedestal")]
public class RewardPedestal : MonoBehaviour
{
    [Header("구성 요소")]
    [Tooltip("보상 색으로 물들일 받침대 렌더러. 공유 머테리얼을 건드리지 않도록 " +
        "MaterialPropertyBlock으로만 색을 넣는다.")]
    [SerializeField] private Renderer tintRenderer;
    [Tooltip("보상 아이콘. 스프라이트가 준비되면 연결한다.")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [Tooltip("보상 이름을 띄우는 월드 텍스트. 평소에는 꺼 두고 겹쳤을 때만 켠다.")]
    [SerializeField] private TMP_Text labelText;
    [Tooltip("플레이어를 감지하는 트리거 콜라이더.")]
    [SerializeField] private Collider2D playerDetector;
    [Tooltip("등급을 내보이는 연출. 비워 두면 등급 연출 없이 동작한다.")]
    [SerializeField] private ItemGradeVisual gradeVisual;

    // 겹친 상태에서 GameInputKeys.Interact를 눌러야 획득된다.
    // 드롭과 같은 키를 쓰도록 그 한 곳에서만 정한다.

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private RoomRewardDefinition reward;
    private RoomInstance owner;
    private GameObject overlappingPlayer;
    private string lastLabelLine;
    private int price;
    private bool exclusive = true;
    private bool claimed;

    public RoomRewardDefinition Reward => reward;
    public int Price => price;
    public bool IsClaimed => claimed;

    /// <summary>방이 받침대를 놓은 직후 무엇을 담을지 알려 준다.</summary>
    public void Configure(RoomRewardDefinition definition, RoomInstance room) =>
        Configure(definition, room, 0, true);

    /// <summary>
    /// 가격과 배타 여부까지 정해 준다. 값이 붙으면 재화를 치러야 하고,
    /// 배타적이지 않으면(상점) 하나를 사도 나머지가 남는다.
    /// </summary>
    public void Configure(
        RoomRewardDefinition definition, RoomInstance room, int rewardPrice, bool isExclusive)
    {
        reward = definition;
        owner = room;
        price = Mathf.Max(0, rewardPrice);
        exclusive = isExclusive;
        claimed = false;

        // 본체도 등급 색으로 물들여, 색만 보고 등급을 알 수 있게 한다.
        Color tint = definition != null ? definition.GradeColor : Color.white;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = definition != null ? definition.Icon : null;
            iconRenderer.color = definition != null ? definition.TintColor : Color.white;
            iconRenderer.enabled = iconRenderer.sprite != null;
        }
        ApplyTint(tint);
        if (gradeVisual != null)
        {
            gradeVisual.Apply(definition != null ? definition.Grade : ItemGrade.Standard);
        }
        if (labelText != null)
        {
            // 런타임에 만들어지는 오브젝트라 GameFontManager의 일괄 적용 대상에서 빠진다.
            GameFontManager.ApplyFont(labelText);
            labelText.gameObject.SetActive(false);
        }
    }

    /// <summary>받침대 색만 이 오브젝트에서 바꾼다. 같은 머테리얼을 쓰는 다른 받침대는 그대로다.</summary>
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
        if (claimed || overlappingPlayer == null || reward == null || GameInputKeys.IsGameplayBlocked) return;

        // 체력이나 재화가 변하면 안내 문구도 따라 바뀌어야 한다.
        RefreshLabel();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard[GameInputKeys.Interact].wasPressedThisFrame) return;

        Claim();
    }

    private void Claim()
    {
        PlayerWallet wallet = price > 0
            ? overlappingPlayer.GetComponentInParent<PlayerWallet>() : null;
        if (price > 0 && (wallet == null || !wallet.CanAfford(price))) return;

        // 효과가 들어가지 않는 보상(가득 찬 체력 등)에 값을 치르게 하지 않는다.
        if (!reward.Grant(overlappingPlayer)) return;
        if (price > 0) wallet.TrySpend(price);

        claimed = true;
        if (labelText != null) labelText.gameObject.SetActive(false);
        if (owner != null) owner.NotifyRewardClaimed(this, exclusive);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (claimed || !other.CompareTag("Player")) return;

        overlappingPlayer = other.gameObject;
        ShowLabel(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (overlappingPlayer == null || other.gameObject != overlappingPlayer) return;

        overlappingPlayer = null;
        ShowLabel(false);
    }

    private void ShowLabel(bool visible)
    {
        if (labelText == null) return;

        if (visible && reward != null) RefreshLabel();
        labelText.gameObject.SetActive(visible);
    }

    /// <summary>문구가 실제로 바뀔 때만 텍스트를 갈아 끼운다.</summary>
    private void RefreshLabel()
    {
        if (labelText == null || reward == null) return;

        string line = BuildDisplayLine() +
            "\n" + GameInputKeys.InteractPrompt + " 획득";
        if (overlappingPlayer != null && !string.IsNullOrEmpty(reward.ReactiveDescription))
            line += "\n" + reward.ReactiveDescription;
        if (line == lastLabelLine) return;

        lastLabelLine = line;
        labelText.text = line;
    }

    /// <summary>가격이 붙어 있으면 값과 잔액 부족 여부까지 함께 보여 준다.</summary>
    private string BuildDisplayLine()
    {
        // 등급 이름을 앞에 색으로 붙인다.
        string line = ItemGradeInfo.ColoredName(reward.Grade) + " " + reward.BuildLabel();
        // 왜 상호작용이 안 먹는지 알려 준다. 체력이 가득 찬 상태의 회복 같은 경우다.
        if (!reward.CanGrant(overlappingPlayer)) line += "   (" + reward.GetUnavailableReason(overlappingPlayer) + ")";
        if (price <= 0) return line;

        PlayerWallet wallet = overlappingPlayer != null
            ? overlappingPlayer.GetComponentInParent<PlayerWallet>() : null;
        bool affordable = wallet != null && wallet.CanAfford(price);
        return line + "   " + price + " C" + (affordable ? string.Empty : " (부족)");
    }

    private void OnValidate()
    {
        if (playerDetector != null && !playerDetector.isTrigger)
        {
            Debug.LogWarning(
                "Player Detector는 Is Trigger가 켜져 있어야 획득을 감지합니다.", this);
        }
    }
}
