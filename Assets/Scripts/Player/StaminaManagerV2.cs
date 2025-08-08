using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kayzie.Player
{
    public partial class PlayerControllerV2 : HealthFunctions
    {

        [FoldoutGroup("Stamina Settings"), SerializeField, Tooltip("Sprinting stamina decrease rate")]
        private float sprintStaminaDecreaseRate = 2f; // Rate at which stamina decreases while sprinting
        [FoldoutGroup("Stamina Settings"), SerializeField, Tooltip("Stamina regen rate")]
        private float staminaRegenRate = 1f; // Rate at which stamina regenerates when not sprinting
        [FoldoutGroup("Stamina Settings"), SerializeField, Tooltip("Stamina regen rate reduction while moving")]
        private float movingRegenMultiplier = 0.5f; // Multiplier for stamina regeneration rate when moving

        [FoldoutGroup("Prefabs"), SerializeField, Tooltip("Floating text prefab for stamina buffs")]
        //private GameObject floatingText; // Prefab for displaying floating text when stamina changes

        // Components for stamina management
        private Image staminaBar;
        private TextMeshProUGUI staminaText;

        // Stamina-related variables
        private float currentStamina = 100f;
        public float GetCurrentStamina => currentStamina; // Getter for current stamina
        private float maxStamina = 100f;
        public float GetMaxStamina => maxStamina; // Getter for maximum stamina


        private void AwakeStamina()
        {
            GameObject staminaObject = GameObject.FindWithTag("StaminaBar"); // Find the stamina manager object by tag
            staminaBar = staminaObject.GetComponent<Image>(); // Find the stamina bar UI element by tag
            if (staminaBar == null)
                Debug.LogError("Stamina bar not found in the scene. Please ensure it is tagged correctly.");
            GameObject textObject = GameObject.FindWithTag("StaminaText"); // Find the stamina text object by tag
            staminaText = textObject.GetComponent<TextMeshProUGUI>(); // Find the stamina text UI element by tag
            if (staminaText == null)
                Debug.LogError("Stamina text not found in the scene. Please ensure it is tagged correctly.");
        }

        /// <summary>
        /// Manages the player's stamina based on movement and sprinting status.
        /// </summary>
        /// <remarks>This method adjusts the player's stamina by decreasing it when sprinting or
        /// regenerating it when moving or idle. Stamina is clamped between 0 and the maximum stamina value to prevent
        /// it from exceeding these bounds. The method also updates the stamina bar UI to reflect the current stamina
        /// level.</remarks>
        /// <param name="isMoving">Indicates whether the player is currently moving. If <see langword="true"/>, stamina may decrease or
        /// regenerate based on sprinting status.</param>
        private void StaminaManager(bool isMoving)
        {
            if (isSprinting || (sprintingPressed && currentStamina >= maxStamina / 10 && isMoving)) {
                currentStamina -= Time.deltaTime * sprintStaminaDecreaseRate;
            } else if (currentStamina < maxStamina && !Mathf.Approximately(moveInput.x, 0f)) { // If walking and can regenerate stamina
                currentStamina += Time.deltaTime * staminaRegenRate * movingRegenMultiplier;
            } else if (currentStamina < maxStamina) {
                currentStamina += Time.deltaTime * staminaRegenRate;
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // Clamp stamina to ensure it doesn't exceed max or go below zero
            UpdateStaminaBar(); // Update the stamina bar UI with the current stamina value
        }

        /// <summary>
        /// Adjusts the player's current stamina by the specified amount.
        /// </summary>
        /// <remarks>The player's stamina is clamped between 0 and the maximum stamina value to ensure it  remains
        /// within valid bounds. This method also updates the stamina bar UI to reflect  the new stamina value.</remarks>
        /// <param name="staminaRestoreAmount">The amount to restore or reduce the player's stamina. Positive values increase stamina,  while negative values
        /// decrease it.</param>
        public void ChangeStamina(float staminaRestoreAmount)
        {
            currentStamina += staminaRestoreAmount; // Adjust the player's maximum stamina based on the multiplier
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // Ensure current stamina does not exceed max stamina
            UpdateStaminaBar(); // Update the stamina bar UI with the new stamina value
            Debug.Log($"Current Stamina: {currentStamina}"); // Log the current stamina for debugging
            ShowStaminaBuff((int)staminaRestoreAmount, false); // Show a stamina buff effect with the specified amount
        }

        /// <summary>
        /// Adjusts the player's stamina by a specified amount, either permanently or temporarily for a given duration.
        /// </summary>
        /// <remarks>If the duration is greater than 0, the stamina will revert to its original value after the
        /// specified duration. The method ensures that the player's stamina remains within the valid range, clamping it
        /// between 0 and the maximum stamina value.</remarks>
        /// <param name="staminaRestoreAmount">The amount of stamina to restore. Can be positive or negative.</param>
        /// <param name="duration">The duration, in seconds, for which the stamina change is applied.  If set to a value less than or equal to 0,
        /// the change is applied permanently.</param>
        public void ChangeStamina(float staminaRestoreAmount, float duration)
        {
            if (duration <= 0.1f) // Check if the duration is valid
            {
                ChangeStamina(staminaRestoreAmount); // If duration is not specified, apply the change permanently
                return; // Exit if permanent change is desired
            }
            float originalStaminaRate = currentStamina; // Store the original stamina before changes
            currentStamina += staminaRestoreAmount; // Adjust the player's stamina based on the restore amount
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // Ensure current stamina does not exceed max stamina
            UpdateStaminaBar(); // Update the stamina bar UI with the new stamina value
            ShowStaminaBuff((int)staminaRestoreAmount, false); // Show a stamina buff effect with the specified amount
            StartCoroutine(TempStat(originalStaminaRate, duration, v => currentStamina = v)); // Start a coroutine to restore the stamina after the specified duration
        }

        /// <summary>
        /// Adjusts the player's maximum stamina by applying a multiplier and updates the current stamina accordingly.
        /// </summary>
        /// <remarks>This method recalculates the player's current stamina based on the new maximum stamina and
        /// ensures that the current stamina remains within valid bounds. It also updates the stamina bar UI to reflect the
        /// changes.</remarks>
        /// <param name="maxStaminaMultiplier">The multiplier to apply to the player's maximum stamina. Must be a positive value.</param>
        public void ChangeMaxStamina(float maxStaminaBoost)
        {
            maxStamina += maxStaminaBoost; // Adjust the player's maximum stamina based on the multiplier
            currentStamina += maxStaminaBoost;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // Ensure current stamina does not exceed max stamina
            UpdateStaminaBar(); // Update the stamina bar UI with the new maximum stamina value
            ShowStaminaBuff((int)maxStaminaBoost, true); // Show a stamina buff effect with the specified amount
        }

        /// <summary>
        /// Temporarily or permanently modifies the player's maximum stamina based on a multiplier.
        /// </summary>
        /// <remarks>If a positive duration is specified, the player's maximum stamina and current stamina are
        /// adjusted for the given duration,  after which the original stamina values are restored. If the duration is 0 or
        /// less, the change is applied permanently.</remarks>
        /// <param name="maxStaminaMultiplier">The multiplier to apply to the player's maximum stamina. Must be greater than 0.</param>
        /// <param name="duration">The duration, in seconds, for which the stamina change should be applied. If set to 0 or less, the change is
        /// applied permanently.</param>
        public void ChangeMaxStamina(float maxStaminaAmount, float duration)
        {
            if (duration <= 0.1f) // Check if the duration is valid
            {
                ChangeMaxStamina(maxStaminaAmount); // If duration is not specified, apply the multiplier permanently
                return; // Exit if permanent change is desired
            }
            ShowStaminaBuff((int)maxStaminaAmount, true);
            float originalStamina = currentStamina; // Store the original stamina before changes
            maxStamina *= maxStaminaAmount; // Adjust the player's maximum stamina based on the multiplier
            currentStamina *= maxStaminaAmount; // Adjust the current stamina based on the multiplier
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // Ensure current stamina does not exceed max stamina
            UpdateStaminaBar(); // Update the stamina bar UI with the new maximum stamina value
            StartCoroutine(TempStat(originalStamina, duration, v => currentStamina = v)); // Start a coroutine to restore the stamina after the specified duration
        }

        /// <summary>
        /// Adjusts the player's stamina regeneration rate by applying a multiplier.
        /// </summary>
        /// <remarks>This method modifies the stamina regeneration rate by multiplying the current rate with the
        /// specified multiplier. Ensure that the multiplier is greater than zero to avoid unintended behavior.</remarks>
        /// <param name="staminaRegenRateMultiplier">The multiplier to apply to the current stamina regeneration rate. Must be a positive value.</param>
        public void ChangeStaminaRegenRate(float staminaRegenRateMultiplier)
        {
            staminaRegenRate *= staminaRegenRateMultiplier; // Adjust the player's stamina regeneration rate based on the multiplier
        }

        /// <summary>
        /// Temporarily or permanently modifies the player's stamina regeneration rate.
        /// </summary>
        /// <remarks>If a positive <paramref name="duration"/> is specified, the stamina regeneration rate will
        /// revert  to its original value after the duration elapses. If <paramref name="duration"/> is less than or  equal
        /// to 0, the change is applied permanently.</remarks>
        /// <param name="staminaRegenRateMultiplier">The multiplier to apply to the current stamina regeneration rate. Must be greater than 0.</param>
        /// <param name="duration">The duration, in seconds, for which the modified stamina regeneration rate should be applied. If set to a value
        /// less than or equal to 0, the change is applied permanently.</param>
        public void ChangeStaminaRegenRate(float staminaRegenRateMultiplier, float duration)
        {
            if (duration <= 0) // Check if the duration is valid
            {
                ChangeStaminaRegenRate(staminaRegenRateMultiplier); // If duration is not specified, apply the multiplier permanently
                return; // Exit if permanent change is desired
            }
            float originalStaminaRegenRate = staminaRegenRate; // Store the original stamina regeneration rate before changes
            staminaRegenRate *= staminaRegenRateMultiplier; // Adjust the player's stamina regeneration rate based on the multiplier
            StartCoroutine(TempStat(originalStaminaRegenRate, duration, v => staminaRegenRate = v)); // Start a coroutine to restore the stamina regeneration rate after the specified duration
        }

        /// <summary>
        /// Adjusts the player's stamina consumption rate by applying a multiplier.
        /// </summary>
        /// <remarks>This method modifies the rate at which stamina decreases during gameplay.  Use this to
        /// dynamically adjust stamina consumption based on game conditions, such as difficulty level or player
        /// actions.</remarks>
        /// <param name="staminaConsumptionRateMultiplier">The multiplier to apply to the current stamina consumption rate. Must be a positive value.</param>
        public void ChangeStaminaConsumptionRate(float staminaConsumptionRateMultiplier)
        {
            sprintStaminaDecreaseRate *= staminaConsumptionRateMultiplier; // Adjust the player's stamina consumption rate based on the multiplier
        }

        /// <summary>
        /// Temporarily or permanently modifies the stamina consumption rate by applying a multiplier.
        /// </summary>
        /// <remarks>If a positive duration is specified, the stamina consumption rate will revert to its original
        /// value  after the duration elapses. If the duration is less than or equal to 0, the change is applied
        /// permanently.</remarks>
        /// <param name="staminaConsumptionRateMultiplier">The multiplier to apply to the current stamina consumption rate. Must be a positive value.</param>
        /// <param name="duration">The duration, in seconds, for which the modified stamina consumption rate should be applied.  If the value is
        /// less than or equal to 0, the change is applied permanently.</param>
        public void ChangeStaminaConsumptionRate(float staminaConsumptionRateMultiplier, float duration)
        {
            if (duration <= 0) // Check if the duration is valid
            {
                ChangeStaminaConsumptionRate(staminaConsumptionRateMultiplier); // If duration is not specified, apply the multiplier permanently
                return; // Exit if permanent change is desired
            }
            float originalStaminaConsumptionRate = sprintStaminaDecreaseRate; // Store the original stamina consumption rate before changes
            sprintStaminaDecreaseRate *= staminaConsumptionRateMultiplier; // Adjust the player's stamina consumption rate based on the multiplier
            StartCoroutine(TempStat(originalStaminaConsumptionRate, duration, v => sprintStaminaDecreaseRate = v)); // Start a coroutine to restore the stamina consumption rate after the specified duration
        }

        public void SetStamina(float _stamina)
        {
            currentStamina = _stamina;
        }

        public void SetMaxStamina(float _stamina)
        {
            maxStamina = _stamina;
        }

        /// <summary>
        /// Updates the stamina bar UI to reflect the player's current stamina as a percentage of their maximum stamina.
        /// </summary>
        /// <remarks>This method calculates the current stamina percentage, clamps the value between 0 and 1,  and
        /// updates the UI using the <see cref="UIHandler.SetStaminaValue(float)"/> method.</remarks>
        private void UpdateStaminaBar()
        {
            staminaBar.fillAmount = Mathf.Clamp01(currentStamina / maxStamina); // Update the stamina bar UI with the current stamina value
            if (staminaText != null) {
                staminaText.text = Mathf.FloorToInt(currentStamina).ToString() + " / " + Mathf.FloorToInt(maxStamina); // Update the stamina text UI with the current stamina value
            }
        }

        private void ShowStaminaBuff(int amount, bool maxStamina)
        {
            Debug.Log($"Showing stamina buff: {amount}"); // Log the stamina buff amount for debugging
            if (floatingText != null) // Check if the floating text prefab is assigned  
            {
                Vector3 spawnPosition = new(transform.position.x + 0.0f, transform.position.y + 1.0f, transform.position.z); // Calculate the spawn position for the floating text  
                GameObject text = Instantiate(floatingText, spawnPosition, Quaternion.identity); // Instantiate the floating text prefab at the player's position
                TextMeshPro textMesh = text.GetComponentInChildren<TextMeshPro>(); // Get the TextMeshPro component from the instantiated text prefab
                if (textMesh != null) // Use TryGetComponent correctly  
                {
                    textMesh.text = amount > 0 ? $"+{amount}" : $"-{amount}"; // Format the message based on the amount
                    textMesh.color = Color.blue; // Set the color of the floating text to blue
                } else {
                    Debug.LogWarning("Floating text prefab does not have a TextMeshPro component attached.");
                }
                SpriteRenderer textSprite = textMesh.GetComponentInChildren<SpriteRenderer>(); // Get the SpriteRenderer component from the TextMeshPro object
                if (textSprite != null) // Check if the SpriteRenderer component exists  
                {
                    Sprite staminaSprite;
                    if (maxStamina) // Check if the buff is for maximum stamina
                        staminaSprite = Resources.Load<Sprite>("Sprites/Icons/MaxStaminaIcon"); // Load the max stamina icon sprite from resources
                    else
                        staminaSprite = Resources.Load<Sprite>("Sprites/Icons/StaminaIcon"); // Load the stamina icon sprite from resources
                    textSprite.sprite = staminaSprite; // Set the sprite of the floating text to the stamina icon
                } else {
                    Debug.LogWarning("Floating text prefab does not have a SpriteRenderer component attached.");
                }
                Destroy(text, 1f); // Destroy the floating text after 1 second  
            }
        }

        /// <summary>
        /// Temporarily modifies a value for a specified duration and restores it afterward.
        /// </summary>
        /// <remarks>This method is designed to be used in a coroutine to temporarily modify a value and restore
        /// it after a specified duration. The <paramref name="setValue"/> delegate is invoked to restore the original value
        /// once the duration has elapsed.</remarks>
        /// <param name="originalValue">The original value to be restored after the duration.</param>
        /// <param name="duration">The duration, in seconds, for which the value remains modified.</param>
        /// <param name="setValue">A delegate used to set the value during and after the duration.</param>
        /// <returns>An enumerator that can be used to control the timing of the value restoration.</returns>
        private IEnumerator TempStat(float originalValue, float duration, System.Action<float> setValue)
        {
            yield return new WaitForSeconds(duration);
            setValue(originalValue); // Restore the value using the delegate
        }

    }
}