using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kayzie.Player
{
    public partial class PlayerControllerV2 : HealthFunctions
    {
        private void StartHealth()
        {
            animator = GetComponent<Animator>(); // Get the Animator component attached to the player
            if (animator == null) // Check if the animator is not found
            {
                Debug.LogWarning("Animator component is not assigned or found on the player object.");
            }

            currentHealth = maxHealth;
            GameObject healthParent = GameObject.FindGameObjectWithTag("HealthBarParent");
            healthBar = healthParent.GetComponentInChildren<Image>();
            if (healthBar == null) // Check if the health bar is not found
            {
                Debug.LogWarning("Health bar Image component is not assigned or found in the HealthBarParent.");
            }
            healthBarText = healthParent.GetComponentInChildren<TextMeshProUGUI>();
            iFrameTimer = iFrameDuration; // Set the invincibility timer to the initial duration
            healthRegenCooldownTimer = healthRegenCooldown; // Initialize the cooldown timer for health regeneration
            healthRegenDamageDelayTimer = healthRegenDamageDelay; // Initialize the damage delay timer for health regeneration
            baseHealthBarWidth = healthBar.rectTransform.sizeDelta.x; // Store the base width of the health bar
            baseHealthBarBackgroundWidth = healthBar.transform.parent.GetComponent<RectTransform>().sizeDelta.x; // Store the base width of the health bar background
            UpdateHealthBar();
        }

        private void UpdateHealth()
        {
            if (isInvincible) {
                iFrameTimer -= Time.deltaTime; // Decrease the invincibility timer
                if (iFrameTimer <= 0) {
                    isInvincible = false;
                    iFrameTimer = iFrameDuration; // Reset the timer to the initial duration
                    animator.SetBool("isHurt", false);
                }
            }
            HealthRegen();
        }
    }
}