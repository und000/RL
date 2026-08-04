using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private Slider healthSlider;

    [SerializeField]
    private TMP_Text healthText;

    private void Start()
    {
        if (playerHealth == null ||
            healthSlider == null ||
            healthText == null)
        {
            Debug.LogError("Player Health UI에 필요한 오브젝트가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        playerHealth.OnHealthChanged += UpdateHealthUI;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        int currentHealth = playerHealth.GetCurrentHealth();
        int maxHealth = playerHealth.GetMaxHealth();

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;

        healthText.text = $"{currentHealth} / {maxHealth}";
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }
}