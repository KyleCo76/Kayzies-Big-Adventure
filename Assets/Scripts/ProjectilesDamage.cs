using UnityEngine;

public class ProjectilesDamage : MonoBehaviour
{

    [SerializeField] private float damage = 1f; // Damage dealt by the projectile

    public void SetDamage(float newDamage)
    {
        damage = newDamage; // Set the damage value for the projectile
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collided object has a Health component
        EnemyHealthManager enemyHealth = collision.GetComponent<EnemyHealthManager>();
        if (enemyHealth != null)
        {
            // Deal damage to the health component
            enemyHealth.TakeDamage(damage);
            // Destroy the projectile after dealing damage
            Destroy(gameObject);
        }
    }
}
