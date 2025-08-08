using UnityEngine;

public class Teleporter : MonoBehaviour
{

    [SerializeField] private Transform connectedMine;
    [Tooltip("Whether followers should spawn to the right of the player or not when spawning at this object")]
    [SerializeField] private bool followerSpawnToRight = true; // Determines if followers spawn to the right of the player
    [SerializeField] private float jumpModifier = 0.0f; // Jump modifier the teleported player will have

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (connectedMine == null) {
            Debug.LogError("Connected mine not assigned in the inspector!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TeleportIfCliming(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TeleportIfCliming(other);
    }

    private void TeleportIfCliming(Collider2D other)
    {
        Kayzie.Player.PlayerControllerV2 playerController = other.gameObject.GetComponent<Kayzie.Player.PlayerControllerV2>();
        if (other.gameObject.CompareTag("Player") && playerController != null && playerController.IsClimbing && playerController.CanTeleport) {
            // Teleport the player to the mine entrance
            other.transform.position = new(connectedMine.position.x, connectedMine.position.y, 0.0f);
            other.attachedRigidbody.linearVelocity = Vector2.zero; // Reset the player's velocity to prevent unwanted movement
            Debug.Log("Player teleported to the mine entrance.");
            playerController.Teleported();
            playerController.JumpForce += jumpModifier; // Apply the jump modifier to the player

            foreach (Transform follower in playerController.Followers) {
                if (follower != null) {
                    Vector3 spawnOffset = followerSpawnToRight ? new Vector3(1.0f, 1.0f, 0.0f) : new Vector3(-1.0f, 1.0f, 0.0f);
                    follower.position = new(connectedMine.position.x + spawnOffset.x, connectedMine.position.y + spawnOffset.y, spawnOffset.z);
                    follower.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; // Reset the follower's velocity
                }
            }
        }
    }
}
