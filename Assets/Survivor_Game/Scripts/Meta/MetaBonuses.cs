using UnityEngine;

/// <summary>
/// 올려 둔 개조를 전부 합친 결과. 런이 시작할 때 한 번 만들고,
/// 개조 화면에서 뭔가 바뀌면 다시 만든다.
/// </summary>
public sealed class MetaBonuses
{
    /// <summary>개조 표가 없을 때 쓰는 빈 값. 아무것도 더하지 않는다.</summary>
    public static readonly MetaBonuses Empty = new MetaBonuses();

    /// <summary>코어 보드·장비와 같은 자리에 더해지는 능력치들.</summary>
    public CoreBoardStats Stats { get; } = new CoreBoardStats();

    public int StartingCredits { get; private set; }
    /// <summary>0.1 = 10% 싸게. 공짜가 되지 않도록 위를 막아 둔다.</summary>
    public float ShopDiscountRate { get; private set; }
    public int RewardChoiceBonus { get; private set; }
    /// <summary>곱해서 쓰는 값이라 기본이 1이다.</summary>
    public float ExperienceRate { get; private set; } = 1f;
    public float SalvageRate { get; private set; } = 1f;
    public int ReviveCount { get; private set; }

    private const float MaximumShopDiscount = 0.6f;

    public static MetaBonuses Build(MetaUpgradeTree tree)
    {
        if (tree == null) return Empty;

        MetaBonuses bonuses = new MetaBonuses();
        foreach (MetaUpgradeNode node in tree.Nodes)
        {
            int level = MetaProgress.GetLevel(node);
            if (level <= 0 || node.Effects == null) continue;

            foreach (MetaEffect effect in node.Effects)
            {
                bonuses.Accumulate(effect, level);
            }
        }

        bonuses.ShopDiscountRate =
            Mathf.Clamp(bonuses.ShopDiscountRate, 0f, MaximumShopDiscount);
        return bonuses;
    }

    private void Accumulate(MetaEffect effect, int level)
    {
        float total = effect.valuePerLevel * level;
        switch (effect.kind)
        {
            case MetaEffectKind.Stat:
                Stats.Add(effect.stat, total);
                break;
            case MetaEffectKind.StartingCredits:
                StartingCredits += Mathf.RoundToInt(total);
                break;
            case MetaEffectKind.ShopDiscountRate:
                ShopDiscountRate += total;
                break;
            case MetaEffectKind.RewardChoiceCount:
                RewardChoiceBonus += Mathf.RoundToInt(total);
                break;
            case MetaEffectKind.ExperienceRate:
                ExperienceRate += total;
                break;
            case MetaEffectKind.SalvageRate:
                SalvageRate += total;
                break;
            case MetaEffectKind.Revive:
                ReviveCount += Mathf.RoundToInt(total);
                break;
        }
    }
}
