using System.Collections;
using UnityEngine;

public class OktoShooter : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float damage = 1f; // Damage dealt by the projectile
    [SerializeField] private float shootFlipTime = 0.2f; // Time to flip sprite for shooting

    private float nextFireTime;

    void Update()
    {
        // Check for enemies and fire automatically, or on button press
        if (Time.time > nextFireTime) {
            FireAtClosestEnemy();
        }
    }

    void FireAtClosestEnemy()
    {
        // Get all colliders within detection radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        // Find the closest enemy
        foreach (Collider2D collider in hitColliders) {
            if (collider.CompareTag("Enemy")) {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEnemy = collider.transform;
                }
            }
        }

        // Fire at the closest enemy if found
        if (closestEnemy != null) {
            // Calculate direction to enemy
            Vector2 direction = (closestEnemy.position - transform.position).normalized;
            
            // Ensure the OktoController is present to flip the sprite
            if (TryGetComponent<OktoController>(out var oktoController)) {
                oktoController.FlipSprite(direction.x); // Flip the sprite to face the enemy
                oktoController.FlipForShooting = true;
                StartCoroutine(FireFlipRelease()); // Start coroutine to reset flip state after firing
                if (oktoController.DidShoot) {
                    StopCoroutine(DidShootRelease()); // Stop any previous coroutine to reset shooting state
                }
                oktoController.DidShoot = true; // Set the shooting state
                StartCoroutine(DidShootRelease()); // Start coroutine to reset shooting state after cooldown
            }

            // Calculate the angle to rotate the projectile
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                                                      // Create and fire projectile
            GameObject projectile = Instantiate(projectilePrefab, transform.position, rotation);
            if (projectile.TryGetComponent<ProjectilesDamage>(out var projectileDamage)) {
                projectileDamage.SetDamage(damage); // Set the damage value for the projectile
            } else {
                Debug.LogWarning("ProjectilesDamage component not found on projectile prefab.");
            }

            if (projectile.TryGetComponent<Rigidbody2D>(out var rb)) {
                rb.linearVelocity = direction * projectileSpeed;
            } else {
                Debug.LogWarning("Rigidbody2D component not found on projectile prefab.");
            }

            // Set cooldown
            nextFireTime = Time.time + cooldown;
        }
    }

    private IEnumerator DidShootRelease()
    {
        //Wait for the cooldown duration to see if we are still shooting
        yield return new WaitForSeconds(cooldown + 1.0f);
        
        // Reset the shooting state
        if (TryGetComponent<OktoController>(out var oktoController)) {
            oktoController.DidShoot = false;
        }
    }
    private IEnumerator FireFlipRelease()
    {
        // Wait for the cooldown duration
        yield return new WaitForSeconds(shootFlipTime);
        
        // Reset the flip state after firing
        if (TryGetComponent<OktoController>(out var oktoController)) {
            oktoController.FlipForShooting = false;
        }
    }

    // Draw the detection radius in the editor for visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
