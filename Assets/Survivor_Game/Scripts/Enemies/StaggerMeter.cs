using System;

/// <summary>Enemy-only impact state. Tick receives scaled gameplay seconds.</summary>
public sealed class StaggerMeter
{
    public float Maximum { get; private set; } = 30f;
    public float Current { get; private set; }
    public float RemainingDuration { get; private set; }
    public bool IsStaggered => RemainingDuration > 0f;
    public float Ratio => IsStaggered ? 1f : Current / Maximum;
    private float duration = 4f;
    private float decayDelay;
    private float decayPerSecond;
    private float recoveryImmunity;
    private float sinceHit;
    private float immunityRemaining;

    public void Configure(float maximum, float holdDuration, float delay, float decay, float immunity)
    {
        Maximum = Math.Max(1f, maximum);
        duration = Math.Max(0.01f, holdDuration);
        decayDelay = Math.Max(0f, delay);
        decayPerSecond = Math.Max(0f, decay);
        recoveryImmunity = Math.Max(0f, immunity);
        Reset();
    }

    // True only on the hit that crosses the threshold. Further hits never refresh the hold.
    public bool AddImpact(float impact)
    {
        if (impact <= 0f || float.IsNaN(impact) || IsStaggered || immunityRemaining > 0f) return false;
        sinceHit = 0f;
        Current = Math.Min(Maximum, Current + impact);
        if (Current < Maximum) return false;
        RemainingDuration = duration;
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (IsStaggered)
        {
            float leftover = Math.Max(0f, deltaTime - RemainingDuration);
            RemainingDuration = Math.Max(0f, RemainingDuration - deltaTime);
            if (!IsStaggered)
            {
                Current = 0f;
                sinceHit = 0f;
                immunityRemaining = Math.Max(0f, recoveryImmunity - leftover);
            }
            return;
        }
        immunityRemaining = Math.Max(0f, immunityRemaining - deltaTime);
        float decayTimeBefore = Math.Max(0f, sinceHit - decayDelay);
        sinceHit += deltaTime;
        float decayTime = Math.Max(0f, sinceHit - decayDelay) - decayTimeBefore;
        Current = Math.Max(0f, Current - decayPerSecond * decayTime);
    }

    public void Reset()
    {
        Current = 0f;
        RemainingDuration = 0f;
        sinceHit = 0f;
        immunityRemaining = 0f;
    }
}
