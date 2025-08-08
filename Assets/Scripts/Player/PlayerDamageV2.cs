using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kayzie.Player
{
    public partial class PlayerControllerV2 : HealthFunctions
    {
        // Player Damage Settings
        [FoldoutGroup("Player Damage Settings"), SerializeField, Tooltip("Strength of player damage when colliding with enemies or hazards.")]
        private float playerDamageStrength = 10.0f; // Strength of player damage when colliding with enemies or hazards
        [FoldoutGroup("Player Damage Settings"), SerializeField, Tooltip("Strength of the player's damage when attacking.")]
        private float playerAttackDamageStrength = 5.0f;
        [FoldoutGroup("Player Damage Settings"), SerializeField, Tooltip("Layermask used for player damage interactions.")]
        private LayerMask playerDamageLayerMask; // Layer mask used for player damage interactions
        [FoldoutGroup("Player Damage Settings"), SerializeField, Tooltip("Cooldown time for the player's attack.")]
        private float attackCooldown = 2f; // Cooldown time for the player's attack

        //[FoldoutGroup("Colliders"), SerializeField, Tooltip("Collider used for player damage detection.")]
        //private BoxCollider2D damageCollider;

        public float PlayerDamageStrength => playerDamageStrength; // Property to access the player damage strength
        private float attackCooldownTimer = 0f; // Timer to track the cooldown for the player's attack


        private void StartDamage()
        {
            attackCollider.enabled = false; // Disable the damage collider initially
            if (playerDamageLayerMask == 0) {
                Debug.LogError("PlayerController: PlayerDamageLayerMask is not set. Please assign it in the inspector.");
            }
        }

        private void UpdateDamage()
        {
            if (attackCooldownTimer > 0f) {
                attackCooldownTimer -= Time.deltaTime; // Decrease the cooldown timer
            }
            if (attackCooldownTimer < 0f) {
                attackCooldownTimer = 0f; // Ensure the cooldown timer does not go below zero
            }
        }

        /// <summary>
        /// Performs a double jump, applying an upward force to the player if certain conditions are met.
        /// </summary>
        /// <remarks>This method allows the player to perform a second jump while airborne, provided the player is
        /// not falling. The force applied depends on the player's current vertical velocity. If the player is moving
        /// upwards, a reduced force is applied; otherwise, the force is adjusted based on the player's downward
        /// velocity.</remarks>
        public void ChangeDamage(float damageMultiplier)
        {
            playerDamageStrength *= damageMultiplier; // Adjust the player's damage strength based on the multiplier
        }

        /// <summary>
        /// Temporarily modifies the player's damage strength and jump force for a specified duration.
        /// </summary>
        /// <remarks>This method adjusts the player's damage strength by the specified multiplier and temporarily
        /// modifies the jump force. After the specified duration, the jump force is restored to its original
        /// value.</remarks>
        /// <param name="damageMultiplier">The multiplier applied to the player's damage strength. Must be greater than 0.</param>
        /// <param name="duration">The duration, in seconds, for which the changes remain active. Must be greater than 0.</param>
        public void ChangeDamage(float damageMultiplier, float duration)
        {
            if (duration <= 0) {
                ChangeDamage(damageMultiplier); // If duration is not specified, apply the multiplier permanently
                return; // Exit if permanent change is desired
            }
            float originalDamageAmount = PlayerDamageStrength; // Store the original jump force before changes
            playerDamageStrength *= damageMultiplier; // Adjust the player's damage strength based on the multiplier
            StartCoroutine(TempStat(originalDamageAmount, duration, v => playerDamageStrength = v)); // Start a coroutine to restore the jump force after the specified duration
        }

        private void OnAttack()
        {
            if (attackCollider == null)
                Debug.LogError("Damage collider is not assigned in PlayerController. Please assign it in the inspector.");

            if (attackCooldownTimer > 0f) return; // Check if the attack is on cooldown
            attackCooldownTimer = attackCooldown; // Reset the cooldown timer

            StartCoroutine(AttackColliderTimer()); // Start the coroutine to handle the attack collider
            CheckForCollision();
        }

        private IEnumerator AttackColliderTimer()
        {
            attackCollider.enabled = true; // Enable the damage collider for the attack
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds to allow the attack to register
            attackCollider.enabled = false; // Disable the damage collider after the attack
        }

        private void CheckForCollision()
        {
            ContactFilter2D filter = new();
            filter.SetLayerMask(playerDamageLayerMask); // Set the layer mask for the contact filter
            filter.useTriggers = true; // Enable trigger detection for the contact filter
            Collider2D[] results = new Collider2D[10]; // Array to store detected colliders
            int count = attackCollider.Overlap(filter, results); // Check for collisions with the damage collider

            for (int i = 0; i < count; i++) {
                Collider2D hitCollider = results[i]; // Get the collider that was hit
                if (hitCollider != null && hitCollider.CompareTag("Enemy")) {
                    // Check if the hit collider has a health manager and apply damage
                    if (hitCollider.TryGetComponent<EnemyHealthManager>(out var enemyHealth)) {
                        enemyHealth.TakeDamage(playerAttackDamageStrength); // Apply damage to the health manager
                    }
                }
            }
        }

    }
}
