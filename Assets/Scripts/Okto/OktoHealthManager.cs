using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OktoHealthManager : HealthFunctions
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>(); // Get the Animator component attached to the player
        if (animator == null) // Check if the animator is not found
        {
            Debug.LogWarning("Animator component is not assigned or found on the player object.");
        }
        healthBar = GameObject.FindGameObjectWithTag("OktoHealthBar").GetComponent<Image>();
        healthRegenCooldownTimer = healthRegenCooldown;
        healthRegenDamageDelayTimer = healthRegenDamageDelay;
        currentHealth = maxHealth;
        iFrameTimer = iFrameDuration;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth < maxHealth) {
            HealthRegen();
        }
        if (isInvincible) {
            iFrameTimer -= Time.deltaTime; // Decrease the invincibility timer
            if (iFrameTimer <= 0f) {
                isInvincible = false;
                iFrameTimer = iFrameDuration; // Reset the timer to the initial duration
                animator.SetBool("isHurt", false);
            }
        }
    }

    /// <summary>
    /// Resets the "Hurt" state in the animator.
    /// </summary>
    /// <remarks>This method clears the "Hurt" trigger and sets the "Hurt" boolean parameter to <see
    /// langword="false"/>. It is typically used to reset the animation state after a hurt animation has been
    /// triggered.</remarks>
    public void ResetHurt()
    {
        animator.SetBool("Hurt", false);
        animator.ResetTrigger("Hurt");
    }
}
