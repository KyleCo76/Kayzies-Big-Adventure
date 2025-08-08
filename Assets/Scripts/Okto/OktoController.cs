using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

public class OktoController : MonoBehaviour
{
    // Pathfinding Variables
    [FoldoutGroup("Pathfinding Settings")]
    [SerializeField] private float nextWaypointDistance = 6.0f; // Distance to the next waypoint
    private Path path; // The current path the AI is following
    private int currentWaypoint = 0;
    Seeker seeker; // Reference to the Seeker component

    // Movement Settings
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private Vector2 targetOffset = new(1.5f, 1.5f); // Offset to the target position
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float followDistance = 1f;
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float moveSpeed = 500f; // Default speed at which Okto moves
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float minMoveSpeed = 200f; // Minimum force that will push Okto
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float maxMoveSpeed = 5000f; // Maximum movement speed allowed
    [FoldoutGroup("Movement Settings")]
    [SerializeField] private float hardStopVelocity = 2f; // Velocity threshold for hard stop animation
    [FoldoutGroup("Movement Settings")]
    public float moveMultiplierIncrement = 0.5f;
    private Vector2 targetPosition; // The target position for the AI to move towards
    private bool isFlipped = false; // Flag to indicate if Okto is flipped
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Range within which Okto will stop moving towards the target position.")]
    [SerializeField] private float oktoStopRange = 0.5f;
    //private float flipPressure = 0;

