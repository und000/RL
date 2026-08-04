using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    [SerializeField]
    [Min(1)]
    private int startingExperienceToNextLevel = 5;

    [SerializeField]
    [Min(1f)]
    private float experienceGrowthMultiplier = 1.5f;

    private int currentLevel = 1;
    private int currentExperience;
    private int experienceToNextLevel;

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

        Debug.Log(
            $"레벨: {currentLevel}, 경험치: " +
            $"{currentExperience} / {experienceToNextLevel}"
        );
    }

    private void LevelUp()
    {
        currentLevel++;

        experienceToNextLevel = Mathf.CeilToInt(
            experienceToNextLevel * experienceGrowthMultiplier
        );

        Debug.Log($"레벨 업! 현재 레벨: {currentLevel}");
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetCurrentExperience()
    {
        return currentExperience;
    }

    public int GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }
}