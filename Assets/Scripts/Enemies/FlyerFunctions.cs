using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

public class FlyerFunctions : EnemyCore, IDamageDealer
{
    [Space]
    [SerializeField] protected float damageAmount = 10f; // Amount of damage the enemy deals to the player
    // Implement IDamageDealer interface to provide damage amount
    public float DamageAmount => damageAmount; // Readonly property for IDamageDealer interface
    [Space]
    [FoldoutGroup("Movement Settings")]
    [SerializeField] protected float moveSpeed = 2f;
    [FoldoutGroup("Movement Settings")]
    [SerializeField] protected float maxWanderSpeed = 5f; // Maximum speed for wandering
    [FoldoutGroup("Movement Settings")]
    [SerializeField] protected float maxSpeed = 10f; // Maximum speed the enemy can reach
    [Space]
    [FoldoutGroup("Follow Settings")]
    [SerializeField] protected float followActivation = 4f; // Distance within which the enemy starts following the player
    [FoldoutGroup("Follow Settings")]
    [SerializeField] protected float followBoostMulti = 2f; // Boost to followDistance while following player
    [FoldoutGroup("Follow Settings")]
    [SerializeField] protected bool hasFollowAnimation = false;
    protected float followDistance;
    [Space]
    [FoldoutGroup("Ground Check Settings")]
    [SerializeField] protected LayerMask groundLayer; // Assign your ground layer(s) in the Inspector
    private readonly float checkDistance = 20f; // Max distance to check for ground above
    private readonly float stopOffset = 0.01f; // Small gap to prevent overlap
    [Space]
    [FoldoutGroup("Sprite Rotation Settings")]
    [SerializeField] protected float baseRotation = 0f; // Rotation for sprite while flying. Default(0) is facing right
    [FoldoutGroup("Sprite Rotation Settings")]
    [SerializeField] protected float idleRotation = 0f; // Rotation while in idle animation. Default(0) is facing right
    [Space]
    [FoldoutGroup("Idle and Wander Settings")]
    [SerializeField] protected float wanderDistance = 5f;
    [FoldoutGroup("Idle and Wander Settings")]
    [SerializeField] protected bool hasIdle = false;
    [FoldoutGroup("Idle and Wander Settings")]
    [SerializeField] protected bool idleUp = false; // Whether the enemy should idle by moving up, will move down if false
    [FoldoutGroup("Idle and Wander Settings")]
    [SerializeField] protected bool swarmToRight = true; // Used to determine if the enemy should swarm to the right or left, negtive for left, positive for right, set by the FlyerSwarm script
    protected bool isIdle = false;
    protected bool movingToIdle = false;
    private float targetY; //Used for moving to ground when hasIdle
    protected Vector2 lastPosition;
    [Space]
    [FoldoutGroup("A* Pathfinding Settings")]
    [Tooltip("The distance at which the enemy considers the next waypoint reached.")]
    [SerializeField] private float nextWaypointDistance = 2f; // Distance to the next waypoint to consider it reached
    private Path path; // Current path for the AI to follow
    private int currentWaypointIndex = 0;
    protected Seeker seeker; // Seeker component for pathfinding

    // Cache last states to avoid redundant calls
    private bool lastIsFollowing = false;
    private bool lastIsFlying = false;

    protected override void Awake()
    {
        FlyerAwakeCall(); // Call the method to initialize flyer-specific properties
    }

