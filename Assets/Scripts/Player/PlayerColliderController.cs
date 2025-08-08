using UnityEngine;

namespace Kayzie.Player
{
    [RequireComponent(typeof(PlayerControllerV2))]
    public class PlayerColliderController : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 5f; // Force applied when bouncing off an enemy
        private float collectableCooldown = 0.5f; // Cooldown time for collecting objects
        private PlayerControllerV2 playerController; // Reference to the player's controller


        // Start is called before the first frame update
        private void Start()
        {
            if (!TryGetComponent<PlayerControllerV2>(out playerController)) {
                Debug.LogError("PlayerControler component not found on the player object.");
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            CollisionDetection(other, true); // Call the method to handle collision detection when entering contact with an object
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            CollisionDetection(other, true); // Call the method to handle collision detection when staying in contact with an object
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            CollisionDetection(other, false); // Call the method to handle collision detection when exiting contact with an object
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CollisionDetection(other, false, true); // Call the method to handle collision detection   
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            CollisionDetection(other); // Call the method to handle collision detection when staying in contact with an object
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            CollisionDetection(other, true); // Call the method to handle collision detection when exiting contact with an object
        }
        private void Update()
        {
            if (!Mathf.Approximately(collectableCooldown, 0.0f)) {
                collectableCooldown -= Time.deltaTime; // Decrease the cooldown time
            }
        }

        /// <summary>
        /// Triggers collision detection based on the type of collider.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="exit"></param>
        /// <param name="enter"></param>
        private void CollisionDetection(Collider2D other, bool exit = false, bool enter = false)
        {
                switch (other.gameObject.tag) {
                case "Climbable":
                    playerController.CanClimb = !exit; // Allow climbing if the object is climbable
                    if (exit) {
                        playerController.CannotClimb(); // Disable climbing when exiting the climbable object
                    }
                    break;
                case "Collectables":
                    CollectObject(other, enter); // Call the method to collect the object
                    break;
                case "Chest":
                    if (playerController.IsInteracting) {
                        other.GetComponent<ChestController>().OpenChest(); // Open the chest if the player is interacting
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Non-Trigger collision detection method.
        /// </summary>
        /// <param name="other"></param>
        private void CollisionDetection(Collision2D other, bool enter)
        {
            //Collider2D myCollider = other.otherCollider; // Get the player's collider from the collision

            //if (myCollider is CapsuleCollider2D capsule) {
            //    if (capsule.isTrigger) return; // Do not process collisions for trigger colliders
            //}
            switch (other.gameObject.tag) {
                case "Enemy":
                    if (!playerController.IsInvincible)
                        ChangeHealth(other); // Call the method to change health when staying in contact with an enemy
                    break;
                case "Climbable":
                    playerController.CanClimb = enter;
                    break;
                case "MovingPlatforms":
                case "Platforms":
                    playerController.onPlatform = enter;
                    if (enter) {
                        playerController.platformCollider = other.collider;
                        if (other.gameObject.CompareTag("MovingPlatforms"))
                            transform.SetParent(other.transform); // Set the player as a child of the platform to follow its movement
                    } else {
                        playerController.platformCollider = null; // Reset the platform collider when exiting
                        if (other.gameObject.CompareTag("MovingPlatforms"))
                            transform.SetParent(null); // Remove the player from the platform's hierarchy
                    }
                    break;
                default:
                    break;
            }
        }

        private void ChangeHealth(Collision2D other)
        {
            if (IsCollisionFromTop(other)) { // Check if the collision is from above
                if (playerController.IsInvincible) { return; } // Prevent further actions if the player is already hurt

                other.gameObject.GetComponent<EnemyHealthManager>()
                    .TakeDamage(playerController.PlayerDamageStrength); // Apply damage to the enemy
                if (TryGetComponent<Rigidbody2D>(out var playerBody)) {
                    playerBody.AddForce(new Vector2(0, bounceForce), ForceMode2D.Impulse); // Bounce the player upwards
                }
            } else {
                if (other.gameObject.TryGetComponent<IDamageDealer>(out var damageDealer)) {
                    playerController.AdjustHealth(-damageDealer.DamageAmount); // Apply damage to the player
                } else {
                    Debug.LogWarning("No valid IDamageDealer component found on the collided object.");
                }
            }
        }

        private bool IsCollisionFromTop(Collision2D collision, float minDownDot = 0.5f)
        {
            // minDownDot: 1.0 = perfectly down, 0 = perpendicular, -1 = perfectly up
            foreach (var contact in collision.contacts) {
                // The normal points away from the surface of B, toward A
                float dot = Vector2.Dot(contact.normal, Vector2.up);
                if (dot > minDownDot) {
                    return true; // Collision is from above (A is above B)
                }
            }
            return false;
        }

        private void CollectObject(Collider2D other, bool isEntering)
        {
            if (!other.TryGetComponent<CollectableStats>(out var stats)) {
                Debug.LogWarning("No CollectableStats component found on the collectable " + gameObject.name);
                return; // Quit if the collectable stats are not found
            }

            // Check if the collectable should be collected on collision and if so, bypass the cooldown
            if (!stats.CollectOnCollision) {
                // If the player is not interacting or the collectable cooldown is active, do not collect the object
                if (!playerController.IsInteracting || collectableCooldown > 0.0f) {
                    return;
                }
                collectableCooldown = 0.5f; // Reset the cooldown time for collecting objects
            }
            // If the collectable is set to collect on collision, prevent onStay collision collection and only allow collection if canCollect is true
            else if ((stats.CollectOnCollision && !isEntering) || (stats.CollectOnCollision && !stats.CanCollect)) {
                return;
            }

            CollectableData data = other.GetComponent<CollectableStats>().GetCollectableData(); // Get the collectable data from the CollectableManager
            Destroy(other.gameObject); // Destroy the collectable object after collection
            gameObject.GetComponent<CollectableManager>().Collect(data); // Call the Collect method to handle the collected data
        }
    }
}
