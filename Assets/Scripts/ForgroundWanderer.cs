using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

[System.Flags]
public enum WandererState
{
    None = 0,
    IdleLock = 1 << 0, // 1
    AttackLock = 1 << 1, // 2
    SleepLock = 1 << 2, // 4
    JumpLock = 1 << 3,  // 8
    ActivityLock = 1 << 4, // 16
    InitJump = 1 << 5, // 32
    WakeLock = 1 << 6 // 64
}

public class ForgroundWanderer : MonoBehaviour
{
    [ToggleGroup("canWander", "Wander Settings")]
    [SerializeField] private bool canWander = false; // Flag to enable or disable wandering
    [ToggleGroup("canWander")]
    [SerializeField] private float wanderRange = 2f;
    [ToggleGroup("Wander Settings")]
    [SerializeField] private float moveSpeed = 1f;
    private Vector2 wanderStart; // Starting position for wandering
    private Vector2 wanderTarget; // Target position for wandering
    private float wanderChance = 0.0f; // Chance of wandering

    [ToggleGroup("canJump", "Jump Settings")]
    [SerializeField] private bool canJump = false; // Flag to enable or disable jumping
    [ToggleGroup("canJump")]
    [SerializeField] private float jumpChance = 0.0f; // Chance of jumping when wandering
    [ToggleGroup("canJump")]
    [SerializeField] private float jumpForce = 5f; // Force applied when the sprite jumps
    [ToggleGroup("canJump")]
    [SerializeField] private float jumpLockTime = 0.5f; // Time to lock the jump state after jumping

    public float JumpForce => jumpForce; // Public property to access jump force

    [ToggleGroup("canAttack", "Attack Settings")]
    [SerializeField] private bool canAttack = false; // Flag to enable or disable attacking
    [ToggleGroup("canAttack")]
    [SerializeField] private float attackChance = 0.0f;

    [ToggleGroup("canIdle", "Idle Settings")]
    [SerializeField] private bool canIdle = false;
    [ToggleGroup("canIdle")]
    [SerializeField] private float idleChance = 0.0f;
    [ToggleGroup("canIdle")]
    [SerializeField] private float idleDuration = 3f; // Duration of idle state
    [ToggleGroup("canIdle")]
    [SerializeField] private bool idleRandomTimer = false; // Duration of idle state

    [ToggleGroup("canSleep", "Sleep Settings")]
    [SerializeField] private bool canSleep = false; // Flag to enable or disable sleeping
    [ToggleGroup("canSleep")]
    [SerializeField] private float sleepChance = 0.1f; // Chance of sleeping when wandering
    [ToggleGroup("canSleep")]
    [SerializeField] private float sleepDuration = 5f; // Duration of sleep state
    [ToggleGroup("canSleep")]
    [SerializeField] private bool sleepRandomTimer = false;

    [FoldoutGroup("Activity Times")]
    [SerializeField] private float minActivityTime = 1f; // Minimum time before the wanderer can perform a new action
    [FoldoutGroup("Activity Times")]
    [SerializeField] private float maxActiviyTime = 2f;

    [SerializeField] private LayerMask groundLayer; // Layer mask for the ground layer

    private bool isFlipped = false; // Flag to check if the sprite is flipped
    private bool isGrounded = true; // Flag to check if the sprite is grounded
    private WandererState state = WandererState.None; // Current state of the wanderer

    private float idleTimer = 0f;
    private float sleepTimer = 0f;
    private float activiyTimer = 0f;
    private float jumpLockTimer = 0f;

    private Rigidbody2D rb;
    private Animator animator;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float[] weights = {
            canJump ? jumpChance : 0f,
            canIdle ? idleChance : 0f,
            canAttack ? attackChance : 0f,
            canSleep ? sleepChance : 0f,
            canWander ? wanderChance : 0f
        };
        if (!Mathf.Approximately(weights.Sum(), 1.0f)) {
            NormalizeWeights(weights); // Normalize weights to ensure they sum to 1.0f
        }

        wanderStart = transform.position; // Set the starting position for wandering
        sbyte direction = UnityEngine.Random.value < 0.5f ? (sbyte)-1 : (sbyte)1; // Randomly choose a direction to start wandering
        if (direction < 0) {
            wanderTarget = new Vector2(wanderStart.x - wanderRange, wanderStart.y); // Set target to the left
            FlipSprite(); // Flip the sprite to face left
        } else {
            wanderTarget = new Vector2(wanderStart.x + wanderRange, wanderStart.y); // Set target to the right
        }

