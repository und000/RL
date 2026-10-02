using UnityEngine;

/// <summary>
/// 씬에 하나 놓아 두는 영구 진행의 창구. 개조 표를 들고 있다가
/// 런이 시작할 때 합산해 두면, 지갑이나 스탯 쪽에서 그 결과만 읽어 간다.
/// 다른 컴포넌트보다 먼저 깨어나야 하므로 실행 순서를 앞으로 당겨 두었다.
/// </summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
[AddComponentMenu("Meta/Meta Progress Runtime")]
public class MetaProgressRuntime : MonoBehaviour
{
    [Tooltip("영구 개조 표. 비워 두면 개조 보너스 없이 그냥 돌아간다.")]
    [SerializeField] private MetaUpgradeTree tree;
    [Tooltip("켜면 시작할 때 지금 걸려 있는 보너스를 콘솔에 적어 준다.")]
    [SerializeField] private bool logOnStart;

    private static MetaUpgradeTree activeTree;
    private static MetaBonuses cached;

    /// <summary>지금 런에 걸려 있는 보너스. 씬에 이 컴포넌트가 없어도 빈 값이 돌아온다.</summary>
    public static MetaBonuses Bonuses
    {
        get
        {
            if (cached == null) cached = MetaBonuses.Build(activeTree);
            return cached;
        }
    }

    public static MetaUpgradeTree Tree => activeTree;

    /// <summary>개조를 사고 난 뒤처럼 합산을 다시 해야 할 때 부른다.</summary>
    public static void Refresh() => cached = MetaBonuses.Build(activeTree);

    /// <summary>
    /// 도메인 다시 불러오기를 꺼 둔 설정에서도 앞선 플레이의 값이 남지 않게 비운다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeTree = null;
        cached = null;
    }

    private void Awake()
    {
        activeTree = tree;
        cached = MetaBonuses.Build(activeTree);

        if (!logOnStart) return;
        Debug.Log(
            "[영구 개조] 잔존 코드 " + MetaProgress.Salvage +
            " · 시작 크레딧 +" + Bonuses.StartingCredits +
            " · 재기동 " + Bonuses.ReviveCount +
            " · 능력치 " + Bonuses.Stats, this);
    }

    private void OnDestroy()
    {
        // 씬을 다시 불러올 때 사라진 표를 계속 붙들고 있지 않게 한다.
        if (activeTree != tree) return;
        activeTree = null;
        cached = null;
    }
}
