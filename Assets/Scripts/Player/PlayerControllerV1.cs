using Kayzie.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))] // Ensure the player has these components
public partial class PlayerControllerV3 : MonoBehaviour
{
    // Player Components
    private Rigidbody2D playerBody;
    private Animator playerAnimator;

    // Animation Settings
    [FoldoutGroup("Animation Settings")]
    [Tooltip("Maximum animation speed for player movement.")]
    [SerializeField] private float maxAnimationSpeed = 1.5f;
    [FoldoutGroup("Animation Settings")]
    [Tooltip("Minimum animation speed for player movement.")]
    [SerializeField] private float minAnimationSpeed = 0.5f;
    private Vector2 moveInput; // Stores the movement input from the player
    private float baseMoveSpeed; // Base movement speed for the player used for temporary speed changes

    // Player Sprite Flipping Variables
    private bool isFlipped = false;
    public bool IsFlipped => isFlipped; // Indicates if the player sprite is flipped
    public bool IsFacingLeft => isFlipped;
    private bool isClimbing = false; // Tracks if the player is currently climbing a surface
    public bool IsClimbing => isClimbing;
    private bool canClimb = false; // Tracks if the player is near a climbable surface
    public bool CanClimb {
        get => canClimb;
        set { canClimb = value; }
    }

    // Interaction Variables
    private bool isInteracting = false;
    public bool IsInteracting => isInteracting;

    //// Ground Check Variables
    [PropertyOrder(1)]
    [FoldoutGroup("Ground Check Settings")]
    [Tooltip("Layer mask for ground detection. Set to the layer(s) that represent the ground in your game.")]
    [SerializeField] private LayerMask groundLayer;
    [FoldoutGroup("Ground Check Settings")]
    [Tooltip("Transform for ground check. Assign an empty GameObject at the player's feet in the Inspector.")]
    [SerializeField] private Collider2D groundCheck;
    //[FoldoutGroup("Ground Check Settings")]
    //[Tooltip("Radius of the ground check area. Adjust based on your game's requirements.")]
    //[SerializeField] private float groundCheckRadius = 0.1f;
    private bool isGrounded = true;

    // Camera Follow Variables
    [FoldoutGroup("Camera Follow Settings")]
    [Tooltip("Object that the camera will follow.")]
    [SerializeField] private GameObject cameraFollowObject;
    private CameraFollowObject cameraFollow; // Reference to the CameraFollowObject script
    private float fallSpeedYDampingChangeThreshold; // Threshold for changing camera damping based on fall speed

    // Player InputAction Variables
    [FoldoutGroup("Input Actions")]
    [Tooltip("Input action for player movement.")]
    [SerializeField] private InputAction moveAction;
    [FoldoutGroup("Input Actions")]
    [Tooltip("Input action for player interactions.")]
    [SerializeField] private InputAction interactAction;
    [FoldoutGroup("Input Actions")]
    [Tooltip("Input action for player jumping.")]
    [SerializeField] private InputAction jumpAction;
    [FoldoutGroup("Input Actions")]
    [Tooltip("Input action for player sprinting.")]
    [SerializeField] private InputAction sprintAction;
    [FoldoutGroup("Input Actions")]
    [Tooltip("Input action for player attacking.")]
    [SerializeField] private InputAction attackAction;

    // Teleport Variables
    [PropertyOrder(2)]
    [Tooltip("Cooldown time for teleporting.")]
    [SerializeField] private float teleportCooldown = 1.0f;
    private bool canTeleport = true;
    public bool CanTeleport => canTeleport;
    private float teleportTimer = 0f;

    // Collider Variables
    [FoldoutGroup("Colliders")]
    [Tooltip("Collider used when player is ducking or crouching.")]
    [SerializeField] private Collider2D crouchCollider;
    [FoldoutGroup("Colliders")]
    [Tooltip("Collider used when player is not ducking or crouching.")]
    [SerializeField] private Collider2D playerCollider;

    [FoldoutGroup("Followers")]
    [Tooltip("Array of follower transforms to be updated with the player's position.")]
    [SerializeField] protected Transform[] followers;
    public Transform[] Followers => followers;
    private bool lastIsDucking = false; // Tracks the last ducking state for animation updates
    private bool lastIsSkipping = false; // Tracks the last skipping state for animation updates
    private bool lastIsJumping = false; // Tracks the last jumping state for animation updates
    private bool lastIsClimbing = false; // Tracks the last climbing state for animation updates
    private float lastAnimSpeed = 1.0f; // Last animation speed for the player

    private DialogManager dialogManagerInstance;
    private bool isInDialog = false;


