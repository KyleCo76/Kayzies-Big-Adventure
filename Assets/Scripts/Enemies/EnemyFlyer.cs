using Kayzie.Player;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyFlyer : FlyerFunctions
{

    private void Start()
    {
        followDistance = followActivation;
    }

    protected override void Awake()
    {
        base.Awake(); // Call the base class Awake method to initialize common properties
        lastPosition = enemyBody.position; // Initialize last position for movement calculations
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // Call the base class FixedUpdate to handle common functionality

        if (movingToIdle) {
            MoveToIdle(); // Move the enemy up to the ground
        }

        if (TryFollowPlayer(out followDistance, isFollowing, followActivation, followBoostMulti, playerTransform, isDamaged, tempFollowDistance)) {
            FollowPlayer(); // Follow the player if within follow distance and exit early
            return;
        } else if (isFollowing) {
            StopFollowingPlayer(); // If not within follow distance, stop following the player
        }

        // If not following the player, check if we can wander or idle
        if (canWander) {
            Wander(); // Wander if allowed
        } else if (!isIdle && hasIdle) {
            Idle(); // If not wandering, check if we can idle and Idle if possible
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the enemy collides with the player
        if (collision.gameObject.CompareTag("Player")) {
            enemyAnimator.SetTrigger("Attack"); // Trigger the attack animation
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerControllerV2 playerHealth = collision.gameObject.GetComponent<PlayerControllerV2>();

        if (playerHealth != null && !playerHealth.IsInvincible) {
            enemyAnimator.SetTrigger("Attack"); // Trigger the attack animation
        }
    }
}
