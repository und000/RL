using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnTable_", menuName = "Spawning/Enemy Spawn Table")]
public class EnemySpawnTable : ScriptableObject
{
    public enum SpawnMode
    {
        WeightedRandom,
        FixedBatch,
        Sequence
    }

    [Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
        [Min(0)] public int fixedSpawnCount = 1;
        [Min(0f)] public float startTime;
        [Min(0f)] public float endTime;
        [Min(1)] public int minimumPlayerLevel = 1;
        [Min(1)] public int maximumPlayerLevel = 999;
        [Min(1)] public int minimumGroupSize = 1;
        [Min(1)] public int maximumGroupSize = 1;

        public bool IsAvailable(float elapsedTime, int playerLevel)
        {
            bool withinTime = elapsedTime >= startTime &&
                (endTime <= 0f || elapsedTime <= endTime);
            return prefab != null && withinTime &&
                playerLevel >= minimumPlayerLevel && playerLevel <= maximumPlayerLevel;
        }
    }

    [SerializeField] private SpawnMode spawnMode = SpawnMode.WeightedRandom;
    [SerializeField, Min(0.1f)] private float batchInterval = 1.5f;
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public SpawnMode Mode => spawnMode;
    public float BatchInterval => Mathf.Max(0.1f, batchInterval);
    public IReadOnlyList<Entry> Entries => entries;

    public bool TrySelectWeighted(float elapsedTime, int playerLevel, out Entry selected)
    {
        selected = null;
        float totalWeight = 0f;
        foreach (Entry entry in entries)
        {
            if (entry.IsAvailable(elapsedTime, playerLevel) && entry.weight > 0f)
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        foreach (Entry entry in entries)
        {
            if (!entry.IsAvailable(elapsedTime, playerLevel) || entry.weight <= 0f)
            {
                continue;
            }

            roll -= entry.weight;
            if (roll <= 0f)
            {
                selected = entry;
                return true;
            }
        }

        return false;
    }

    public bool TrySelectSequence(
        float elapsedTime,
        int playerLevel,
        ref int sequenceIndex,
        out Entry selected)
    {
        selected = null;
        if (entries.Count == 0) return false;

        int startIndex = Mathf.Abs(sequenceIndex) % entries.Count;
        for (int offset = 0; offset < entries.Count; offset++)
        {
            int index = (startIndex + offset) % entries.Count;
            if (!entries[index].IsAvailable(elapsedTime, playerLevel)) continue;
            selected = entries[index];
            sequenceIndex = (index + 1) % entries.Count;
            return true;
        }

        return false;
    }
}