    private void Awake()
    {
        dialogManagerInstance = FindAnyObjectByType<DialogManager>();
    }
    /// <summary>
    /// Initializes the player controller by setting up required components, enabling input actions,  and configuring
    /// initial player state.
    /// </summary>
    /// <remarks>This method retrieves and validates essential components such as <see cref="Rigidbody2D"/>, 
    /// <see cref="Animator"/>, and <see cref="PlayerHealthManager"/>. If any required component is  missing, an error
    /// is logged, and the method exits early. It also enables input actions for  movement, interaction, and sprinting,
    /// and sets initial values for animation speed, teleport  cooldown, and movement speed.</remarks>
    void Start()
    {
        playerBody = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component attached to the player
        playerAnimator = GetComponent<Animator>(); // Get the Animator component attached to the player
        //playerHealthManager = GetComponent<PlayerHealthManager>(); // Get the PlayerHealthManager component attached to the player
        //if (playerBody == null || playerAnimator == null || playerHealthManager == null) {
        //    Debug.LogError("PlayerController missing required components");
        //    return; // Exit if components are not found
        //}

        cameraFollow = cameraFollowObject.GetComponent<CameraFollowObject>(); // Get the CameraFollowObject component from the assigned object
        fallSpeedYDampingChangeThreshold = Managers.Instance.CameraManager.fallSpeedYDampingChangeThreashold; // Get the fall speed damping change threshold from CameraManagger

        playerAnimator.SetFloat("animSpeed", 1.0f); // Set the animation speed to an intial value
        teleportTimer = teleportCooldown; // Initialize the teleport cooldown timer
        baseMoveSpeed = moveSpeed; // Store the base movement speed for later use
        crouchCollider.enabled = false; // Disable the crouch collider initially

        DamageStart();
    }

    /// <summary>
    /// Subscribes to the jump action event and enables the associated input action.
    /// </summary>
    /// <remarks>This method is called automatically when the component is enabled. It attaches the <see
    /// cref="OnJump"/> event handler to the <see cref="jumpAction"/> and enables the input action to start listening
    /// for input.</remarks>
    public void OnEnable()
    {
        jumpAction.performed += OnJump;
        jumpAction.Enable();
        sprintAction.performed += OnSprint;
        sprintAction.canceled += OnSprint;
        sprintAction.Enable(); // Enable the sprint action to start listening for input
        interactAction.performed += context => isInteracting = context.performed; // Subscribe to the interact action to set the interaction state
        interactAction.canceled += context => isInteracting = !context.canceled;
        interactAction.Enable();
        attackAction.performed += OnAttack;
        attackAction.Enable();
        moveAction.Enable();
        dialogManagerInstance.OnDialogStart += DialogStarted;
        dialogManagerInstance.OnDialogEnd += DialogEnded;
    }

    /// <summary>
    /// Called when the object is disabled. Unsubscribes from input action events and disables the associated input
    /// actions.
    /// </summary>
    /// <remarks>This method ensures that input actions such as jumping and sprinting are properly
    /// unsubscribed and disabled when the object is no longer active. This prevents unintended behavior or memory leaks
    /// caused by lingering event subscriptions.</remarks>
    void OnDisable()
    {
        jumpAction.performed -= OnJump;
        jumpAction.Disable();
        sprintAction.performed -= OnSprint;
        sprintAction.canceled -= OnSprint;
        sprintAction.Disable();
        interactAction.performed -= context => isInteracting = context.performed;
        interactAction.Disable();
        interactAction.canceled -= context => isInteracting = !context.canceled;
        attackAction.performed -= OnAttack;
        attackAction.Disable();
        moveAction.Disable();
        dialogManagerInstance.OnDialogStart -= DialogStarted;
        dialogManagerInstance.OnDialogEnd -= DialogEnded;
    }

    /// <summary>
    /// Updates the player's physics and animation states at a fixed time interval.
    /// </summary>
    /// <remarks>This method is called at a consistent rate, independent of the frame rate, to handle
    /// physics-related updates. It checks the player's ability to sprint, applies movement forces, manages sprite
    /// orientation, and updates animation states based on the player's current actions and environment.</remarks>
    void FixedUpdate()
    {
        if (isInDialog) return; // If the player is in a dialog, skip the update

        CanSprint(out float horizontalInput, out float moveForce); // Check if the player can sprint and get the horizontal input and force to apply
        
        ApplyForces(horizontalInput, moveForce); // Apply forces based on player input and sprinting state

        // Check for sprite flipping based on movement direction
        if (moveInput.x < 0.0f && !isFlipped) {
            FlipSprite();
        } else if (moveInput.x > 0.0f && isFlipped) {
            FlipSprite();
        }

        // Handle jumping logic
        CheckGrounded(); // Check if the player is grounded

        UpdateAnimatorStates();
    }

