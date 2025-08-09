using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class TempMaxHealthData
{
    public float currentHealth;
    public float maxHealth;
}

public class HealthFunctions : MonoBehaviour
{
    [SerializeField]
    protected float currentHealth;
    public float GetCurrentHealth => currentHealth; // Public property to access current health
    [FoldoutGroup("Health Settings"), SerializeField, Tooltip("Maximum health the player starts with")]
    protected float maxHealth = 100f;
    public float GetMaxHealth => maxHealth; // Public property to access maximum health

    [FoldoutGroup("Health Settings"), SerializeField, Tooltip("How much health is gained per tick of health regen")]
    protected float healthRegenRate = 1f;
    [FoldoutGroup("Health Settings"), SerializeField, Tooltip("The delay after getting attacked before health regen starts again")]
    protected float healthRegenDamageDelay = 5f;
    [FoldoutGroup("Health Settings"), SerializeField, Tooltip("The time to wait in frames between health regen ticks")]
    protected float healthRegenCooldown = 10f;
    //[FoldoutGroup("Health Settings")]
    //[SerializeField] protected float baseMaxHealth = 100f; // Base maximum health for scaling the health bar width
    protected Image healthBar;
    protected TextMeshProUGUI healthBarText; // Text component to display health percentage
    protected float baseHealthBarWidth; // Base width of the health bar
    protected float baseHealthBarBackgroundWidth; // Base width of the health bar background
    [FoldoutGroup("Player iFrame Settings"), SerializeField, Tooltip("The amount of time in seconds the player will be invincible after getting attacked")]
    protected float iFrameDuration = 2f;
    protected float iFrameTimer; // Timer to track invincibility frame duration
    protected bool isInvincible = false;
    public bool IsInvincible => isInvincible; // Public property to access invincibility status
    [FoldoutGroup("Component References")]
    [SerializeField] protected GameObject floatingText; // Reference to the floating text prefab for displaying health changes
    //[FoldoutGroup("Component References")]
    //[SerializeField] Camera mainCamera; // Reference to the main camera for positioning floating text
    [FoldoutGroup("Component References")]
    [SerializeField] float spawnLocation = 0.5f; // Default spawn location for floating text in viewport coordinates
    [FoldoutGroup("Component References")]
    [SerializeField] protected float spawnLocation1 = 0.5f; // Default spawn location for floating text in viewport coordinates

    protected float healthRegenCooldownTimer = 0f; // Timer for health regeneration cooldown
    protected float healthRegenDamageDelayTimer = 0f; // Timer for health regeneration delay after taking damage
    protected Animator animator;


    /// <summary>
    /// Regenerates the player's health over time, subject to cooldown and damage delay timers.
    /// </summary>
    /// <remarks>Health regeneration occurs only when both the cooldown timer and the damage delay timer have
    /// expired. The amount of health regenerated is determined by the regeneration rate and the elapsed time. The
    /// player's health is clamped between 0 and the maximum health value to ensure it remains within valid bounds. This
    /// method also updates the health bar UI to reflect the current health.</remarks>
    public void HealthRegen()
    {
        healthRegenCooldownTimer -= Time.deltaTime; // Decrease the cooldown timer for health regeneration
        healthRegenDamageDelayTimer -= Time.deltaTime; // Decrease the damage delay timer for health regeneration
        if (healthRegenCooldownTimer <= 0 && healthRegenDamageDelayTimer <= 0) // If both timers have reached zero
        {
            float tempHealth = currentHealth; // Store the current health temporarily
            tempHealth += healthRegenRate * Time.deltaTime; // Regenerate health based on the regeneration rate
            currentHealth = Mathf.Clamp(tempHealth, 0, maxHealth); // Ensure current health does not exceed max health or drop below zero
            UpdateHealthBar(); // Update the health bar UI with the new health value
        }
    }

