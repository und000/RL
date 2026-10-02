using System;
using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    [SerializeField, Min(1)] private int startingExperienceToNextLevel = 5;
    [SerializeField, Min(1f)] private float experienceGrowthMultiplier = 1.5f;

    private int currentLevel = 1;
    private int currentExperience;
    private int experienceToNextLevel;
    private float experienceRemainder;

    public event Action OnProgressChanged;
    public event Action<int> OnLevelUp;

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

        // 영구 개조의 경험치 배율. 남는 소수점은 들고 있다가
        // 다음에 더해야, 작은 경험치가 반올림으로 사라지지 않는다.
        float scaled = amount * Mathf.Max(0f, MetaProgressRuntime.Bonuses.ExperienceRate)
            + experienceRemainder;
        int granted = Mathf.FloorToInt(scaled);
        experienceRemainder = scaled - granted;
        if (granted <= 0)
        {
            return;
        }

        currentExperience += granted;
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
        OnLevelUp?.Invoke(currentLevel);
    }
}