    /// <summary>
    /// Updates the player's state and behavior each frame, handling movement, stamina, animation, and other gameplay
    /// mechanics.
    /// </summary>
    /// <remarks>This method should be called every frame to ensure the player's state is updated correctly.
    /// It processes input for movement, manages stamina consumption, adjusts camera damping based on the player's
    /// vertical velocity, and handles teleportation cooldowns. The method also updates the player's animation speed
    /// according to their movement and runs additional update logic from other parts of the partial class.</remarks>
    void Update()
    {
        if (GameManager.isPaused) return;

        moveInput = moveAction.ReadValue<Vector2>();

        StaminaManager(Mathf.Abs(moveInput.x) > 0.1f);

        if (isInDialog) return;

        isClimbing = moveInput.y > 0.1f;

        // Check if the player is falling and adjust camera damping accordingly
        if (playerBody.linearVelocityY < fallSpeedYDampingChangeThreshold && !Managers.Instance.CameraManager.IsLerpingYDamping && !Managers.Instance.CameraManager.LerpedFromPlayerFalling) {
            Managers.Instance.CameraManager.LerpYDamping(true); // Lerp camera damping for falling state
        } else if (playerBody.linearVelocityY >= 0f && !Managers.Instance.CameraManager.IsLerpingYDamping && Managers.Instance.CameraManager.LerpedFromPlayerFalling) {
            Managers.Instance.CameraManager.LerpedFromPlayerFalling = false; // Reset the lerped state
            Managers.Instance.CameraManager.LerpYDamping(false); // Lerp camera damping back to normal state
        }

        // Manage teleportation cooldown
        if (!canTeleport) {
            teleportTimer -= Time.deltaTime;
            if (teleportTimer <= 0f) {
                canTeleport = true;
                teleportTimer = teleportCooldown;
            }
        }
        // Clamp the player's vertical velocity to prevent excessive falling speed
        if (playerBody.linearVelocityY > maxVerticalVelocity) {
            playerBody.linearVelocityY = maxVerticalVelocity;
        }

        // Update the animation speed based on player movement
        float animationSpeed = Mathf.Abs(playerBody.linearVelocity.magnitude) / (isSprinting ? sprintSpeed : moveSpeed); // Calculate the animation speed based on player velocity and movement speed
        animationSpeed *= maxAnimationSpeed;
        animationSpeed = Mathf.Clamp(animationSpeed, minAnimationSpeed, maxAnimationSpeed); // Clamp the animation speed to ensure it doesn't exceed the maximum value
        if (!Mathf.Approximately(animationSpeed, lastAnimSpeed)) {
            playerAnimator.SetFloat("animSpeed", animationSpeed);
            lastAnimSpeed = animationSpeed;
        }

        // Run the Update methods from other parts of the partial class
        DamageUpdate();
    }

    private void DialogStarted(DialogData data)
    {
        isInDialog = true;
        playerBody.linearVelocityX = 0f;
    }

    private void DialogEnded(DialogData data)
    {
        isInDialog = false;
    }

    /// <summary>
    /// Updates the animator states based on the player's movement input and current conditions.
    /// </summary>
    /// <remarks>This method adjusts the player's animation states such as ducking, skipping, jumping, and
    /// climbing by setting the appropriate animator parameters. It also manages the player's colliders and gravity
    /// scale when climbing.</remarks>
    private void UpdateAnimatorStates()
    {
        // Only update animator if state changed
        if (canClimb && (isClimbing || moveInput.y < 0.0f) && !isJumping) {
            playerBody.gravityScale = 0f;
            playerBody.MovePosition(playerBody.position + new Vector2(moveInput.x * Time.fixedDeltaTime, climbSpeed * Time.fixedDeltaTime * (isClimbing ? 1 : -1)));
            if (!lastIsClimbing) {
                playerAnimator.SetBool("isClimbing", true);
                lastIsClimbing = true;
            }
        } else if (canClimb && lastIsClimbing && !isGrounded && !isJumping) {
            playerBody.gravityScale = 0f;
            playerBody.MovePosition(playerBody.position + new Vector2(moveInput.x * Time.fixedDeltaTime, 0f));
            return;
        } else if (lastIsClimbing) {
            playerBody.gravityScale = 1f;
            playerAnimator.SetBool("isClimbing", false);
            lastIsClimbing = false;
        }

        bool isDucking = moveInput.y < 0.0f && !lastIsClimbing;
        if (isDucking != lastIsDucking) {
            playerAnimator.SetBool("isDucking", isDucking);
            lastIsDucking = isDucking;
            crouchCollider.enabled = isDucking;
            playerCollider.enabled = !isDucking;
        }

        bool isSkipping = moveInput.x != 0.0f;
        if (isSkipping != lastIsSkipping) {
            playerAnimator.SetBool("isSkipping", isSkipping);
            lastIsSkipping = isSkipping;
        }

        bool isJumpingAnim = (playerBody.linearVelocityY > 0.0f && !isGrounded);
        if (!jumpLock && isJumpingAnim != lastIsJumping) {
            playerAnimator.SetBool("isJumping", isJumpingAnim);
            lastIsJumping = isJumpingAnim;
        }

    }

