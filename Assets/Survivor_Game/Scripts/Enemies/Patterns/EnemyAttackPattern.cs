using System.Collections;
using UnityEngine;

public abstract class EnemyAttackPattern : MonoBehaviour
{
    [Header("패턴 선택 조건")]
    [SerializeField, Min(0f)] private float selectionWeight = 1f;
    [SerializeField, Min(0f)] private float minimumRange;
    [SerializeField, Min(0f)] private float maximumRange = 100f;
    [Tooltip("비우면 모든 페이즈에서 사용합니다. 페이즈는 1부터 시작합니다.")]
    [SerializeField] private int[] availablePhases;
    [SerializeField] private bool stopMovementWhileExecuting;

    private float nextAvailableTime;
    private EnemyAwareness awareness;
    public float SelectionWeight => selectionWeight;
    public bool StopMovementWhileExecuting => stopMovementWhileExecuting;
    protected abstract float Cooldown { get; }
    protected virtual float InitialDelay => 0f;

    protected virtual void Awake()
    {
        awareness = GetComponent<EnemyAwareness>();
    }

    public void InitializeAvailability()
    {
        nextAvailableTime = Time.time + Mathf.Max(0f, InitialDelay);
    }

    public bool CanExecute(Transform target, int phase)
    {
        if (!isActiveAndEnabled || target == null || Time.time < nextAvailableTime ||
            (awareness != null && !awareness.CanAct))
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        float validMaximumRange = Mathf.Max(minimumRange, maximumRange);
        return distance >= minimumRange && distance <= validMaximumRange &&
            IsAvailableInPhase(phase) && CanExecutePattern(target);
    }

    public IEnumerator Execute(Transform target)
    {
        nextAvailableTime = Time.time + Mathf.Max(0f, Cooldown);
        yield return ExecutePattern(target);
    }

    protected virtual bool CanExecutePattern(Transform target) => true;
    protected abstract IEnumerator ExecutePattern(Transform target);

    private bool IsAvailableInPhase(int phase)
    {
        if (availablePhases == null || availablePhases.Length == 0)
        {
            return true;
        }

        foreach (int availablePhase in availablePhases)
        {
            if (availablePhase == phase)
            {
                return true;
            }
        }

        return false;
    }
}
