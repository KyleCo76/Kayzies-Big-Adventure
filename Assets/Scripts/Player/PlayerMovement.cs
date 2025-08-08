using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerControllerV3 : MonoBehaviour
{
    // Player Movement Settings
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Speed of player movement.")]
    [SerializeField] private float moveSpeed = 5f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Speed of player sprinting.")]
    [SerializeField] private float sprintSpeed = 8f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Maximum velocity of the player while skipping.")]
    [SerializeField] private float maxPlayerVelocity = 4.0f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Maximum velocity of the player while sprinting.")]
    [SerializeField] private float maxPlayerSprintVelocity = 6.0f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Maximum velocity of the player while crouching.")]
    [SerializeField] private float maxCrouchVelocity = 3.0f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Reduction in movement speed while in the air or ducking.")]
    [SerializeField] private float airAndDuckMovementReduction = 6.0f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Maximum vertical velocity of the player.")]
    [SerializeField] private float maxVerticalVelocity = 10.0f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Amount of extra force applied to the player when moving from a standstill.")]
    [SerializeField] private float playerVelocityBoost = 2.0f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Range for player velocity boost.")]
    [SerializeField] private float playerVelocityBoostRange = 0.5f;
    [FoldoutGroup("Movement Settings")]
    [Tooltip("Amount of false friction applied to the player when coming to a stop.")]
    [SerializeField] private float falseFrictionValue = 5f;

    [FoldoutGroup("Jump Settings")]
    [Tooltip("Force applied when jumping.")]
    [SerializeField] private float jumpForce = 5f;
    [FoldoutGroup("Jump Settings")]
    [Tooltip("Stamina cost for jumping. Doubled for double jump")]
    [SerializeField] private float jumpStaminaCost = 5.0f;
    [FoldoutGroup("Jump Settings")]
    [Tooltip("Maximum vertical velocity considered for a quick jump.")]
    [SerializeField] private float quickJumpMaxVelocity = 7f;
    [FoldoutGroup("Jump Settings"), SerializeField, Tooltip("Minimum vertical velocity considered for quick jump.")]
    private float quickJumpMinVelocity = 3.5f;

    [Tooltip("Speed of player climbing.")]
    [SerializeField] private float climbSpeed = 3f;
    private bool isJumping = false; // Tracks if the player is currently jumping
    private bool jumpLock = false; // Prevents the isGrounded state from being set to true while starting a jump
    public float JumpForce { set { jumpForce = value; } get => jumpForce; }
    private bool doubleJumping = false;
    private Coroutine jumpLockCoroutine; // Coroutine for jump lock timer

    [HideInInspector]
    public bool onPlatform = false;
    //[HideInInspector]
    public Collider2D platformCollider = null; // Collider of the platform the player is currently on


    /// <summary>
    /// Handles the input action for sprinting, toggling the sprinting state based on the input context.
    /// </summary>
    /// <remarks>When the sprint action is performed, the sprinting state is activated. If the action is
    /// canceled, sprinting is deactivated.</remarks>
    /// <param name="context">The input action context that provides information about the sprint action, such as whether it was performed or
    /// canceled.</param>
    private void OnSprint(InputAction.CallbackContext context)
    {
        if (isInDialog) return; // Ignore sprint input if the player is in a dialog 
        if (context.performed && !lastIsDucking) // Check if the sprint action was performed
        {
            sprintingPressed = true;
        } else// if (context.canceled) // Check if the sprint action was canceled
        {
            sprintingPressed = false;
            isSprinting = false;
        }
    }

    /// <summary>
    /// Determines whether the player can sprint based on current velocity, stamina, and health status.
    /// </summary>
    /// <remarks>This method evaluates the player's ability to sprint by checking the current velocity against
    /// maximum sprint velocity, the player's stamina, and invincibility status. It adjusts the horizontal movement
    /// input and calculates any additional force needed for acceleration.</remarks>
    /// <param name="moveInputX">The horizontal movement input to be adjusted based on sprinting conditions.</param>
    /// <param name="forceToApply">The additional force to apply for acceleration if the player is starting to move.</param>
    private void CanSprint(out float moveInputX, out float forceToApply)
    {
        moveInputX = Mathf.Abs(playerBody.linearVelocityX) > maxPlayerVelocity ? 0 : moveInput.x;
        //if ((isSprinting || (sprintingPressed && currentStamina > maxStamina / 10)) && !playerHealthManager.IsInvincible) {
        //    if ((Mathf.Abs(playerBody.linearVelocityX) < maxPlayerSprintVelocity) && currentStamina > 0f) {
        //        moveInputX = moveInput.x;
        //        isSprinting = true;
        //    } else {
        //        moveInputX = 0f; // Check if the player is sprinting and adjust horizontal input accordingly
        //        if (currentStamina <= 0f) isSprinting = false;
        //    }
        //} else {
        //    isSprinting = false; // Set the sprinting state to false
        //}
        forceToApply = 0f;
        // Apply additional force if the player is beginning to move to increase acceleration
        if (Mathf.Abs(playerBody.linearVelocityX) < playerVelocityBoostRange && !Mathf.Approximately(moveInputX, 0f)) {
            forceToApply = playerVelocityBoost * moveInputX;
        }
    }

    /// <summary>
    /// Applies movement forces to the player character based on input and current state.
    /// </summary>
    /// <remarks>This method calculates and applies forces to the player character's body to simulate
    /// movement. It considers the player's current velocity, whether the player is grounded, and if the player is
    /// sprinting or crouching. The method ensures that the player's velocity does not exceed predefined limits and
    /// applies friction when necessary.</remarks>
    /// <param name="horizontalInput">The horizontal input value, typically from user input, which determines the direction and magnitude of the force
    /// applied.</param>
    /// <param name="moveForce">An additional force to be applied, which can be used to modify the player's movement behavior.</param>
    protected void ApplyForces(float horizontalInput, float moveForce)
    {
        if (playerBody.linearVelocityX > maxPlayerSprintVelocity || lastIsClimbing) {
            return;
        }
        Vector2 force = Vector2.zero; // Initialize force to zero
        if (isGrounded) {
            // Check for crouching or ducking state
            if (moveInput.y < -0.1f) {
                if (playerBody.linearVelocityX > maxCrouchVelocity) {
                    return; // Prevent movement if the player is moving too fast while crouching
                }
                force = (((horizontalInput * (moveSpeed) - airAndDuckMovementReduction) + moveForce) * Vector2.right);
            }
            // Check for false friction when the player is not moving but is sliding
            else if (Mathf.Approximately(Mathf.Abs(moveInput.x), 0f) && Mathf.Abs(playerBody.linearVelocityX) > 0.1f) {
                playerBody.linearVelocityX = Mathf.Lerp(playerBody.linearVelocityX, 0f, Time.deltaTime * falseFrictionValue); // Slow down the player when not moving
            }
            // If still moving, apply movement forces based on player velocity
            else if (Mathf.Abs(horizontalInput) > 0.1f) {
                if (!isSprinting && Mathf.Abs(playerBody.linearVelocityX) > maxPlayerVelocity) {
                    return;
                }
                force = ((horizontalInput * (isSprinting ? sprintSpeed : moveSpeed) + moveForce) * Vector2.right);
            }
        }
        // Default to no forces
        else if (Mathf.Abs(horizontalInput) > 0.1f) {
            force = (((horizontalInput * (moveSpeed) - airAndDuckMovementReduction) + moveForce) * Vector2.right);
        } else {
            return;
        }

        playerBody.AddForce(force, ForceMode2D.Force); // Apply the calculated force to the player body
    }

    /// <summary>
    /// Handles the player's jump action based on the current state.
    /// </summary>
    /// <remarks>If the player is already jumping, this method initiates a double jump.  If the player is not
    /// grounded, the method exits without performing any action. Otherwise, it sets the appropriate animation
    /// parameters, updates the jumping state,  and locks the grounded state to prevent animation conflicts.</remarks>
    /// <param name="context">The context of the input action triggering the jump.</param>
    private void OnJump(InputAction.CallbackContext context)
    {
        if (isInDialog) return; // Ignore jump input if the player is in a dialog
        if (isJumping) { DoubleJump(); return; } // If the player is already jumping, call DoubleJump
        if (!isGrounded && !lastIsClimbing) return; // Quit if the player is not grounded
        if (currentStamina < jumpStaminaCost) return; // Quit if the player does not have enough stamina to jump
        if (onPlatform && moveInput.y < 0f) { JumpDown(); return; } // Quit if the player is on a platform and trying to jump down

        currentStamina -= jumpStaminaCost; // Reduce the player's stamina by the jump cost
        isJumping = true; // Set jumping state to true
        jumpLock = true; // Lock out the isGrounded state to prevent animation issues
        jumpLockCoroutine = StartCoroutine(JumpLockTimer()); // Start a coroutine to reset the jump lock after a short delay
        playerAnimator.SetBool("isJumping", true); // Set the "isJumping" animation parameter to true
        playerAnimator.SetTrigger("Jump"); // Trigger the jump animation
        return; // Exit the method to prevent further processing
    }

    private void JumpDown()
    {
        if (!onPlatform || !platformCollider) return; // Quit if the player is not on a platform or the platform collider is null
        StartCoroutine(DropThroughPlatform());
    }

    private IEnumerator DropThroughPlatform()
    {
        Collider2D droppedPlatform = platformCollider; // Store the platform collider to drop through
        Physics2D.IgnoreCollision(crouchCollider, droppedPlatform, true); // Ignore collision with the platform
        Physics2D.IgnoreCollision(playerCollider, droppedPlatform, true); // Also catch the normal collider to prevent collision issues
        yield return new WaitForSeconds(1f); // Wait for a short duration to allow the player to drop through
        Physics2D.IgnoreCollision(crouchCollider, droppedPlatform, false); // Re-enable collision with the platform
        Physics2D.IgnoreCollision(playerCollider, droppedPlatform, false); // Also re-enable collision with the normal collider

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

    /// <summary>
    /// Temporarily locks the ability to jump for a short duration.
    /// </summary>
    /// <remarks>This method enforces a cooldown period during which jumping is disabled.  It is typically
    /// used to prevent rapid consecutive jumps.</remarks>
    /// <returns>An enumerator that can be used to control the timing of the jump lock reset.</returns>
    private IEnumerator JumpLockTimer()
    {
        yield return new WaitForSeconds(0.3f); // Wait for a short duration to allow the jump lock to reset
        jumpLock = false; // Reset the jump lock state to allow jumping again
    }

    /// <summary>
    /// Flips the sprite horizontally by inverting its local scale along the X-axis.
    /// </summary>
    /// <remarks>This method toggles the flipped state of the sprite. If the sprite is currently flipped,  it
    /// will be restored to its original orientation. If the sprite is not flipped, it will  be inverted horizontally.
    /// The flipped state is determined by the <c>isFlipped</c> field.</remarks>
    private void DoubleJump()
    {
        if (doubleJumping) { return; } // Quit if the player is not moving upwards (to prevent double jump when falling)
        if (currentStamina < jumpStaminaCost * 2) { return; } // Quit if the player does not have enough stamina to double jump
        currentStamina -= jumpStaminaCost * 2; // Reduce the player's stamina by the double jump cost
        doubleJumping = true;
        if (playerBody.linearVelocityY > quickJumpMinVelocity && playerBody.linearVelocityY < quickJumpMaxVelocity) {
            StartCoroutine(DoubleJumpForce(true));
            Debug.Log("DoublJump Quick Jump");
        } else {
            StartCoroutine(DoubleJumpForce(false));
        }
    }

    private IEnumerator DoubleJumpForce(bool quickJump)
    {
        if (!quickJump) {
            playerBody.linearVelocityY = 0f;
        }
        yield return null;
        if (quickJump) {
            playerBody.AddForce(((jumpForce + (Mathf.Abs(playerBody.linearVelocityY))) / 2) * Vector2.up, ForceMode2D.Impulse);
        } else {
            playerBody.AddForce(jumpForce * 2 * Vector2.up, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Adjusts the player's movement speed by applying a multiplier to the current speed.
    /// </summary>
    /// <remarks>This method updates the player's movement speed and adjusts the associated animation speed 
    /// parameter to reflect the new movement speed relative to the base speed.</remarks>
    /// <param name="moveSpeedMultiplier">The multiplier to apply to the current movement speed. Must be a positive value.</param>
    public void ChangeMoveSpeed(float moveSpeedMultiplier)
    {
        moveSpeed *= moveSpeedMultiplier; // Adjust the player's movement speed based on the multiplier
        sprintSpeed *= moveSpeedMultiplier; // Adjust the sprint speed based on the multiplier
        playerAnimator.SetFloat("animSpeed", (isSprinting && currentStamina > 0f ? sprintSpeed : moveSpeed) / baseMoveSpeed); // Update the "moveSpeed" animation parameter
    }

    /// <summary>
    /// Temporarily modifies the player's movement speed by applying a multiplier for a specified duration.
    /// </summary>
    /// <remarks>This method adjusts the player's movement speed and updates the associated animation speed
    /// parameter.  After the specified duration, the movement speed is automatically restored to its original
    /// value.</remarks>
    /// <param name="moveSpeedMultiplier">The factor by which the player's movement speed is multiplied. Must be greater than 0.</param>
    /// <param name="duration">The duration, in seconds, for which the modified movement speed is applied. Must be greater than 0.</param>
    public void ChangeMoveSpeed(float moveSpeedMultiplier, float duration)
    {
        if (duration <= 0) // Check if the duration is valid
        {
            ChangeMoveSpeed(moveSpeedMultiplier); // If duration is not specified, apply the multiplier permanently
            return; // Exit if permanent change is desired
        }
        float originalMoveSpeed = moveSpeed; // Store the original movement speed before changes
        float originalSprintSpeed = sprintSpeed; // Store the original sprint speed before changes
        moveSpeed *= moveSpeedMultiplier; // Adjust the player's movement speed based on the multiplier
        sprintSpeed *= moveSpeedMultiplier; // Adjust the sprint speed based on the multiplier
        playerAnimator.SetFloat("animSpeed", (isSprinting && currentStamina > 0f ? sprintSpeed : moveSpeed) / baseMoveSpeed); // Update the "moveSpeed" animation parameter
        StartCoroutine(TempMoveSpeed(originalMoveSpeed, originalSprintSpeed, duration)); // Start a coroutine to restore the speed after the specified duration
    }

    /// <summary>
    /// Adjusts the player's jump force by applying the specified multiplier.
    /// </summary>
    /// <remarks>This method modifies the player's jump force dynamically, allowing for changes in jump height
    /// based on gameplay mechanics or external conditions. Ensure the multiplier is greater than zero  to avoid
    /// unintended behavior.</remarks>
    /// <param name="jumpForceMultiplier">The factor by which to multiply the current jump force. Must be a positive value.</param>
    public void ChangeJumpForce(float jumpForceMultiplier)
    {
        jumpForce *= jumpForceMultiplier; // Adjust the player's jump force based on the multiplier
    }

    /// <summary>
    /// Temporarily modifies the player's jump force by applying a multiplier for a specified duration.
    /// </summary>
    /// <remarks>After the specified duration, the jump force is automatically restored to its original
    /// value.</remarks>
    /// <param name="jumpForceMultiplier">The factor by which to multiply the current jump force. Must be greater than 0.</param>
    /// <param name="duration">The duration, in seconds, for which the modified jump force will be applied. Must be greater than 0.</param>
    public void ChangeJumpForce(float jumpForceMultiplier, float duration)
    {
        if (duration <= 0) // Check if the duration is valid
        {
            ChangeJumpForce(jumpForceMultiplier); // If duration is not specified, apply the multiplier permanently
            return; // Exit if permanent change is desired
        }
        float originalJumpForce = jumpForce; // Store the original jump force before changes
        jumpForce *= jumpForceMultiplier; // Adjust the player's jump force based on the multiplier
        StartCoroutine(TempStat(originalJumpForce, duration, v => jumpForce = v)); // Start a coroutine to restore the jump force after the specified duration
    }

    /// <summary>
    /// Temporarily modifies the player's movement and sprint speeds for a specified duration.
    /// </summary>
    /// <remarks>This method temporarily changes the player's movement and sprint speeds and restores them to
    /// their original values  after the specified duration. It also updates the player's animation speed based on the
    /// restored values.</remarks>
    /// <param name="originalMoveSpeed">The original movement speed to restore after the duration ends.</param>
    /// <param name="originalSprintSpeed">The original sprint speed to restore after the duration ends.</param>
    /// <param name="duration">The duration, in seconds, for which the temporary speed modification is applied.</param>
    /// <returns>An enumerator that handles the timing of the speed restoration.</returns>
    private IEnumerator TempMoveSpeed(float originalMoveSpeed, float originalSprintSpeed, float duration)
    {
        yield return new WaitForSeconds(duration); // Wait for the specified duration
        moveSpeed = originalMoveSpeed; // Restore the movement speed to its original value
        sprintSpeed = originalSprintSpeed; // Restore the sprint speed to its original value
        playerAnimator.SetFloat("animSpeed", (isSprinting && currentStamina > 0f ? sprintSpeed : moveSpeed) / baseMoveSpeed); // Update the animation speed based on the restored speed
    }

}