    /// <summary>
    /// Flips the sprite horizontally and adjusts related components to match the new orientation.
    /// </summary>
    /// <remarks>This method toggles the sprite's flipped state by rotating it 180 degrees around the Y-axis. 
    /// It also ensures that the camera and other related components are updated to align with the  new orientation. If
    /// the player's velocity exceeds a certain threshold, a force is applied  to reduce the velocity after
    /// flipping.</remarks>
    public void FlipSprite()
    {
        if (!isFlipped) {
            Vector3 rotation = new(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
            isFlipped = true; // Set the flipped state to true

            // Turn the cameraFollowObject to match the player's orientation
            cameraFollow.CallTurn(); // Call the method to flip the camera's Y rotation
        } else {
            Vector3 rotation = new(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotation); // Restore the sprite to its original orientation
            isFlipped = false; // Set the flipped state to false

            // Turn the cameraFollowObject to match the player's orientation
            cameraFollow.CallTurn(); // Call the method to flip the camera's Y rotation
        }
        if (Mathf.Abs(playerBody.linearVelocity.magnitude) > maxPlayerVelocity * 0.75f) {
            playerBody.AddForce(-playerBody.linearVelocity * 0.25f, ForceMode2D.Impulse); // Apply a force to the player to slow down after flipping
        }
    }

    /// <summary>
    /// Determines whether the object is currently grounded by checking for collisions within a specified area.
    /// </summary>
    /// <remarks>This method uses a circular overlap check to detect collisions with objects in the specified
    /// ground layer. Ensure that <see cref="groundCheck"/>, <see cref="groundCheckRadius"/>, and <see
    /// cref="groundLayer"/> are properly configured before calling this method.</remarks>
    private void CheckGrounded()
    {
        //Vector2 origin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        //RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckRadius, groundLayer);
        //isGrounded = hit.collider != null;

        isGrounded = Physics2D.IsTouchingLayers(groundCheck == null ? playerCollider : groundCheck, groundLayer);

        if (!isGrounded && jumpLock) // If the player is not grounded and jump lock is active, reset jump lock
        {
            jumpLock = false; // Reset jump lock to allow jumping again
            StopCoroutine(jumpLockCoroutine);
            return; // Exit the method to prevent further processing
        } else if (jumpLock) {
            return; // If jump lock is active, exit the method to prevent further processing
        }
        playerAnimator.SetBool("isGrounded", isGrounded); // Set the "isGrounded" animation parameter
        if (isJumping && isGrounded) // If the player is jumping and is grounded
        {
            playerAnimator.SetBool("isJumping", false); // Set the "isJumping" animation parameter to false
            isJumping = false; // Reset jumping state
            doubleJumping = false; // Reset double jumping state
        }
    }

    /// <summary>
    /// Disables all player actions and stops player movement.
    /// </summary>
    /// <remarks>This method disables key player input actions, resets the player's velocity,  and deactivates
    /// the <see cref="PlayerController"/> component to ensure the player  can no longer perform any actions or move.
    /// Use this method to handle scenarios  where the player character needs to be effectively "removed" from gameplay,
    /// such  as upon death or game over.</remarks>
    public void Die()
    {
        moveAction.Disable(); // Disable the move action to stop player movement
        interactAction.Disable(); // Disable the interact action to stop player interactions
        jumpAction.Disable(); // Disable the jump action to stop player jumping
        sprintAction.Disable(); // Disable the sprint action to stop player sprinting
        playerBody.linearVelocity = Vector2.zero; // Reset player velocity to zero
        //this.GetComponent<PlayerControllerV1>().enabled = false; // Disable the PlayerController script to stop all player actions
    }

    /// <summary>
    /// Resets the teleportation cooldown and disables the ability to teleport.
    /// </summary>
    /// <remarks>This method should be called after a teleportation event to ensure the teleportation cooldown
    /// is enforced. The ability to teleport will remain disabled until the cooldown period has elapsed.</remarks>
    public void Teleported()
    {
        teleportTimer = teleportCooldown;
        canTeleport = false; // Set the teleported state to true to indicate the player has been teleported
    }

    public void SetFlippedState(bool flipped)
    {
        isFlipped = flipped;
    }
}
