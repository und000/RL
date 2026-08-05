using System;
using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    [SerializeField, Min(1)] private int startingExperienceToNextLevel = 5;
    [SerializeField, Min(1f)] private float experienceGrowthMultiplier = 1.5f;

    private int currentLevel = 1;
    private int currentExperience;
    private int experienceToNextLevel;

    public event Action OnProgressChanged;

    private void Awake()
    {
        experienceToNextLevel = startingExperienceToNextLevel;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentExperience += amount;
        while (currentExperience >= experienceToNextLevel)
        {
            currentExperience -= experienceToNextLevel;
            LevelUp();
        }

        OnProgressChanged?.Invoke();
    }

    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public int GetExperienceToNextLevel() => experienceToNextLevel;

    private void LevelUp()
    {
        currentLevel++;
        experienceToNextLevel = Mathf.CeilToInt(
            experienceToNextLevel * experienceGrowthMultiplier
        );
    }
}