    /// <summary>
    /// Adjusts the player's health by a specified amount, optionally bypassing animation effects.
    /// </summary>
    /// <remarks>When reducing health, the method checks if the player is invincible and skips the adjustment
    /// if invincibility is active. If animations are not bypassed, the method triggers appropriate animations for hurt
    /// or death based on the health change. Health is clamped between 0 and the maximum health value, and the health
    /// bar UI is updated accordingly.</remarks>
    /// <param name="amount">The amount by which to adjust the player's health. Positive values increase health, while negative values
    /// decrease it.</param>
    /// <param name="bypassAnimator">A value indicating whether to bypass animation effects during the health adjustment.  If <see langword="true"/>,
    /// animations such as hurt or death will not be triggered.</param>
    public void AdjustHealth(float amount, bool bypassAnimator = false)
    {
        Debug.Log("invincible: " + isInvincible + " amount: " + amount + "currentHealth: " + currentHealth);
        if (!bypassAnimator) {
            if (animator == null) // Check if the animator is not null to avoid NullReferenceException
            {
                Debug.LogWarning("Animator component is not assigned or found on the player object.");
                return; // Exit the method if animator is not set
            }
            if (amount < 0) // If health is being reduced
            {
                if (isInvincible) // If the player is currently invincible
                {
                    return; // Do not apply damage if invincible
                }
                healthRegenDamageDelayTimer = healthRegenDamageDelay; // Reset the damage delay timer for health regeneration
                isInvincible = true; // Set the player to be invincible
                animator.SetBool("isHurt", true); // Trigger the hurt animation
                animator.SetTrigger("Hurt"); // Trigger the hurt animation state
            }
        }
        float tempHealth = currentHealth; // Store the current health temporarily
        tempHealth += amount; // Change the current health by the specified amount
        currentHealth = Mathf.Clamp(tempHealth, 0, maxHealth); // Ensure current health does not exceed max health or drop below zero
        UpdateHealthBar(); // Update the health bar UI with the new health value
        if (currentHealth <= 0) // If health drops to zero or below
        {
            animator.SetTrigger("Die"); // Trigger the death animation
            Death();
        }
    }
    
    /// <summary>
    /// Adjusts the current health of the entity by a specified amount, optionally displaying a visual indicator.
    /// </summary>
    /// <remarks>The method ensures that the current health remains within the valid range of 0 to the maximum
    /// health. If the health drops to 0 or below, the death animation is triggered.</remarks>
    /// <param name="showBuff">A value indicating whether to display a visual indicator for the health adjustment. <see langword="true"/> to
    /// show the indicator; otherwise, <see langword="false"/>.</param>
    /// <param name="amount">The amount by which to adjust the current health. Positive values increase health, while negative values
    /// decrease it.</param>
    public void AdjustHealth(bool showBuff, float amount)
    {
        float tempHealth = currentHealth; // Store the current health temporarily
        tempHealth += amount; // Change the current health by the specified amount
        currentHealth = Mathf.Clamp(tempHealth, 0, maxHealth); // Ensure current health does not exceed max health or drop below zero
        UpdateHealthBar(); // Update the health bar UI with the new health value
        if (showBuff) {
            ShowHealthBuff((int)amount, false); // Show a floating text indicating the health change
        }
        if (currentHealth <= 0) // If health drops to zero or below
        {
            animator.SetTrigger("Die"); // Trigger the death animation
            Death();
        }
    }

    private void Death()
    {
        // Handle player death logic here, such as disabling player controls, playing death animation, etc.
        Debug.Log("Player has died."); // Placeholder for death logic
        Managers.Instance.GameManager.Die();
    }

    /// <summary>
    /// Modifies the maximum health of the entity by a specified amount, optionally for a limited duration.
    /// </summary>
    /// <remarks>This method adjusts both the maximum health and the current health by the specified amount. 
    /// If a non-zero duration is provided, the maximum health will revert to its original value after the duration
    /// expires.</remarks>
    /// <param name="amount">The amount by which to increase the maximum health. Can be positive or negative.</param>
    /// <param name="duration">The duration, in seconds, for which the maximum health modification is applied.  If set to <see
    /// langword="0.0f"/>, the modification is permanent.</param>
    public void ModifyMaxHealth(float amount, float duration = 0.0f)
    {
        maxHealth += amount; // Increase the maximum health by the specified amount
        currentHealth += amount; // Adjust current health accordingly
        UpdateHealthBar(); // Update the health bar UI with the new health value
        ShowHealthBuff((int)amount, true);
        if (!Mathf.Approximately(duration, 0.0f)) {
            StartCoroutine(TempMaxHealth(amount, duration)); // Start a coroutine to restore the maximum health after the specified duration
        }
    }

