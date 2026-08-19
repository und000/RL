using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ScenePlacedEnemyManager : MonoBehaviour
{
    private static readonly List<ScenePlacedEnemy> members = new List<ScenePlacedEnemy>();
    private static ScenePlacedEnemyManager instance;
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateManager()
    {
        if (instance != null) return;
        GameObject managerObject = new GameObject("ScenePlacedEnemyManager");
        instance = managerObject.AddComponent<ScenePlacedEnemyManager>();
        DontDestroyOnLoad(managerObject);
    }

    public static void Register(ScenePlacedEnemy member)
    {
        if (member != null && !members.Contains(member)) members.Add(member);
    }

    public static void Unregister(ScenePlacedEnemy member)
    {
        members.Remove(member);
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
            if (player == null) return;
        }

        float now = Time.unscaledTime;
        for (int index = members.Count - 1; index >= 0; index--)
        {
            ScenePlacedEnemy member = members[index];
            if (member == null)
            {
                members.RemoveAt(index);
                continue;
            }
            if (now < member.NextCheckTime) continue;
            member.NextCheckTime = now + member.CheckInterval;
            if (member.IsSpawnerManaged ||
                (member.TryGetComponent(out PooledEnemy pooled) && pooled.IsConfigured))
            {
                continue;
            }
            bool shouldBeActive = ((Vector2)member.transform.position -
                (Vector2)player.position).sqrMagnitude <= member.ActivationRangeSquared;
            if (member.gameObject.activeSelf != shouldBeActive)
            {
                member.gameObject.SetActive(shouldBeActive);
            }
        }
    }
}
