using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Enemies/Patterns/Enemy Pattern Controller")]
public class EnemyPatternController : MonoBehaviour, IEnemyPoolLifecycle
{
    [SerializeField, Min(0.02f)] private float decisionInterval = 0.15f;

    private readonly List<EnemyAttackPattern> candidates = new List<EnemyAttackPattern>(8);
    private EnemyAttackPattern[] patterns;
    private EnemyPhaseController phaseController;
    private EnemyMovement enemyMovement;
    private EnemyStagger stagger;
    private Rigidbody2D body;
    private Transform target;
    private Coroutine runningPattern;
    private float nextDecisionTime;
    private bool movementDisabledByPattern;

    private void Awake()
    {
        patterns = GetComponents<EnemyAttackPattern>();
        phaseController = GetComponent<EnemyPhaseController>();
        enemyMovement = GetComponent<EnemyMovement>();
        stagger = GetComponent<EnemyStagger>();
        body = GetComponent<Rigidbody2D>();

        foreach (EnemyAttackPattern pattern in patterns)
        {
            pattern.InitializeAvailability();
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    private void Update()
    {
        if (stagger != null && stagger.IsStaggered) return;
        if (runningPattern != null || target == null || Time.time < nextDecisionTime)
        {
            return;
        }

        nextDecisionTime = Time.time + decisionInterval;
        EnemyAttackPattern selectedPattern = SelectPattern();
        if (selectedPattern != null)
        {
            runningPattern = StartCoroutine(RunPattern(selectedPattern));
        }
    }

    private EnemyAttackPattern SelectPattern()
    {
        candidates.Clear();
        float totalWeight = 0f;
        int currentPhase = phaseController != null ? phaseController.CurrentPhase : 1;

        foreach (EnemyAttackPattern pattern in patterns)
        {
            if (!pattern.CanExecute(target, currentPhase) || pattern.SelectionWeight <= 0f)
            {
                continue;
            }

            candidates.Add(pattern);
            totalWeight += pattern.SelectionWeight;
        }

        if (candidates.Count == 0 || totalWeight <= 0f)
        {
            return null;
        }

        float selection = Random.value * totalWeight;
        foreach (EnemyAttackPattern candidate in candidates)
        {
            selection -= candidate.SelectionWeight;
            if (selection <= 0f)
            {
                return candidate;
            }
        }

        return candidates[candidates.Count - 1];
    }

    private IEnumerator RunPattern(EnemyAttackPattern pattern)
    {
        bool stoppedMovement = pattern.StopMovementWhileExecuting && enemyMovement != null;
        if (stoppedMovement)
        {
            enemyMovement.enabled = false;
            movementDisabledByPattern = true;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        yield return pattern.Execute(target);

        if (stoppedMovement && enemyMovement != null)
        {
            enemyMovement.enabled = true;
            movementDisabledByPattern = false;
        }

        runningPattern = null;
    }

    private void OnDisable()
    {
        StopRunningPattern();
    }

    public void OnEnemySpawned()
    {
        StopRunningPattern();
        nextDecisionTime = Time.time;
        foreach (EnemyAttackPattern pattern in patterns)
        {
            if (pattern != null) pattern.InitializeAvailability();
        }
    }

    public void OnEnemyDespawned()
    {
        StopRunningPattern();
    }

    private void StopRunningPattern()
    {
        if (runningPattern != null)
        {
            StopCoroutine(runningPattern);
            runningPattern = null;
        }

        if (movementDisabledByPattern && enemyMovement != null)
        {
            enemyMovement.enabled = true;
        }
        movementDisabledByPattern = false;
    }

    public void InterruptForStagger()
    {
        StopRunningPattern();
        nextDecisionTime = Time.time + decisionInterval;
        if (body != null) body.linearVelocity = Vector2.zero;
    }
}
