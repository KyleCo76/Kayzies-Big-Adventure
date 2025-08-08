using Sirenix.OdinInspector;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Kayzie.Player
{
    public partial class PlayerControllerV2 : HealthFunctions
    {

        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Acceleration force applied to the player when moving")]
        private float accelerationForce = 9f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Acceleration force applied to player when sprinting")]
        private float sprintAccelerationForce = 15f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Deceleration force applied to the player when stopping")]
        private float decelerationForce = 9f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Maximum speed the player can reach when walking")]
        private float maxWalkVelocity = 5f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Maximum speed the player can reach when sprinting")]
        private float maxSprintVelocity = 10f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Maximum horizontal velocity to apply acceleration boost from 0")]
        private float maxAccelerationBoostVelocity = 2f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Amount of extra force to apply when within maxAccelerationBoostVelocity")]
        private float accelerationBoostForce = 2f;
        [FoldoutGroup("Movement Settings"), SerializeField, Range(0f, 1f), Tooltip("Movement Reduction Percentage while in air and ducking. Expected values are 0-1")]
        private float duckMovementReduction = 0.5f;
        [FoldoutGroup("Movement Settings"),SerializeField, Range(0f, 1f), Tooltip("Movement Reduction Percentage while in air.")]
        private float airMovementReduction = 0.1f;
        [FoldoutGroup("Movement Settings"), SerializeField, Tooltip("Max velocity the player can reach while ducking")]
        private float maxCrouchVelocity = 2.5f;


        [FoldoutGroup("Jump Settings"), SerializeField, Tooltip("Force applied to the player when jumping")]
        private float jumpForce = 10f;
        [FoldoutGroup("Jump Settings"), SerializeField, Tooltip("Multiplier to apply to jumpForce when double jumping")]
        private float doubleJumpMulti = 1.2f;
        [FoldoutGroup("Jump Settings"), SerializeField, Tooltip("Max Vertical Velocity the player can reach when jumping")]
        private float maxVerticalJumpVelocity = 10f;
        [FoldoutGroup("Jump Settings"), SerializeField, Tooltip("Force applied to the player when double jumping")]
        private float2 quickJumpVelocityRange = new(5f, 10f);

        [FoldoutGroup("Climb Settings"), SerializeField, Tooltip("Force applied to the player when climbing")]
        private float climbForce = 5f;

        [FoldoutGroup("Stamina Settings"), SerializeField, Tooltip("Stamina cost of jumping")]
        private float jumpStaminaCost = 10f;
        [FoldoutGroup("Stamina Settings"), SerializeField, Tooltip("Stamina cost of double jumping")]
        private float doubleJumpStaminaCost = 5f;
        [FoldoutGroup("Stamina Settings"), SerializeField, Tooltip("Minimum percent of stamina the player must have to begin sprinting. Valid values are 0-1")]
        private float minSprintStaminaPercent = 0.2f;

        // Movement Variables
        private Vector2 moveInput = Vector2.zero;
        private bool lastIsSkipping = false;
        private float baseAcceleration;

        // Sprinting Variables
        private bool isSprinting = false;
        private bool sprintingPressed = false;

        // Jump Variables
        private bool isJumping = false;
        private bool doubleJumping = false;
        private bool jumpLock = false;
        private bool lastIsJumping = false;
        private Coroutine jumpLockCoroutine;
        [HideInInspector] public float JumpForce { get => jumpForce; set => jumpForce = value; }

        // Ducking Variables
        private bool isDucking = false;
        private bool lastIsDucking = false;

        // Platform Variables
        [HideInInspector] public bool onPlatform = false;
        [HideInInspector] public Collider2D platformCollider;

        // Climbing Variables
        private bool isClimbing = false;
        public bool IsClimbing => isClimbing;
        private bool lastIsClimbing = false;

        private void StartMovement()
        {
            baseAcceleration = accelerationForce; // Store the base acceleration for animation speed calculations
        }
        private void FixedUpdateMovement()
        {
            isSprinting = CanSprint();
            ApplyForces();
        }

        private void HandleSprintAction(bool isPressed)
        {
            sprintingPressed = isPressed;
        }

        private void HandleMoveAction(Vector2 direction)
        {
            moveInput = direction;
        }


        //private void CanSprint(out float moveInputX, out float forceToApply)
        //{
        //    moveInputX = Mathf.Abs(playerBody.linearVelocityX) > maxWalkVelocity ? 0 : moveInput.x;
        //    if ((isSprinting || (sprintingPressed && currentStamina > maxStamina / 10)) && !playerHealthManager.IsInvincible) {
        //        if ((Mathf.Abs(playerBody.linearVelocityX) < maxSprintVelocity) && currentStamina > 0f) {
        //            moveInputX = moveInput.x;
        //            isSprinting = true;
        //        } else {
        //            moveInputX = 0f; // Check if the player is sprinting and adjust horizontal input accordingly
        //            if (currentStamina <= 0f) isSprinting = false;
        //        }
        //    } else {
        //        isSprinting = false; // Set the sprinting state to false
        //    }
        //    forceToApply = 0f;
        //    // Apply additional force if the player is beginning to move to increase acceleration
        //    if (Mathf.Abs(playerBody.linearVelocityX) < playerVelocityBoostRange && !Mathf.Approximately(moveInputX, 0f)) {
        //        forceToApply = playerVelocityBoost * moveInputX;
        //    }
        //}

        /// <summary>
        /// Handles the player's jump action based on the current game state and player conditions.
        /// </summary>
        /// <remarks>This method checks several conditions before allowing the player to jump, such as
        /// whether the player is in a dialog, already jumping, grounded, or has sufficient stamina. It also handles the
        /// animation and state changes necessary for a jump action.</remarks>
        private void HandleJumpAction()
        {
            if (isInDialog) return; // Ignore jump input if the player is in a dialog
            if (isJumping) { DoubleJump(); return; } // If the player is already jumping, call DoubleJump
            if (!isGrounded && !isClimbing) return; // Quit if the player is not grounded
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



        protected void ApplyForces()
        {
            if (playerBody.linearVelocityX > maxSprintVelocity || isClimbing) {
                return;
            }
            Vector2 force = Vector2.zero; // Initialize force to zero
            if (isGrounded) {
                // Check for crouching or ducking state
                if (moveInput.y < -0.1f) {
                    if (playerBody.linearVelocityX > maxCrouchVelocity) {
                        return; // Prevent movement if the player is moving too fast while crouching
                    }
                    force = (((moveInput.x * (accelerationForce) * duckMovementReduction)) * Vector2.right);
                }
                // Check for false friction when the player is not moving but is sliding
                else if (Mathf.Approximately(Mathf.Abs(moveInput.x), 0f) && Mathf.Abs(playerBody.linearVelocityX) > 0.1f) {
                    playerBody.linearVelocityX = Mathf.Lerp(playerBody.linearVelocityX, 0f, Time.deltaTime * decelerationForce); // Slow down the player when not moving
                }
                // If still moving, apply movement forces based on player velocity
                else if (Mathf.Abs(moveInput.x) > 0.1f) {
                    
                    if (Mathf.Abs(playerBody.linearVelocityX) > maxSprintVelocity) return;
                    else if (!isSprinting && Mathf.Abs(playerBody.linearVelocityX) > maxWalkVelocity) return;

                    force = ((moveInput.x * (isSprinting ? sprintAccelerationForce : accelerationForce)) * Vector2.right);
                }
            }
            else if (Mathf.Abs(moveInput.x) > 0.1f && Mathf.Abs(playerBody.linearVelocityX) < maxWalkVelocity) {
                force = ((moveInput.x * accelerationForce * airMovementReduction) * Vector2.right);
            // Default to no forces
            } else {
                return;
            }

            // Apply additional force if the player is beginning to move to increase acceleration
            if (Mathf.Abs(playerBody.linearVelocityX) < maxAccelerationBoostVelocity && !Mathf.Approximately(moveInput.x, 0f)) {
                force.x += accelerationBoostForce * moveInput.x;
            }

            playerBody.AddForce(force, ForceMode2D.Force); // Apply the calculated force to the player body
        }

        /// <summary>
        /// Determines whether the player can initiate or continue sprinting based on current game conditions.
        /// </summary>
        /// <remarks>Sprinting is disallowed if the player is in a dialog, invincible, climbing, ducking,
        /// or grounded. The player must have sufficient stamina to start or continue sprinting.</remarks>
        /// <returns><see langword="true"/> if the player can sprint; otherwise, <see langword="false"/>.</returns>
        private bool CanSprint()
        {
            if (!sprintingPressed || isInDialog || IsInvincible) return false; // Ignore sprint input if the player is in a dialog or invincible
            if (isClimbing) return false; // Ignore sprint if the player is climbing or ducking

            if (isSprinting && currentStamina > 0f) return true; // If the player is already sprinting and has stamina, allow sprinting

            if (currentStamina >= minSprintStaminaPercent * maxStamina) return true; // Allow sprinting to start if the player meets the minimum stamina amount set

            return false; // Otherwise, do not allow sprinting
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
        /// Adjusts the player's movement speed by applying a multiplier to the current speed.
        /// </summary>
        /// <remarks>This method updates the player's movement speed and adjusts the associated animation speed 
        /// parameter to reflect the new movement speed relative to the base speed.</remarks>
        /// <param name="moveSpeedMultiplier">The multiplier to apply to the current movement speed. Must be a positive value.</param>
        public void ChangeMoveSpeed(float moveSpeedMultiplier)
        {
            accelerationForce *= moveSpeedMultiplier; // Adjust the player's movement speed based on the multiplier
            sprintAccelerationForce *= moveSpeedMultiplier; // Adjust the sprint speed based on the multiplier
            playerAnimator.SetFloat("animSpeed", (isSprinting && currentStamina > 0f ? sprintAccelerationForce : accelerationForce) / baseAcceleration); // Update the "moveSpeed" animation parameter
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
            float originalMoveSpeed = accelerationForce; // Store the original movement speed before changes
            float originalSprintSpeed = sprintAccelerationForce; // Store the original sprint speed before changes
            accelerationForce *= moveSpeedMultiplier; // Adjust the player's movement speed based on the multiplier
            sprintAccelerationForce *= moveSpeedMultiplier; // Adjust the sprint speed based on the multiplier
            playerAnimator.SetFloat("animSpeed", (isSprinting && currentStamina > 0f ? sprintAccelerationForce : accelerationForce) / baseAcceleration); // Update the "moveSpeed" animation parameter
            StartCoroutine(TempMoveSpeed(originalMoveSpeed, originalSprintSpeed, duration)); // Start a coroutine to restore the speed after the specified duration
        }

        /// <summary>
        /// Initiates a double jump for the player if conditions are met.
        /// </summary>
        /// <remarks>The player can perform a double jump only if they are not already double jumping and
        /// have sufficient stamina. The method reduces the player's stamina by the cost of a double jump and applies
        /// the appropriate jump force.</remarks>
        private void DoubleJump()
        {
            if (doubleJumping) { return; } // Quit if the player is already double jumping
            if (currentStamina < doubleJumpStaminaCost) { return; } // Quit if the player does not have enough stamina to double jump
            currentStamina -= doubleJumpStaminaCost; // Reduce the player's stamina by the double jump cost
            doubleJumping = true;
            if (playerBody.linearVelocityY > quickJumpVelocityRange.x && playerBody.linearVelocityY < quickJumpVelocityRange.y) {
                StartCoroutine(DoubleJumpForce(true));
            } else {
                StartCoroutine(DoubleJumpForce(false));
            }
        }

        /// <summary>
        /// Applies an additional force to the player character to perform a double jump.
        /// </summary>
        /// <remarks>This method should be used to enhance the player's jump capabilities by allowing a
        /// second jump while airborne. The force applied varies depending on the <paramref name="quickJump"/>
        /// parameter, which can affect the jump's height and speed.</remarks>
        /// <param name="quickJump">A boolean value indicating whether the double jump should be executed quickly. If <see langword="true"/>,
        /// the jump force is adjusted based on the current vertical velocity; otherwise, a standard force is applied.</param>
        /// <returns></returns>
        private IEnumerator DoubleJumpForce(bool quickJump)
        {
            if (!quickJump) {
                playerBody.linearVelocityY = 0f;
                yield return null;
            }
            if (quickJump) {
                playerBody.AddForce(((jumpForce + (Mathf.Abs(playerBody.linearVelocityY))) / 2) * Vector2.up, ForceMode2D.Impulse);
            } else {
                playerBody.AddForce(jumpForce * doubleJumpMulti * Vector2.up, ForceMode2D.Impulse);
            }
        }

        /// <summary>
        /// Temporarily disables collision between the player and a platform, allowing the player to drop through it.
        /// </summary>
        /// <remarks>This coroutine ignores collisions between the player's colliders and the specified
        /// platform collider for a short duration, enabling the player to pass through the platform. After the wait
        /// period, collisions are re-enabled.</remarks>
        /// <returns></returns>
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
        /// Initiates the process for the player to drop through the current platform.
        /// </summary>
        /// <remarks>This method should be called only when the player is on a platform and the platform
        /// collider is valid. It starts a coroutine to handle the drop-through action.</remarks>
        private void JumpDown()
        {
            if (!onPlatform || !platformCollider) return; // Quit if the player is not on a platform or the platform collider is null
            StartCoroutine(DropThroughPlatform());
        }

        /// <summary>
        /// Manages the timing for releasing the jump lock after the player leaves the ground.
        /// </summary>
        /// <remarks>This coroutine waits until the player is no longer in contact with the ground before
        /// resetting the jump lock, allowing the player to jump again. It should be used in conjunction with ground
        /// detection logic to ensure proper jump mechanics.</remarks>
        /// <returns></returns>
        private IEnumerator JumpLockTimer()
        {
            float time = 0f;
            //yield return new WaitForSeconds(0.3f); // Wait for a short duration to allow the jump lock to reset
            while (groundCheckResult || time > 0.5f) // Wait until the player has left the ground
            {
                time += Time.deltaTime;
                yield return null;
            }
            jumpLock = false;
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
            accelerationForce = originalMoveSpeed; // Restore the movement speed to its original value
            sprintAccelerationForce = originalSprintSpeed; // Restore the sprint speed to its original value
            playerAnimator.SetFloat("animSpeed", (isSprinting && currentStamina > 0f ? sprintAccelerationForce : accelerationForce) / baseAcceleration); // Update the animation speed based on the restored speed
        }


    }
}