using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyBase : EnemyCore, IDamageDealer
{

    [FoldoutGroup("Movement Settings")]
    [Tooltip("Speed at which the enemy moves when not following the player")]
    [SerializeField] private float moveSpeed = 3f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Distance within which the enemy starts following the player")]
    [SerializeField] private float followActivation = 10f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Speed at which the enemy follows the player")]
    [SerializeField] private float followSpeed = 5f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Multiplier for the follow distance when the enemy begins following the player")]
    [SerializeField] private float followBoostMulti = 1.5f;
    private float followRange;

    [Tooltip("Amount of damage the enemy deals to the player")]
    [SerializeField] private float damageAmount = 10f;
    public float DamageAmount => damageAmount;

    [Tooltip("Reference to the player GameObject")]
    [SerializeField] private GameObject player;


    protected override void Awake()
    {
        base.Awake();
        if (isSleeping) return; // Exit if the enemy is set to sleep

        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player"); // Find the player GameObject by tag
            if (player == null) {
                Debug.LogError("Player GameObject not found. Make sure it has the 'Player' tag.");
            }
        }

        if (followSpeed == 0f) {
            followSpeed = moveSpeed; // Set follow speed to move speed if not specified
        }
        followRange = followActivation; // Initialize follow distance
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // Call the base class FixedUpdate to handle common functionality

        bool followPlayer = TryFollowPlayer(out followRange, isFollowing, followActivation, followBoostMulti, player.transform, isDamaged, tempFollowDistance);
        if (followPlayer) {
            FollowPlayer(); // Call the method to follow the player
        } else {
            StopFollowingPlayer(out isFollowing, ref tempFollowDistance, ref isDamaged, wanderTarget.x, followRange);
            if (canWander) {
                Wander(); // Call the method to wander randomly
            } else {
                return; // Exit to prevent playing walk sound when not wandering or following
            }
        }
        //PlayWalkSound(followPlayer); // Play the walking sound if available
    }

    protected void FollowPlayer()
    {
        isFollowing = true;
        Vector2 targetPosition = transform.position; // Get the current position of the enemy

        // Move towards the player's x position only
        float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
        Vector2 playerDirection = (player.transform.position - transform.position).normalized;

        //Check if we are following the player more vertically than horizontally
        if (Mathf.Abs(playerDirection.x) < Mathf.Abs(playerDirection.y)) {
            return; // Do not follow vertically, only horizontally
        }
        targetPosition.x += direction * followSpeed * Time.deltaTime;
        enemyBody.MovePosition(targetPosition);
        if (direction < 0 && !isFlipped) {
            FlipSprite(); // Flip the sprite to face left
        } else if (direction > 0 && isFlipped) {
            FlipSprite(); // Flip the sprite to face right
        }
    }

    protected void Wander()
    {
        // Wander within the specified range
        if (Vector2.Distance(transform.position, wanderTarget) < 0.4f) {
            // Flip the wander target to the opposite side when close enough
            wanderTarget = isFlipped ? wanderTargetRight : wanderTargetLeft;
            FlipSprite(); // Flip the sprite to face the new direction
        }
        if (isFollowing) {
            isFollowing = false; // Reset the following flag when wandering
            float pointDirection = Mathf.Sign(wanderTarget.x - transform.position.x);
            if (pointDirection < 0 && !isFlipped) {
                FlipSprite(); // Flip the sprite to face left
            } else if (pointDirection > 0 && isFlipped) {
                FlipSprite(); // Flip the sprite to face right
            }
        }
        // Move towards the wander target
        Vector2 moveDir = (wanderTarget - (Vector2)transform.position).normalized;
        enemyBody.MovePosition((Vector2)transform.position + moveSpeed * Time.deltaTime * moveDir);
        if (CheckStuck()) {
            ResetTargetForObstacle();
        }
    }
}