    // Ground Check Settings
    [FoldoutGroup("Ground Check Settings")]
    [SerializeField] private float groundCheckDistance = 20f; // Distance to check for ground
    [FoldoutGroup("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer; // Layer mask to identify ground layers
    [FoldoutGroup("Ground Check Settings")]
    [SerializeField] private float fallVelocityCheck = -20f;

    // Idle Animation Settings
    [FoldoutGroup("Idle Animation Settings")]
    [SerializeField] private float timeUntilIdle = 2f;
    [FoldoutGroup("Idle Animation Settings")]
    [SerializeField] private float timeUntilIdle2 = 6f; // Additional time for the second idle state
    [FoldoutGroup("Idle Animation Settings")]
    [SerializeField] private float timeUntilWater = 10f; // Time until Okto enters water state
    [FoldoutGroup("Idle Animation Settings")]
    [SerializeField] private float idleVelocity;
    private float idleAnimTimer = 0f; // Timer for idle state
    private float totalIdleTime = 0f; // Total time spent in idle state

    // Components
    [FoldoutGroup("Components")]
    [SerializeField] private Transform playerTransform; // Reference to the player's transform
    Rigidbody2D oktoBody; // Reference to the Rigidbody2D component
    Animator animator; // Reference to the Animator component
    Collider2D oktoCollider;

    // Water State Management
    private bool inWater = false; // Flag to indicate if Okto is in water
    private bool isIdle = false; // Flag to indicate if Okto is idle
    private bool isRunning = false; // Flag to indicate if Okto is running
    private bool hardStop = false; // Flag to indicate if Okto is in a hard stop state

    // Shooter Variables
    private bool flipForShooting = false; // Flag to indicate if Okto is flipped for shooting
    public bool FlipForShooting { get => flipForShooting; set { flipForShooting = value; } }
    private bool didShoot = false;
    public bool DidShoot { get => didShoot;
        set {
            didShoot = value;
            if (!value) {
                idleAnimTimer = totalIdleTime = 0f; // Reset idle timer when releasing didShoot
            }
        }
    }

    /// <summary>
    /// Initializes the necessary components and starts periodic path updates for the object.
    /// </summary>
    /// <remarks>This method verifies the presence of required components, including <see cref="Seeker"/>, 
    /// <see cref="Rigidbody2D"/>, and <see cref="Animator"/> on the current object, as well as  <see
    /// cref="PlayerController"/> on the player's transform. If any required component is missing,  an error is logged
    /// to the console.  Once initialized, the method calculates the target position based on the player's current
    /// position  and facing direction, and begins invoking the <see cref="UpdatePath"/> method at regular intervals  to
    /// update the object's path.</remarks>
    void Start()
    {

        if (!TryGetComponent<Seeker>(out seeker)) {
            Debug.LogError("Seeker component not found on " + gameObject.name);
        }
        if (!TryGetComponent<Rigidbody2D>(out oktoBody) || !TryGetComponent<Animator>(out animator)) {
            Debug.LogError("Rigidbody2D or Animator component not found on " + gameObject.name);
        }
        if (!playerTransform.TryGetComponent<Kayzie.Player.PlayerControllerV2>(out var playerController)) {
            Debug.LogError("PlayerController component not found on " + playerTransform.name);
        }
        if (!TryGetComponent<Collider2D>(out oktoCollider)) {
            Debug.LogError("Collider2D component not found on " + gameObject.name);
        }

        targetPosition = (Vector2)playerTransform.position + (playerController.IsFlipped ? new Vector2(-targetOffset.x, targetOffset.y) : targetOffset); // Get the player's position
        InvokeRepeating(nameof(UpdatePath), 0f, 0.25f); // Start updating the path every 0.25 seconds
    }
    
    /// <summary>
    /// Updates the object's position and state at fixed intervals, ensuring smooth movement and behavior.
    /// </summary>
    /// <remarks>This method is called automatically by Unity's physics system at fixed time steps. It checks
    /// the object's proximity      to the target position, updates its path if necessary, calculates movement forces,
    /// and applies them to the object.      Additionally, it manages idle state and sprite orientation based on
    /// movement direction.</remarks>
    void FixedUpdate()
    {
        UpdateTarget();
        //Check if Okto is close enough to the target position
        if ((Mathf.Abs(this.transform.position.x - targetPosition.x) <= oktoStopRange &&
            Mathf.Abs(this.transform.position.y - targetPosition.y) <= oktoStopRange))
        {
            IdleManager(true); // Call the IdleManager method with true to indicate Okto is close enough to the target position
            return; // Exit the method to prevent further movement
        } else {
            IdleManager(false); // Call the IdleManager method with false to indicate Okto is not close enough to the target position
        }

        if (path == null || path.vectorPath.Count == 0) {
            UpdatePath();
            return;
        }

        DetermineWaypoint();

        //// Determine the force to apply to Okto
        Vector2 force = CalculateMoveForce();

        Run(force);

        //Flip the sprite based on the direction of movement
        FlipSprite(force.x);
    }

    /// <summary>
    /// Calculates the movement force required to move towards the target position.
    /// </summary>
    /// <remarks>This method determines the direction and distance to the target position and calculates the
    /// appropriate movement force based on the current speed, distance, and other conditions. The resulting force takes
    /// into account constraints such as minimum and maximum movement speed, as well as adjustments for falling and
    /// proximity to the ground.</remarks>
    /// <returns>A <see cref="Vector2"/> representing the calculated movement force, where the X and Y components indicate the
    /// horizontal and vertical forces, respectively.</returns>
    private Vector2 CalculateMoveForce()
    {

        // Determine direction and distance to target position
        //Vector2 direction = (targetPosition - (Vector2)transform.position).normalized; // Calculate the direction to the player
        if (currentWaypoint >= path.vectorPath.Count || currentWaypoint < 0) {
            UpdatePath();
            currentWaypoint = path.vectorPath.Count - 1; // Clamp to last waypoint
        }
        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - (Vector2)transform.position).normalized; // Calculate the direction to the next waypoint
        Vector2 targetDistance = (Vector2)targetPosition - (Vector2)transform.position;

        float distance = Mathf.Abs(targetDistance.x) + Mathf.Abs(targetDistance.y);
        float tempSpeed = (Mathf.Abs(distance)) * moveSpeed;
        if (distance > followDistance) {
            tempSpeed += moveSpeed;
        }

        if (distance < followDistance * 2) {
            tempSpeed /= 8;
        }

        tempSpeed = Mathf.Clamp(tempSpeed, minMoveSpeed, maxMoveSpeed);
        
        Vector2 force = tempSpeed * direction; // Calculate the force to apply based on the direction and speed

        if (oktoBody.linearVelocityY < fallVelocityCheck && IsNearGround()) {
            if (GroundImminent()) {
                // If Okto is falling and near the ground, set vertical force to zero
                force.y = moveSpeed;
                oktoBody.linearVelocity = Vector2.zero; // Reset vertical velocity to zero
                Debug.Log("Okto is falling and close to the ground, setting vertical force to -moveSpeed: " + oktoBody.linearVelocityY);
            } else {
                force.y = 0f; // If Okto is falling and near the ground, set vertical force to zero
                Debug.Log("Okto is near the ground, setting vertical force to zero." + oktoBody.linearVelocityY);
            }
    
        }
        return force;
    }

    /// <summary>
    /// Updates the object's state to indicate whether it is in water.
    /// </summary>
    /// <param name="state">A value indicating the water state. A value greater than <see langword="0.0f"/> sets the object as in water;
    /// otherwise, it is set as not in water.</param>
    public void ChangeWaterState(float state)
    {
        inWater = state > 0.0f;
        animator.SetBool("inWater", inWater);
        animator.ResetTrigger("ExitWater");
    }

    /// <summary>
    /// Updates the current waypoint in the path based on the proximity to the next waypoint.
    /// </summary>
    /// <remarks>This method ensures that the current waypoint is clamped to the last waypoint in the path if
    /// it exceeds the path's bounds. It also advances to the next waypoint when the distance to the current waypoint is
    /// less than the specified threshold.</remarks>
    private void DetermineWaypoint()
    {
        if (currentWaypoint >= path.vectorPath.Count) {
            UpdatePath();
            currentWaypoint = path.vectorPath.Count - 1; // Clamp to last waypoint
        }

        //// Manage changing waypoints by checking if we are close enough to the next waypoint or past our current waypoint
        float distanceToWaypoint = Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]);
        if ((!isFlipped && distanceToWaypoint < 0f) || (isFlipped && distanceToWaypoint > 0f)) {
            currentWaypoint++;
            return;
        } else if (distanceToWaypoint < nextWaypointDistance && currentWaypoint < path.vectorPath.Count - 1) {
            currentWaypoint++; // Move to the next waypoint
        }
        currentWaypoint = Mathf.Clamp(currentWaypoint, 0, path.vectorPath.Count - 1); // Ensure currentWaypoint is within bounds
    }

    /// <summary>
    /// Flips the sprite's orientation based on the horizontal force applied.
    /// </summary>
    /// <remarks>This method adjusts the sprite's local scale to reflect its orientation and updates the 
    /// flipped state. If the sprite's horizontal velocity exceeds a predefined threshold in the  opposite direction, a
    /// "RunStop" animation is triggered.</remarks>
    /// <param name="xForce">The horizontal force applied to the sprite. A positive value flips the sprite to face right,  while a negative
    /// value flips it to face left.</param>
    //public void FlipSprite(float xForce)
    //{
    //    if (flipForShooting) {
    //        // If Okto is flipped for shooting, do not flip the sprite
    //        return;
    //    }
    //    bool flip = FlipOnVelocity(); // Check if the sprite should be flipped based on velocity
    //    //if (!flip) {
    //    //    flip = FlipOnPressure(xForce); // Check if the sprite should be flipped based on pressure
    //    //}

    //    //xForce = oktoBody.linearVelocityX; // Use the horizontal velocity of Okto to determine the direction
    //    if ((oktoBody.linearVelocityY >= 0.1f) || (xForce >= 0.1f && isFlipped)) {
    //        // Move right
    //        isFlipped = false; // Reset the flipped state
    //        //transform.localScale = new Vector3(transform.localScale.x,transform.localScale.y, 1);
    //        Vector3 rotation = new(transform.rotation.x, 0f, transform.rotation.z);
    //        transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
    //        if (Mathf.Abs(oktoBody.linearVelocityX) > hardStopVelocity) {
    //            animator.SetTrigger("RunStop"); // Trigger the run stop animation if moving left at high speed
    //        }
    //    } else if ((oktoBody.linearVelocityX <= 0.1f) || (xForce <= -0.1f && !isFlipped)) {
    //        // Move left
    //        isFlipped = true; // Set the flipped state
    //        //transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, 1);
    //        Vector3 rotation = new(transform.rotation.x, 180f, transform.rotation.z);
    //        transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
    //        if (Mathf.Abs(oktoBody.linearVelocityX) < -hardStopVelocity) {
    //            animator.SetTrigger("RunStop"); // Trigger the run stop animation if moving right at high speed
    //        }
    //    }
    //}

    public void FlipSprite(float xForce)
    {
        Vector3 direction = (targetPosition - (Vector2)transform.position).normalized; // Calculate the direction to the target position
        if (direction.x > 0f && isFlipped) {
            // Move right
            isFlipped = false; // Reset the flipped state

            Vector3 rotation = new(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
            if (Mathf.Abs(oktoBody.linearVelocityX) > hardStopVelocity) {
                animator.SetTrigger("RunStop"); // Trigger the run stop animation if moving left at high speed
            }
        } else if (direction.x < 0f && !isFlipped) {
            // Move left
            isFlipped = true; // Set the flipped state

            Vector3 rotation = new(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
            if (Mathf.Abs(oktoBody.linearVelocityX) < -hardStopVelocity) {
                animator.SetTrigger("RunStop"); // Trigger the run stop animation if moving right at high speed
            }
        }
    }

    //private bool FlipOnPressure(float force)
    //{
    //    if (Mathf.Abs(oktoBody.linearVelocityX) > 2f) {
    //        return false;
    //    }
    //    if (force >= 0.1f) {
    //        if (flipPressure < 0) flipPressure = 0;
    //        flipPressure += Time.deltaTime; // Increase pressure over time
    //        if (flipPressure >= 5f) {
    //            return true; // Flip the sprite if pressure is high enough
    //        } else {
    //            return false; // Do not flip the sprite if pressure is not high enough
    //        }
    //    } else if (force <= -0.1f) {
    //        if (flipPressure > 0) flipPressure = 0;
    //        flipPressure -= Time.deltaTime;
    //        if (flipPressure <= -5f) {
    //            return true; // Flip the sprite if pressure is high enough
    //        } else {
    //            return false; // Do not flip the sprite if pressure is not high enough
    //        }
    //    }
    //    return false; // Do not flip the sprite if pressure is not high enough
    //}

    private bool FlipOnVelocity()
    {
        if (oktoBody.linearVelocityX >= 0.1f && isFlipped) {
            return true; // Flip the sprite if moving right and currently flipped
        } else if (oktoBody.linearVelocityX <= -0.1f && !isFlipped) {
            return true; // Flip the sprite if moving left and currently not flipped
        }
        return false; // Do not flip the sprite if velocity is not high enough
    }

    /// <summary>
    /// Determines whether the ground is within a specified distance below the object.
    /// </summary>
    /// <remarks>This method uses a 2D raycast to check for ground presence directly below the object's
    /// position. Ensure that <c>groundCheckDistance</c> and <c>groundLayer</c> are properly configured for accurate
    /// detection.</remarks>
    /// <returns><see langword="true"/> if the ground is detected within the specified distance; otherwise, <see
    /// langword="false"/>.</returns>
    private bool GroundImminent()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null && hit.distance < 10f; // Check if the distance to the ground is less than 0.5 units
    }

    /// <summary>
    /// Resets the hard stop condition, allowing operations to continue.
    /// </summary>
    /// <remarks>This method clears the hard stop flag by setting it to <see langword="false"/>.  It should be
    /// called when the system is ready to resume operations after a hard stop condition.</remarks>
    public void HardStopReset()
    {
        hardStop = false;
    }

    /// <summary>
    /// Immediately halts all movement by setting the hard stop flag and resetting velocity to zero.
    /// </summary>
    /// <remarks>This method is intended to enforce a complete stop in motion. It sets the internal hard stop
    /// flag  and clears the linear velocity of the associated object. Use this method when an abrupt stop is
    /// required.</remarks>
    public void HardStop()
    {
        hardStop = true; // Set the hard stop flag to true
        oktoBody.linearVelocity = Vector2.zero; // Reset Okto's velocity to zero
    }

    /// <summary>
    /// Manages the idle state of the character, transitioning between idle, running, and water states based on the
    /// provided input.
    /// </summary>
    /// <remarks>This method handles the character's behavior when idle, including animations and transitions
    /// to secondary idle states or water entry.  It ensures the character's state is updated correctly based on timers
    /// and conditions, such as whether the character is running or already in water.  When entering idle, the method: -
    /// Stops running animations if the character is running. - Tracks idle time and transitions to idle animations or
    /// water entry based on configured thresholds. - Triggers secondary idle animations if applicable.  When exiting
    /// idle, the method: - Resets idle timers and flags. - Handles transitions out of water if the character is in
    /// water.  This method assumes that the character's animation parameters and state flags (e.g., <c>isRunning</c>,
    /// <c>isIdle</c>, <c>inWater</c>)  are managed externally and updated consistently with the character's
    /// behavior.</remarks>
    /// <param name="enterIdle">A boolean value indicating whether the character should attempt to enter the idle state.  If <see
    /// langword="true"/>, the method evaluates and transitions the character to idle or water states as appropriate. 
    /// If <see langword="false"/>, the method resets idle-related states and transitions out of idle or water states if
    /// necessary.</param>
    private void IdleManager(bool enterIdle)
    {
        if (enterIdle) {
            //Quit if already in water
            if (inWater) { return; }
            if (isRunning) {
                animator.SetBool("isRunning", false); // Set the isRunning parameter to false to stop running animations
                isRunning = false; // Reset the isRunning flag
            }

            idleAnimTimer += Time.deltaTime; // Increment the idle timer
            totalIdleTime += Time.deltaTime; // Increment the total idle time

            // Ensure Okto is facing the player when idle
            float direction = transform.position.x - playerTransform.position.x; // calculate the direction to the player
            if (isFlipped && direction <= -0.01f) { // if moving right and okto is flipped
                FlipSprite(1f); // reset the scale to face right
            } else if (!isFlipped && direction >= 0.01f) { // if moving left and okto is not flipped
                FlipSprite(-1f); // set the scale to face left
            }

            // Attempt to enter idle state
            if (!isIdle && idleAnimTimer >= timeUntilIdle) {
                animator.SetBool("isIdle", true); // Set the isIdle parameter to true to start idle animations
                isIdle = true; // Set the isIdle flag to true
                idleAnimTimer = 0f; // Reset the idle timer
                totalIdleTime = 0f; // Reset the total idle time
                return; // Exit the method to prevent further processing
            }

            // Check if total idle time exceeds the threshold for entering water
            if (totalIdleTime >= timeUntilWater && !didShoot) {
                ChangeWaterState(1.0f); // Call the ExitWater method to handle water entry
                isIdle = false; // Reset the isIdle flag
                inWater = true; // Set inWater to true to indicate Okto is now in water
                animator.SetBool("isIdle", false); // Set the isIdle parameter to false to stop idle animations
                animator.SetTrigger("EnterWater"); // Trigger the water entry animation
                idleAnimTimer = 0f; // Reset the idle timer
                totalIdleTime = 0f; // Reset the total idle time
                return; // Exit the method to prevent further processing
            }

            // Attempt to enter second idle state
            if (idleAnimTimer >= timeUntilIdle2 && !didShoot) {
                animator.SetTrigger("Idle2"); // Trigger the second idle animation
                idleAnimTimer = 0f; // Reset the idle timer
                return; // Exit the method to prevent further processing
            }
        // If not entering idle state
        } else {
            idleAnimTimer = totalIdleTime = 0f; // Reset the idle timer
            if (isIdle) {
                animator.SetBool("isIdle", false); // Set the isIdle parameter to false to stop idle animations
                isIdle = false; // Reset the isIdle flag
            }
            if (inWater) {
                animator.SetTrigger("ExitWater"); // Trigger the exit water animation
                return; // Exit the method to prevent further processing. inWater will be handled in the ChangeWaterState method
            }
        }
    }

    /// <summary>
    /// Determines whether the object is near the ground.
    /// </summary>
    /// <remarks>This method performs a raycast downward from the object's position to check for ground within
    /// a specified distance. The ground is identified based on the configured ground layer.</remarks>
    /// <returns><see langword="true"/> if the object is near the ground; otherwise, <see langword="false"/>.</returns>
    private bool IsNearGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// Handles the completion of a pathfinding operation.
    /// </summary>
    /// <remarks>This method processes the result of a pathfinding operation. If the pathfinding was
    /// successful,  it updates the current path and resets relevant state variables. If an error occurred,  it logs the
    /// error details for debugging purposes.</remarks>
    /// <param name="p">The completed path object. If the pathfinding operation was successful,  this parameter contains the calculated
    /// path. If an error occurred, it contains error details.</param>
    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p; // Set the current path to the completed path
            currentWaypoint = 0; // Reset the current waypoint index
            seeker.PostProcess(p); // Post-process the path to ensure it is ready for use
        }
        else
        {
            Debug.LogError("Path error: " + p.errorLog); // Log any errors in pathfinding
        }
    }

    /// <summary>
    /// Updates the running state of the object based on its velocity and applies a force if certain conditions are met.
    /// </summary>
    /// <remarks>The method checks the object's velocity to determine whether it is running or idle and
    /// updates the animator accordingly. If the object is not in water and is not in a hard stop state, the specified
    /// force is applied to the object.</remarks>
    /// <param name="force">The force to apply to the object, represented as a 2D vector.</param>
    private void Run(Vector2 force)
    {
        // Set animator to running and apply forces
        if (oktoBody.linearVelocity.magnitude < idleVelocity) {
            animator.SetBool("isRunning", false);
            isRunning = false;
        } else if (!isRunning) {
            animator.SetBool("isRunning", true); // Set the isRunning parameter to true to start running animations
            isRunning = true; // Set the isRunning flag to true
        }
        if (Vector2.Distance(transform.position, targetPosition) < oktoStopRange) {
            oktoBody.linearVelocityX /= 2f;
            force.x /= 2f;
        }
        if (!inWater && !hardStop) {
            oktoBody.AddForce(force);
            //transform.position = Vector2.MoveTowards(transform.position, targetPosition, force.magnitude * Time.fixedDeltaTime); // Move towards the target position
        }
    }

    /// <summary>
    /// Updates the path for the seeker to follow, targeting the player's position with an offset.
    /// </summary>
    /// <remarks>This method checks if the seeker is ready to calculate a new path and, if so, determines the
    /// target position  based on the player's current position and facing direction. It then initiates pathfinding from
    /// the current  position to the calculated target position.</remarks>
    private void UpdatePath()
    {
        if (seeker.IsDone()) // Check if the seeker is ready to calculate a new path
        {
            UpdateTarget();
            seeker.StartPath(transform.position, targetPosition, OnPathComplete); // Start a new path from the octopus to the player
        }
    }

    private void UpdateTarget()
    {
        Kayzie.Player.PlayerControllerV2 playerController = playerTransform.GetComponent<Kayzie.Player.PlayerControllerV2>(); // Get the PlayerController component from the player Transform
        targetPosition = (Vector2)playerTransform.position + (playerController.IsFlipped ? targetOffset : new Vector2(-targetOffset.x, targetOffset.y)); // Get the player's position with offset
    }

    protected void UpdateAStarGrid()
    {
        Bounds area = oktoCollider.bounds;
        area.Expand(4.0f); // Expand the bounds to include a margin for pathfinding
        AstarPath.active.UpdateGraphs(area); // Update the A* pathfinding graphs
    }

}
