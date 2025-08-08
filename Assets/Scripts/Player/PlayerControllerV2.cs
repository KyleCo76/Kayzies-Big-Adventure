using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kayzie.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CircleCollider2D))]
    [RequireComponent(typeof(CapsuleCollider2D), typeof(BoxCollider2D))]
    public partial class PlayerControllerV2 : HealthFunctions
    {

        [FoldoutGroup("Ground Settings"), SerializeField, Tooltip("Layers that are considered ground")]
        private LayerMask groundLayers;

        [FoldoutGroup("Animation Settings"), SerializeField, Range(0.1f, 10f), Tooltip("Minimum animation speed for player movement")]
        private float minAnimationSpeed = 0.5f;
        [FoldoutGroup("Animation Settings"), SerializeField, Range(0.1f, 10f), Tooltip("Maximum animation speed for player movement")]
        private float maxAnimationSpeed = 2.0f;

        [FoldoutGroup("Followers"), SerializeField, Tooltip("Array of follower transforms to be updated with the player's position.")]
        private List<Transform> followers;

        // Followers Property
        public List<Transform> Followers
        {
            get => followers;
            set => followers = value;
        }

        // Components
        private Rigidbody2D playerBody;
        private Animator playerAnimator;
        private CircleCollider2D crouchCollider;
        private CompositeCollider2D playerCollider;
        private BoxCollider2D groundCheckCollider;
        private BoxCollider2D attackCollider;
        //private PlayerHealthManager healthManager;
        private CameraFollowObject cameraFollow;
        private DialogManager dialogManagerInstance;

        // Dialog Variables
        private bool isInDialog;

        // Camera Follow Settings
        private float fallSpeedYDampingChangeThreshold; // Threshold for changing camera damping based on fall speed

        // Animation Variables
        private float lastAnimSpeed = 1.0f; // Last animation speed to avoid unnecessary updates

        // Ground check settings
        private bool isGrounded;
        private bool groundCheckResult;

        // Flipping settings
        private bool isFlipped;
        public bool IsFlipped => isFlipped; // Public property to access the flipped state
        private readonly float flipDamperMulti = 0.75f; // Percent of walk velocity considered for applying a reverse force after flipping the sprite

        // Teleportation settings
        private readonly float teleportCooldown = 5f; // Cooldown time for teleportation
        private float teleportTimer;
        private bool canTeleport = true; // Flag to check if teleportation is available
        public bool CanTeleport => canTeleport; // Public property to access the teleportation state

        // Interaction settings
        private bool isInteracting;
        public bool IsInteracting => isInteracting;

        // Climbing settings
        private bool canClimb;
        public bool CanClimb { get => canClimb; set { canClimb = value; } }


        private void OnEnable()
        {
            GameManager.OnMoveAction += HandleMoveAction;
            GameManager.OnJumpAction += HandleJumpAction;
            GameManager.OnAttackAction += HandleAttackAction;
            GameManager.OnInteractAction += HandleInteractAction;
            GameManager.OnSprintAction += HandleSprintAction;
            GameManager.OnAttackAction += HandleAttackAction;
            Managers.Instance.SaveGameManager.OnSaveGame += GameSaved;
            Managers.Instance.SaveGameManager.OnLoadGame += GameLoaded;
            dialogManagerInstance.OnDialogStart += DialogStarted;
            dialogManagerInstance.OnDialogEnd += DialogEnded;

        }

        private void OnDisable()
        {
            GameManager.OnMoveAction -= HandleMoveAction;
            GameManager.OnJumpAction -= HandleJumpAction;
            GameManager.OnAttackAction -= HandleAttackAction;
            GameManager.OnInteractAction -= HandleInteractAction;
            GameManager.OnSprintAction -= HandleSprintAction;
            GameManager.OnAttackAction -= HandleAttackAction;
            Managers.Instance.SaveGameManager.OnSaveGame -= GameSaved;
            Managers.Instance.SaveGameManager.OnLoadGame -= GameLoaded;
            dialogManagerInstance.OnDialogStart -= DialogStarted;
            dialogManagerInstance.OnDialogEnd -= DialogEnded;
        }

        private void Awake()
        {
            // Initialize components
            if (!TryGetComponent<Rigidbody2D>(out playerBody))
                Debug.LogError("Rigidbody2D component is missing from the player GameObject.");

            if (!TryGetComponent<Animator>(out playerAnimator))
                Debug.LogError("Animator component is missing from the player GameObject.");

            if (!TryGetComponent<CircleCollider2D>(out crouchCollider))
                Debug.LogError("CircleCollider2D component is missing from the player GameObject.");

            if (!TryGetComponent<CompositeCollider2D>(out playerCollider))
                Debug.LogError("CapsuleCollider2D component is missing from the player GameObject.");

            if (!transform.Find("GroundCheck").TryGetComponent<BoxCollider2D>(out groundCheckCollider))
                Debug.LogError("Ground Check collider is missing from the player GameObject");

            if (!transform.Find("DamageCollider").TryGetComponent<BoxCollider2D>(out attackCollider))
                Debug.LogError("Damage collider is missing from the player GameObject");
            cameraFollow = FindAnyObjectByType<CameraFollowObject>();
            if (cameraFollow == null)
                Debug.LogError("CameraFollowObject not found in the scene.");
            dialogManagerInstance = FindAnyObjectByType<DialogManager>();
            if (dialogManagerInstance == null)
                Debug.LogError("DialogManager not found in the scene.");
            GameObject healthBarParent = GameObject.FindGameObjectWithTag("HealthBarParent");
            healthBar = healthBarParent.GetComponentInChildren<Image>();
            if (healthBar == null)
                Debug.LogError("Health bar not found in the scene.");
            healthBarText = healthBarParent.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (healthBarText == null)
                Debug.LogError("Health bar text not found in the scene.");

            fallSpeedYDampingChangeThreshold = Managers.Instance.CameraManager.fallSpeedYDampingChangeThreashold; // Get the fall speed damping change threshold from CameraManagger

            playerAnimator.SetFloat("animSpeed", 1.0f); // Set the animation speed to an intial value
            teleportTimer = teleportCooldown; // Initialize the teleport cooldown timer
            //baseMoveSpeed = accelerationForce; // Store the base movement speed for later use
            crouchCollider.enabled = false; // Disable the crouch collider initially

            AwakeStamina();

            GameObject okto = GameObject.FindGameObjectWithTag("Okto");
            if (okto != null) {
                followers.Add(okto.transform); // We always add the Okto follower to the followers list
            }
        }

        void Start()
        {
            StartDamage();
            StartHealth();
            StartMovement();
        }

        void Update()
        {

            if (GameManager.isPaused) return;

            StaminaManager(Mathf.Abs(moveInput.x) > 0.1f);

            if (isInDialog) return;

            isClimbing = moveInput.y > 0.1f;
            isDucking = moveInput.y < -0.1f;

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
            if (playerBody.linearVelocityY > maxVerticalJumpVelocity) {
                playerBody.linearVelocityY = maxVerticalJumpVelocity;
            }

            // Update the animation speed based on player movement
            float animationSpeed = Mathf.Abs(playerBody.linearVelocity.magnitude) / (isSprinting ? sprintAccelerationForce : accelerationForce); // Calculate the animation speed based on player velocity and movement speed
            animationSpeed *= maxAnimationSpeed;
            animationSpeed = Mathf.Clamp(animationSpeed, minAnimationSpeed, maxAnimationSpeed); // Clamp the animation speed to ensure it doesn't exceed the maximum value
            if (!Mathf.Approximately(animationSpeed, lastAnimSpeed)) {
                playerAnimator.SetFloat("animSpeed", animationSpeed);
                lastAnimSpeed = animationSpeed;
            }

            // Run the Update methods from other parts of the partial class
            UpdateDamage();
            UpdateHealth();

        }

        private void FixedUpdate()
        {
            CheckGrounded();
            FixedUpdateMovement();

            if (moveInput.x < 0.0f && !isFlipped) {
                FlipSprite();
            } else if (moveInput.x > 0.0f && isFlipped) {
                FlipSprite();
            }

            UpdateAnimatorStates();
        }

        private void GameLoaded(SaveGameData[] saveData)
        {
            if (saveData == null || saveData.Length == 0) {
                Debug.LogError("No Save Data Found");
                return;
            }

            foreach (SaveGameData data in saveData)
            {
                if (data.CharacterName == this.name)
                {
                    transform.SetPositionAndRotation(new Vector3(data.PositionX, data.PositionY, data.PositionZ), new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW));
                    currentHealth = data.Health;
                    maxHealth = data.MaxHealth;
                    currentStamina = data.Stamina;
                    maxStamina = data.MaxStamina;
                    this.GetComponent<CollectableManager>().SetCollectedItems(data.InventoryItems);
                    isInvincible = data.IsInvicible;
                    playerBody.linearVelocity = Vector2.zero; // Reset velocity to prevent unwanted movement
                    playerAnimator.SetFloat("animSpeed", 1.0f); // Reset animation speed
                    playerAnimator.SetBool("isGrounded", false); // Set grounded state
                    playerAnimator.SetBool("isClimbing", false); // Reset climbing state
                    playerAnimator.SetBool("isJumping", false); // Reset jumping state
                    playerAnimator.SetBool("isDucking", false); // Reset ducking state
                    playerAnimator.SetBool("isSkipping", false); // Reset skipping state
                    Debug.Log($"Loaded game with name: {data.SaveName}");
                    Managers.Instance.SaveGameManager.ObjectsManaged++;
                    return;
                }
            }
        }

        private void GameSaved(string saveName)
        {
            SaveGameData saveData = new()
            {
                SaveName = saveName,
                CharacterName = gameObject.name,
                PositionX = transform.position.x,
                PositionY = transform.position.y,
                PositionZ = transform.position.z,
                RotationX = transform.rotation.x,
                RotationY = transform.rotation.y,
                RotationZ = transform.rotation.z,
                RotationW = transform.rotation.w,
                InventoryItems = this.GetComponent<CollectableManager>().GetInventoryAsJson(), // Assuming PlayerInventory has a method to get inventory as JSON
                Health = Mathf.RoundToInt(currentHealth),
                MaxHealth = Mathf.RoundToInt(maxHealth),
                Stamina = Mathf.RoundToInt(currentStamina),
                MaxStamina = Mathf.RoundToInt(maxStamina),
                IsInvicible = isInvincible
            };
            Managers.Instance.SaveGameManager.SaveObjectData(saveData);
        }

        private void HandleAttackAction()
        {
            OnAttack();
        }

        private void HandleInteractAction(bool performed)
        {
            isInteracting = performed;
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
        /// Disables the climbing state for the player.
        /// </summary>
        /// <remarks>This method re-enables gravity for the player, updates the climbing state to indicate that
        /// the player can no longer climb, and resets the climbing animation.</remarks>
        public void CannotClimb()
        {
            playerBody.gravityScale = 1f; // Re-enable gravity when not climbing
            canClimb = false; // Set the climbing state to false
            playerAnimator.SetBool("isClimbing", false); // Set the "isClimbing" animation parameter to false
        }

        private void CheckGrounded()
        {
            groundCheckResult = Physics2D.IsTouchingLayers(groundCheckCollider == null ? playerCollider : groundCheckCollider, groundLayers);

            if (jumpLock) return; // If jump lock is active, exit the method to prevent further processing
            
            if (groundCheckResult != isGrounded) {
                isGrounded = groundCheckResult;
                playerAnimator.SetBool("isGrounded", isGrounded); // Set the "isGrounded" animation parameter
            }
            // Reset jump state
            if (isJumping && isGrounded)
            {
                playerAnimator.SetBool("isJumping", false); // Set the "isJumping" animation parameter to false
                isJumping = false; // Reset jumping state
                doubleJumping = false; // Reset double jumping state
            }
        }

        public void Die()
        {
            OnDisable(); // Unsubscribe from all events to prevent further processing
            playerBody.linearVelocity = Vector2.zero; // Reset player velocity to zero
            this.GetComponent<PlayerControllerV2>().enabled = false; // Disable the PlayerController script to stop all player actions
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
            if (Mathf.Abs(playerBody.linearVelocity.magnitude) > maxWalkVelocity * flipDamperMulti) {
                playerBody.AddForce(-playerBody.linearVelocity * 0.25f, ForceMode2D.Impulse); // Apply a force to the player to slow down after flipping
            }
        }

        /// <summary>
        /// Sets the flipped state of the object.
        /// </summary>
        /// <param name="flipped">A <see langword="bool"/> value indicating the desired flipped state. <see langword="true"/> to set the
        /// object as flipped; otherwise, <see langword="false"/>.</param>
        public void SetFlippedState(bool flipped)
        {
            isFlipped = flipped;
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

        private void UpdateAnimatorStates()
        {
            // Only update animator if state changed
            if (canClimb && (isClimbing || moveInput.y < 0.0f) && !isJumping) {
                playerBody.gravityScale = 0f;
                playerBody.MovePosition(playerBody.position + new Vector2(moveInput.x * Time.fixedDeltaTime, climbForce * Time.fixedDeltaTime * (isClimbing ? 1 : -1)));
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

    }
}
