using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerControllerV3: MonoBehaviour
{
    // Player Damage Settings
    [FoldoutGroup("Player Damage Settings")]
    [Tooltip("Strength of player damage when colliding with enemies or hazards.")]
    [SerializeField] private float playerDamageStrength = 10.0f; // Strength of player damage when colliding with enemies or hazards
    [FoldoutGroup("Player Damage Settings")]
    [Tooltip("Strength of the player's damage when attacking.")]
    [SerializeField] private float playerAttackDamageStrength = 5.0f;
    [FoldoutGroup("Player Damage Settings")]
    [Tooltip("Layermask used for player damage interactions.")]
    [SerializeField] private LayerMask playerDamageLayerMask; // Layer mask used for player damage interactions
    public float PlayerDamageStrength => playerDamageStrength; // Property to access the player damage strength
    [FoldoutGroup("Player Damage Settings")]
    [Tooltip("Cooldown time for the player's attack.")]
    [SerializeField] private float attackCooldown = 2f; // Cooldown time for the player's attack
    private float attackCooldownTimer = 0f; // Timer to track the cooldown for the player's attack

    [FoldoutGroup("Colliders")]
    [Tooltip("Collider used for player damage detection.")]
    [SerializeField] private BoxCollider2D damageCollider;

    private void DamageStart()
    {
        damageCollider.enabled = false; // Disable the damage collider initially
        if (playerDamageLayerMask == 0) {
            Debug.LogError("PlayerController: PlayerDamageLayerMask is not set. Please assign it in the inspector.");
        }
    }

    private void DamageUpdate()
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

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (damageCollider == null)
            Debug.LogError("Damage collider is not assigned in PlayerController. Please assign it in the inspector.");

        if (attackCooldownTimer > 0f) return; // Check if the attack is on cooldown
        attackCooldownTimer = attackCooldown; // Reset the cooldown timer

        StartCoroutine(AttackColliderTimer()); // Start the coroutine to handle the attack collider
        CheckForCollision();
    }

    private IEnumerator AttackColliderTimer()
    {
        damageCollider.enabled = true; // Enable the damage collider for the attack
        yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds to allow the attack to register
        damageCollider.enabled = false; // Disable the damage collider after the attack
    }

    private void CheckForCollision()
    {
        ContactFilter2D filter = new();
        filter.SetLayerMask(playerDamageLayerMask); // Set the layer mask for the contact filter
        filter.useTriggers = true; // Enable trigger detection for the contact filter
        Collider2D[] results = new Collider2D[10]; // Array to store detected colliders
        int count = damageCollider.Overlap(filter, results); // Check for collisions with the damage collider

        for (int i = 0; i < count; i++)
        {
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
