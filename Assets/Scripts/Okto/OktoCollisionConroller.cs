using UnityEngine;

public class OktoCollisionController : MonoBehaviour
{
    private OktoHealthManager oktoHealthManager; // Reference to the player's health manager

    // Start is called before the first frame update
    private void Start()
    {
        // Get the PlayerHealthManager component attached to this GameObject
        oktoHealthManager = GetComponent<OktoHealthManager>();
        if (oktoHealthManager == null) {
            Debug.LogWarning("PlayerHealthManager component not found on the player object.");
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // Check if the collided object has a tag "Enemy"
        if (other.gameObject.CompareTag("Enemy") && oktoHealthManager != null && !oktoHealthManager.IsInvincible) {
            ChangeHealth(other); // Call the method to change health when colliding with an enemy
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy") && oktoHealthManager != null && !oktoHealthManager.IsInvincible) {
            ChangeHealth(other); // Call the method to change health when staying in contact with an enemy
        }
    }

    private void ChangeHealth(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<IDamageDealer>(out var damageDealer)) {
            oktoHealthManager.AdjustHealth(-damageDealer.DamageAmount); // Apply damage to the player
        } else {
            Debug.LogWarning("No valid IDamageDealer component found on the collided object.");
        }
        //if (other.gameObject.TryGetComponent<EnemyWalker>(out var enemyWalker)) {
        //    oktoHealthManager.AdjustHealth(-enemyWalker.DamageAmount); // Apply damage to the player
        //} else if (other.gameObject.TryGetComponent<EnemyFlyer>(out var enemyFlyer)) {
        //    oktoHealthManager.AdjustHealth(-enemyFlyer.DamageAmount);// Apply damage to the player
        //} else {
        //    Debug.LogWarning("No valid enemy component found on the collided object.");
        //}
    }
}