    protected void FlyerAwakeCall()
    {
        base.Awake(); // Call the base class Awake method
        if (isSleeping) return; // Exit early if the enemy is set to sleep

        // Cache component lookups
        if (!TryGetComponent<Collider2D>(out enemyCollider)) {
            Debug.LogError("Collider2D component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<SpriteRenderer>(out enemySpriteRender)) {
            Debug.LogError("SpriteRenderer component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<Seeker>(out seeker)) {
            Debug.LogError("Seeker component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<Rigidbody2D>(out enemyBody)) {
            Debug.LogError("Rigidbody2D component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<Animator>(out enemyAnimator)) {
            Debug.LogError("Animator component is missing on " + gameObject.name);
        }
        if (hasMultiAudios) {
            if (!TryGetComponent<AudioSource>(out enemyAudioSource)) {
                Debug.LogError("AudioSource component is missing on " + gameObject.name);
            }
            // Initialize the current audio to idle audio
            ChangeAudio(enemyAudioIdle);
        }
    }

    protected void FollowPlayer()
    {
        if (!IsInvoking(nameof(UpdatePath))) {
            InvokeRepeating(nameof(UpdatePath), 0f, 0.5f); // Update path every 0.5 seconds
        }
        isFollowing = true;
        isIdle = false;

        // Only update animator if state changed
        if (hasFollowAnimation) {
            if (!lastIsFollowing) {
                enemyAnimator.SetBool("isFollowing", true);
                lastIsFollowing = true;
            }
        } else {
            if (!lastIsFlying) {
                enemyAnimator.SetBool("isFlying", true);
                lastIsFlying = true;
            }
        }

        FollowPath(); // Follow the path towards the player
    }

    protected void StopFollowingPlayer()
    {
        StopFollowingPlayer(out isFollowing, ref tempFollowDistance, ref isDamaged, wanderTarget.x, followDistance);

        if (hasFollowAnimation && lastIsFollowing) {
            enemyAnimator.SetBool("isFollowing", false);
            lastIsFollowing = false;
        }
        if (lastIsFlying) {
            enemyAnimator.SetBool("isFlying", false);
            lastIsFlying = false;
        }
        CancelInvoke(nameof(UpdatePath));
    }

    protected bool Wander(bool swarm = false)
    {
        isIdle = false;
        bool shouldFlip = false; // Flag to determine if we should flip the sprite
        // Wander within the specified range
        if (Vector2.Distance(transform.position, wanderTarget) < 0.4f) {
            // Flip the wander target to the opposite side when close enough
            wanderTarget = isFlipped ? wanderTargetRight : wanderTargetLeft;
            if (swarm) {
                shouldFlip = true;
            } else {
                FlipSprite(); // Flip the sprite to face the new direction
            }
        }
        // Move towards the wander target
        Vector2 moveDir = (wanderTarget - (Vector2)transform.position).normalized;

        if (!swarm && enemyBody.linearVelocity.magnitude > maxWanderSpeed) {
            return shouldFlip; // Prevent moving too fast
        }
        if (swarm) {
            //enemyBody.AddForce(moveSpeed * Time.deltaTime * moveDir, ForceMode2D.Impulse);
            transform.position = Vector3.MoveTowards(transform.position, wanderTarget, moveSpeed * Time.deltaTime);
        } else {
            enemyBody.MovePosition((Vector2)transform.position + moveSpeed * Time.deltaTime * moveDir);
        }
        if (CheckStuck()) {
            ResetTargetForObstacle(); // Reset stuck state if stuck
        }
        return shouldFlip;
    }


    protected bool MoveToIdle()
    {
        // Move up until reaching the targetY
        Vector2 pos = enemyBody.position;
        float tempSpeed = moveSpeed / 4;
        if (Vector2.Distance(enemyBody.position, lastPosition) < 0.01f) {
            //tempSpeed = 200f;
            enemyBody.position = new Vector2(pos.x, targetY); // Snap to targetY if very close
        }
        lastPosition = enemyBody.position; // Update last position for movement calculations
        float newY = Mathf.MoveTowards(pos.y, targetY, tempSpeed * Time.deltaTime);

        isIdle = false;
        if (!lastIsFlying) {
            enemyAnimator.SetBool("isFlying", true);
            lastIsFlying = true;
        }

        enemyBody.MovePosition(new Vector2(pos.x, newY));
        RotateSprite(false);

        // Stop moving if we've reached or passed the target
        if (Mathf.Abs(newY - targetY) < 0.01f) {
            movingToIdle = false;
            isIdle = true;
            enemyBody.linearVelocity = Vector2.zero; // Stop the Rigidbody2D
            RotateSprite(true);
            if (hasFollowAnimation) {
                enemyAnimator.SetBool("isFollowing", false);
            }
            if (hasFollowAnimation && lastIsFollowing) {
                enemyAnimator.SetBool("isFollowing", false);
                lastIsFollowing = false;
            }
            if (lastIsFlying) {
                enemyAnimator.SetBool("isFlying", false);
                lastIsFlying = false;
            }
            return false; // Return false to indicate we are idle
        }
        return true; // Return true to indicate we are still moving to idle
    }

    protected void Idle()
    {
        // If the enemy has an idle state, check if it should move up
        if (idleUp && !movingToIdle) {
            CalculateTargetToGround(Vector2.up); // Start moving up to the ground
        } else if (!movingToIdle) {
            // If not moving up, check if we should move down
            CalculateTargetToGround(Vector2.down); // Start moving down to the ground
        }
    }

    public void CalculateTargetToGround(Vector2 direction)
    {
        Vector2 origin = new(enemyCollider.bounds.center.x, enemyCollider.bounds.max.y + 0.01f);
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, checkDistance, groundLayer);

        // Cast a ray upward from the top center of the collider
        if (hit.collider != null) {
            // Move up: set the new Y position so the bottom of this collider is just below the ground
            float spriteHalfHeight = enemyCollider.bounds.extents.y;
            if (direction == Vector2.up) {
                targetY = hit.point.y - spriteHalfHeight - stopOffset;
            } else if (direction == Vector2.down) {
                targetY = hit.point.y + spriteHalfHeight + stopOffset;
            } else {
                Debug.LogError("Invalid direction for CalculateTargetToGround. Use Vector2.up or Vector2.down.");
                return;
            }

            movingToIdle = true;
        }
    }

    private void RotateSprite(bool idle)
    {
        if (idle) {
            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 270 + idleRotation);
        } else if (movingToIdle) {
            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 180 + baseRotation);
        } else if (enemyBody.linearVelocityX > 0.01f) {
            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 0 + baseRotation);
            enemySpriteRender.flipX = false;
        } else {
            enemySpriteRender.flipX = true;
        }
    }

