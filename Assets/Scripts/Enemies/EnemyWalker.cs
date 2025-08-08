using Kayzie.Player;
using UnityEngine;

public class EnemyWalker : EnemyBase
{

    [SerializeField] private float jumpForce = 5f; // Force applied when the enemy jumps
    public float JumpForce => jumpForce; // Public property to access jump force


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
            ChangeAudio(enemyAudioAttack);
        }
    }
}
