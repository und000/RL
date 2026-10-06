using System;

/// <summary>적의 한 번의 스폰에 속한 공격. 풀에서 같은 적을 재사용해도 옛 공격은 되살아나지 않는다.</summary>
public sealed class EnemyAttackContext
{
    public string SourceName { get; }
    public bool IsCancelled { get; private set; }
    public event Action OnCancelled;

    public EnemyAttackContext(string sourceName)
    {
        SourceName = string.IsNullOrWhiteSpace(sourceName) ? "알 수 없는 적" : sourceName;
    }

    public void Cancel()
    {
        if (IsCancelled) return;
        IsCancelled = true;
        Action listeners = OnCancelled;
        OnCancelled = null;
        listeners?.Invoke();
    }
}