    /// <summary>
    /// Modifies the health regeneration parameters based on the provided multipliers.
    /// </summary>
    /// <param name="regenRateMulti">The multiplier applied to the health regeneration rate. Must be positive.</param>
    /// <param name="damageDelayMulti">The multiplier applied to the delay after taking damage before health regeneration begins. Must be positive.</param>
    /// <param name="cooldoownMulti">The multiplier applied to the cooldown period between health regeneration cycles. Must be positive.</param>
    /// <returns>A <see cref="float3"/> containing the updated health regeneration parameters: <list type="bullet">
    /// <item><description>The cooldown period between health regeneration cycles.</description></item>
    /// <item><description>The delay after taking damage before health regeneration begins.</description></item>
    /// <item><description>The health regeneration rate.</description></item> </list></returns>
    public float3 ModifyHealthRegen(float _regenRateMulti, float _damageDelayMulti, float _cooldoownMulti)
    {
        healthRegenCooldown /= _cooldoownMulti;
        healthRegenDamageDelay /= _damageDelayMulti;
        healthRegenRate *= _regenRateMulti;
        return new float3(healthRegenCooldown, healthRegenDamageDelay, healthRegenRate); // Return the modified health regeneration parameters
    }

    /// <summary>
    /// Updates the health bar to reflect the current health value.
    /// </summary>
    /// <remarks>This method adjusts the fill amount of the health bar based on the ratio of 
    /// <c>currentHealth</c> to <c>maxHealth</c>. The fill amount is clamped between 0 and 1. Additionally, the width of the health bar
    /// is updated based on the ratio of <c>maxHealth</c> to <c>baseMaxHealth</c>.</remarks>
    public void UpdateHealthBar()
    {      
        // Update fill amount
        healthBar.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        if (healthBarText != null) {
            healthBarText.text = Mathf.FloorToInt(currentHealth).ToString() + " / " + Mathf.FloorToInt(maxHealth);
        }
    }

    public void SetHealth(float _health)
    {
        currentHealth = _health;
    }

    public void SetMaxHealth(float _health)
    {
        maxHealth = _health;
    }

    private void ShowHealthBuff(int amount, bool maxHealth)
    {
        if (floatingText != null) // Check if the floating text prefab is assigned  
        {
            Vector3 spawnPosition = new(transform.position.x + spawnLocation, transform.position.y + spawnLocation1, transform.position.z); // Calculate the spawn position for the floating text  
            GameObject text = Instantiate(floatingText, spawnPosition, Quaternion.identity); // Instantiate the floating text prefab at the player's position
            TextMeshPro textMesh = text.GetComponentInChildren<TextMeshPro>(); // Get the TextMeshPro component from the instantiated text prefab
            if (textMesh != null) // Use TryGetComponent correctly  
            {
                textMesh.text = amount > 0 ? $"+{amount}" : $"-{amount}"; // Format the message based on the amount
                SpriteRenderer floatingSprite = textMesh.GetComponentInChildren<SpriteRenderer>(); // Get the SpriteRenderer component from the instantiated text prefab
                if (floatingSprite != null) {
                    Sprite healthSprite;
                    if (maxHealth) {
                        healthSprite = Resources.Load<Sprite>("Sprites/Icons/MaxHealthIcon"); // Load the health sprite from Resources folder
                    } else {
                        healthSprite = Resources.Load<Sprite>("Sprites/Icons/HealthIcon"); // Load the health buff sprite from Resources folder
                    }
                    floatingSprite.sprite = healthSprite; // Assign the health sprite to the SpriteRenderer
                } else {
                    Debug.LogWarning("Floating text prefab does not have a TextMeshPro component attached.");
                }
            }
            Destroy(text, 1f); // Destroy the floating text after 1 second  
        }
    }

    /// <summary>
    /// Temporarily increases the maximum health of the entity for a specified duration.
    /// </summary>
    /// <remarks>After the specified duration, the maximum health is restored to its original value, and the
    /// current health is adjusted accordingly. This method is intended to be used in coroutine-based
    /// workflows.</remarks>
    /// <param name="amount">The amount by which to increase the maximum health. Must be positive.</param>
    /// <param name="duration">The duration, in seconds, for which the maximum health remains increased. Must be positive.</param>
    /// <returns>An enumerator that handles the timing of the temporary health increase.</returns>
    private IEnumerator TempMaxHealth(float amount, float duration)
    {
        yield return new WaitForSeconds(duration); // Wait for the specified duration
        ModifyMaxHealth(-amount); // Restore the maximum health back to its original value
        AdjustHealth(-amount, true); // Adjust current health accordingly
    }

}
