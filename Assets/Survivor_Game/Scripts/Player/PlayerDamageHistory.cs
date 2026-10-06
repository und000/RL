public enum PlayerDamageKind { Unknown = 0, Contact = 1, Projectile = 2 }

/// <summary>실제로 체력을 잃은 피해만 기록한다. 무적 중 충돌은 호출하지 않는다.</summary>
public sealed class PlayerDamageHistory
{
    public int HitCount { get; private set; }
    public int TotalHealthLost { get; private set; }
    public int LastHealthLost { get; private set; }
    public string LastSource { get; private set; }
    public PlayerDamageKind LastKind { get; private set; }

    public void Record(int healthLost, string source, PlayerDamageKind kind)
    {
        if (healthLost <= 0) return;
        HitCount++;
        TotalHealthLost += healthLost;
        LastHealthLost = healthLost;
        LastSource = string.IsNullOrWhiteSpace(source) ? "알 수 없는 원인" : source;
        LastKind = kind;
    }

    public string DescribeLastHit()
    {
        if (HitCount == 0) return "기록된 피해 없음";
        string kind = LastKind == PlayerDamageKind.Contact ? "접촉" :
            LastKind == PlayerDamageKind.Projectile ? "투사체" : "기타 피해";
        return $"{LastSource} · {kind} · 체력 -{LastHealthLost}";
    }
}