    //protected void FlipSprite()
    //{
    //    if (isFlipped) {
    //        enemySpriteRender.flipX = false;
    //    } else {
    //        enemySpriteRender.flipX = true;
    //    }
    //    isFlipped = !isFlipped;
    //}

    private void UpdatePath()
    {
        if (seeker.IsDone()) {
            // Start a new path request to the target
            seeker.StartPath(enemyBody.position, playerTransform.position, OnPathComplete);
        }
    }

    private void FollowPath()
    {
        if (path == null || path.vectorPath.Count == 0) {
            UpdatePath();
            return;
        }
        seeker.DrawGizmos();
        UpdateAStarGrid();

        // Clamp the waypoint index to the last point
        if (currentWaypointIndex >= path.vectorPath.Count)
            currentWaypointIndex = path.vectorPath.Count - 1;

        Vector2 targetPosition = path.vectorPath[currentWaypointIndex];
        Vector2 direction = (targetPosition - enemyBody.position).normalized;
        Vector2 force = moveSpeed * Time.deltaTime * direction;
        RotateSprite(false);
        // Only apply forces if velocity is less than maxSpeed or if the force is opposite our velocity, indicating we are turning around
        if (enemyBody.linearVelocity.magnitude < maxSpeed || enemyBody.linearVelocity.x * force.x < 0.1f) {
            enemyBody.AddForce(force, ForceMode2D.Impulse);
        }

        if (Vector2.Distance(enemyBody.position, targetPosition) < nextWaypointDistance) {
            if (currentWaypointIndex < path.vectorPath.Count - 1) {
                currentWaypointIndex++;
            }
        }
    }

    private void OnPathComplete(Path p)
    {
        // Check if the path was successfully calculated
        if (p.error) {
            Debug.LogError("Pathfinding error: " + p.errorLog);
            return;
        }
        // Set the path and reset waypoint index
        seeker.PostProcess(p);
        path = p;
        currentWaypointIndex = 0;
    }
}