        if (!TryGetComponent<Rigidbody2D>(out rb)) {
            Debug.LogError("Rigidbody2D component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<Animator>(out animator)) {
            Debug.LogError("Animator component is missing on " + gameObject.name);
        }
    }

    void FixedUpdate()
    {
        GroundCheck();

        if (!isGrounded)
            return;

        Locks(); // Update timers for idle, sleep, and attack states and manage locks including jump lock

        if (state.HasFlag(WandererState.ActivityLock)) {
            Wander(); // If in activity lock, continue wandering
            return;
        } else if (state != WandererState.None) {
            return; // If in any other state, do not perform actions
        }

        int action = GetWeightedRandom(); // Get a random action based on weights

        if (action == 0 && jumpLockTimer >= 0f) {
            while (action == 0) { // Ensure that the action is not jump if the jump lock timer is active
                action = GetWeightedRandom(); // Get a new action until it's not jump
            }
        }

        switch (action) {
            case 0: // Jump
                InitJump();
                break;
            case 1: // Idle
                Idle();
                break;
            case 2: // Attack
                Attack();
                break;
            case 3: // Sleep
                Sleep();
                break;
            case 4: // Wander
                activiyTimer = UnityEngine.Random.Range(minActivityTime, maxActiviyTime);
                Wander();
                break;
            default:
                break;
        }
    }

    private void Attack()
    {
        if (!isGrounded) { return; } // Do not attack if not grounded
        state |= WandererState.AttackLock; // Set the AttackLock state to prevent further actions while attacking
        animator.SetBool("isAttacking", true); // Trigger the attack animation
    }

    public void EndAttack()
    {
        state &= ~WandererState.AttackLock; // Clear the AttackLock state
        animator.SetBool("isAttacking", false); // Stop the attack animation
    }

