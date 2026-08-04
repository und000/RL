using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    [Min(1)]
    private int maxHealth = 5;

    private int currentHealth;
    private bool isDead;

    private Rigidbody2D body;
    private PlayerMovement playerMovement;
    private AutoWeapon autoWeapon;
    private Collider2D playerCollider;
    private SpriteRenderer spriteRenderer;

    public event Action OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;

        body = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        autoWeapon = GetComponent<AutoWeapon>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        Debug.Log($"Player 남은 체력: {currentHealth} / {maxHealth}");

        OnHealthChanged?.Invoke();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (autoWeapon != null)
        {
            autoWeapon.enabled = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }

        Debug.Log("Player가 사망했습니다.");
    }
}