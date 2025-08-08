using UnityEngine;

public class EnemyHealthManager : MonoBehaviour
{

    [SerializeField] private float maxHealth = 100f; // Maximum health of the enemy
    public float MaxHealth => maxHealth; // Property to access maximum health
    private float currentHealth; // Current health of the enemy
    public float CurrentHealth => currentHealth; // Property to access current health

    private Animator animator; // Reference to the Animator component

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth; // Initialize current health to maximum health
        animator = GetComponent<Animator>(); // Get the Animator component attached to the enemy
    }

    public void LoadHealthValues(int _maxHealth, int _currentHealth)
    {
        maxHealth = _maxHealth; // Set the maximum health from the loaded data
        currentHealth = _currentHealth; // Set the current health from the loaded data
    }
    /// <summary>
    /// Reduces the current health of the entity by the specified damage amount.
    /// </summary>
    /// <remarks>If the entity's health drops to zero or below, the death animation is triggered, and the
    /// entity is marked as dead. Additionally, if the entity has an <see cref="EnemyController"/> component, it will be
    /// disabled.</remarks>
    /// <param name="damageAmount">The amount of damage to apply. Must be a positive value.</param>
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0) {
            
            animator.SetTrigger("Die"); // Trigger the death animation
            animator.SetBool("isDead", true); // Set the IsDead parameter to true in the Animator
            if (this.TryGetComponent<EnemyCore>(out var enemyController)) {
                enemyController.enabled = false; // Disable the EnemyController script
            }
            this.tag = "Untagged"; // Remove the enemy's tag
        } else {
            if (TryGetComponent<EnemyCore>(out var enemy)) {
                enemy.WasDamaged(); // Ensure the enemy is not idle when taking damage
            }
        }
    }
}