    /// <summary>
    /// Flips the sprite horizontally by inverting its local scale along the X-axis.
    /// </summary>
    /// <remarks>This method toggles the flipped state of the sprite. If the sprite is currently flipped,  it
    /// will be restored to its original orientation. If the sprite is not flipped, it will  be inverted horizontally.
    /// The flipped state is determined by the <c>isFlipped</c> field.</remarks>
    private void FlipSprite()
    {
        if (isFlipped) {
            Vector3 rotation = new(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
            isFlipped = false; // Set the flipped state to true
        } else {
            Vector3 rotation = new(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotation); // Restore the sprite to its original orientation
            isFlipped = true; // Set the flipped state to false
        }
    }

    private int GetWeightedRandom()
    {
        // Set your weights (must sum to 1.0f or 100f for percentages)

        float[] weights = { jumpChance, idleChance, attackChance, sleepChance, wanderChance };
        float rand = UnityEngine.Random.value; // Random float [0,1)
        float cumulative = 0.0f;
        for (int i = 0; i < weights.Length; i++) {
            cumulative += weights[i];
            if (rand < cumulative)
                return i;
        }
        return weights.Length;
    }

    private void GroundCheck()
    {
        isGrounded = rb.IsTouchingLayers(groundLayer); // Check if the sprite is touching the ground layer
        if (!isGrounded && (state & WandererState.InitJump) != 0) {
            state &= ~WandererState.InitJump; // Clear JumpLock state if grounded
        } else if (isGrounded && (state & WandererState.JumpLock) != 0) {
            state &= ~WandererState.JumpLock; // Clear the InitJump state
        }
    }

    /// <summary>
    /// Transitions the entity into an idle state, halting movement and preventing further actions.
    /// </summary>
    /// <remarks>This method sets the entity's state to idle only if it is grounded. While idling, the
    /// entity's  movement is stopped, and the idle animation is activated. The idle state persists for the  duration
    /// specified by <c>idleDuration</c>.</remarks>
    private void Idle()
    {
        if (!isGrounded) { return; } // Do not idle if not grounded
        state |= WandererState.IdleLock; // Set the IdleLock state to prevent further actions while idling
        idleTimer = idleRandomTimer ? UnityEngine.Random.Range(idleDuration, maxActiviyTime) : idleDuration; // Set idleTimer to either a random range or the specified range
        rb.linearVelocity = Vector2.zero; // Stop any movement while idling
        animator.SetBool("isIdle", true);
    }

    /// <summary>
    /// Initializes the jump action for the wanderer if grounded.
    /// </summary>
    /// <remarks>This method sets the necessary states to lock further actions during the jump and triggers
    /// the jump animation. It has no effect if the wanderer is not grounded.</remarks>
    private void InitJump()
    {
        if (!isGrounded) { return; }
        state |= WandererState.JumpLock | WandererState.InitJump; // Set the JumpLock state to prevent further actions while jumping
        animator.SetTrigger("Jump"); // Trigger the jump animation
        jumpLockTimer = jumpLockTime; // Set the jump lock timer
    }

    /// <summary>
    /// Called by Animator to remove the WakeLock when the character has fully exited sleep modes
    /// </summary>
    private void IsAwake()
    {
        state &= ~WandererState.WakeLock;
    }

    /// <summary>
    /// Causes the object to perform a jump by applying an upward force.
    /// </summary>
    /// <remarks>The jump force is applied vertically, with an additional horizontal force based on the
    /// object's movement speed and direction. Ensure that the object's Rigidbody2D component is properly configured to
    /// respond to forces. Method is called by Animtor</remarks>
    private void Jump()
    {
        Vector2 force = Vector2.up * jumpForce;
        force.x = moveSpeed * (isFlipped ? -1 : 1);
        rb.AddForce(force, ForceMode2D.Impulse); // Apply an upward force to the sprite
    }

    private void Locks()
    {
        if (state.HasFlag(WandererState.IdleLock)) {
            idleTimer -= Time.deltaTime; // Decrease the idle timer
            if (idleTimer <= 0f) {
                animator.SetBool("isIdle", false); // Stop idling animation
                state &= ~WandererState.IdleLock; // Clear the IdleLock state
            }
        }
        if (state.HasFlag(WandererState.SleepLock)) {
            sleepTimer -= Time.deltaTime; // Decrease the sleep timer
            if (sleepTimer <= 0f) {
                animator.SetBool("isSleeping", false); // Stop sleeping animation
                state &= ~WandererState.SleepLock; // Clear the SleepLock state
            }
        }
        if (state.HasFlag(WandererState.ActivityLock)) {
            activiyTimer -= Time.deltaTime; // Decrease the activity timer
            if (activiyTimer <= 0f) {
                state &= ~WandererState.ActivityLock; // Clear the ActivityLock state
            }
        }
        if (state.HasFlag(WandererState.JumpLock) || isGrounded) {
            state &= ~WandererState.JumpLock; // Clear the JumpLock state if grounded
        }
        if (jumpLockTimer >= 0f) {
            jumpLockTimer -= Time.deltaTime; // Decrease the jump lock timer
        }
    }

    private void NormalizeWeights(float[] weights)
    {
        if (weights.Sum() > 1.0f) {
            Debug.LogWarning("Weights sum to more than 1.0f, normalizing them. " + gameObject.name);
        } else if (weights.Sum() > 0.0f) {
            if (!canWander) {
                Debug.LogError("Wanderer is not allowed to wander, setting wander chance to 0.0f for " + gameObject.name);
                return;
            }
            wanderChance = 1.0f - weights.Sum(); // Calculate the remaining chance for wandering
            return; // Exit if the weights do not sum to 1.0f
        } else {
            Debug.LogError("Weights sum to 0.0f, cannot normalize. " + gameObject.name);
            return; // Exit if the weights sum to 0.0f
        }
            float sum = weights.Sum();
        for (int i = 0; i < weights.Length; i++) {
            weights[i] /= sum;
        }
        jumpChance = weights[0];
        idleChance = weights[1];
        attackChance = weights[2];
        sleepChance = weights[3];
        wanderChance = weights[4];
    }

    private void Sleep()
    {
        state |= WandererState.SleepLock | WandererState.WakeLock; // Set the SleepLock state to prevent further actions while sleeping
        sleepTimer = sleepRandomTimer ? UnityEngine.Random.Range(sleepDuration, maxActiviyTime) : sleepDuration; // Set sleepTimer to either a random duration or the one specified
        rb.linearVelocity = Vector2.zero; // Stop any movement while sleeping
        animator.SetBool("isSleeping", true); // Trigger the sleep animation
    }

    private void Wander()
    {
        if (!isGrounded) { return; } // Do not wander if not grounded
        state |= WandererState.ActivityLock;
        // Wander within the specified range
        if (Vector2.Distance(transform.position, wanderTarget) < 0.4f) {
            // Flip the wander target to the opposite side when close enough
            wanderTarget.x = isFlipped ? wanderStart.x + wanderRange : wanderStart.x - wanderRange;
            FlipSprite(); // Flip the sprite to face the new direction
        }
        // Move towards the wander target
        Vector2 moveDir = (wanderTarget - (Vector2)transform.position).normalized;
        rb.MovePosition((Vector2)transform.position + moveSpeed * Time.deltaTime * moveDir);
    }
}
