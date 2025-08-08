using UnityEngine;

public class ForgroundJumpers : MonoBehaviour
{
    [Header("Jumper Settings")]
    [SerializeField] private float minJumpTime = 1.0f; // Minimum time between jumps
    [SerializeField] private float maxJumpTime = 3.0f; // Maximum time between jumps
    [SerializeField] private float jumpForce = 5.0f; // Force applied when jumping
    private float jumpTimer;
    private bool jumpLock = false;
    [Space]
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 2.0f; // Force applied for horizontal movement
    [SerializeField] private LayerMask groundLayers; // Layers considered as ground for the jumper
    [SerializeField] private float wanderRange = 4.0f; // Range within which the jumper can wander
    private float startPosition; // Initial position of the jumper

    private bool isGrounded = true;
    private bool isFlipped = false;
    private Rigidbody2D jumperBody;
    private Animator jumperAnimator; // Animator component for handling animations


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPosition = transform.position.x; // Store the initial position of the jumper
    }

    void Awake()
    {
        if (!TryGetComponent<Rigidbody2D>(out jumperBody) || !TryGetComponent<Animator>(out jumperAnimator)) {
            Debug.LogError("Missing Components on ForgroundJumper");
        }
        jumpTimer = UnityEngine.Random.Range(minJumpTime, maxJumpTime); // Initialize the jump timer with a random value between min and max jump time
        int moveDirection = UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f ? -1 : 1; // Randomly set the move direction to either -1 or 1
        FlipSprite(moveDirection); // Call FlipSprite to set the initial orientation based on the random move direction
    }

    // Update is called once per frame
    void Update()
    {
        GroundCheck();
    }

    private void FixedUpdate()
    {
        jumpTimer -= Time.fixedDeltaTime;
        if (jumpTimer <= 0.0f) {
            if (!isGrounded) {
                jumpTimer = UnityEngine.Random.Range(minJumpTime, maxJumpTime);
                return; // Exit if not grounded, preventing further actions until the next jump timer
            }

            int jumpDirection = UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f ? -1 : 1;
            if (transform.position.x < startPosition - wanderRange) {
                jumpDirection = 1; // Force jump direction to right if the position is less than startPosition - wanderRange
            } else if (transform.position.x > startPosition + wanderRange) {
                jumpDirection = -1; // Force jump direction to left if the position is greater than startPosition + wanderRange
            }
            
            FlipSprite(jumpDirection);
            jumpLock = true;
            jumperAnimator.SetFloat("animSpeed", 1.0f); // Set the animation speed to 1 to trigger the jump animation
            jumpTimer = UnityEngine.Random.Range(minJumpTime, maxJumpTime);
        }
    }


    /// <summary>
    /// Flips the sprite's orientation based on the horizontal force applied.
    /// </summary>
    /// <remarks>This method adjusts the sprite's local scale to reflect its orientation and updates the 
    /// flipped state. If the sprite's horizontal velocity exceeds a predefined threshold in the  opposite direction, a
    /// "RunStop" animation is triggered.</remarks>
    /// <param name="xForce">The horizontal force applied to the sprite. A positive value flips the sprite to face right,  while a negative
    /// value flips it to face left.</param>
    private void FlipSprite(float xForce)
    {
        if (xForce >= 0.01f && isFlipped) {
            // Move right
            isFlipped = false;
        } else if (xForce <= -0.01f && !isFlipped) {
            // Move left
            isFlipped = true;
        } else {
            return; // No change in direction, exit the method
        }
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }


    private void GroundCheck()
    {
        bool grounded = jumperBody.IsTouchingLayers(groundLayers); // Check if the Rigidbody2D is touching the ground layers
        if (grounded && !jumpLock) {
            isGrounded = true; // Set grounded state to true if the Rigidbody2D is touching the ground layers
            //jumperAnimator.SetFloat("animSpeed", 0.0f); // Set the animation speed to 0 to stop the jump animation
        } else if (jumpLock && !grounded) {
            jumpLock = false; // Reset jump lock if the jumper is not grounded
        }
    }

    /// <summary>
    /// Applies an upward force to the associated Rigidbody2D component, simulating a jump.
    /// </summary>
    /// <remarks>This method requires the GameObject to have a Rigidbody2D component. If the component is not
    /// found,  a warning is logged and no force is applied. The grounded state is reset after the jump.</remarks>
    /// <param name="value">An integer value that can be used to modify the jump behavior. Currently unused.</param>
    public void JumpTrigger(int value)
    {
        if (value == 0) {
            float xForce = moveForce;
            if (isFlipped) {
                xForce *= -1;
            }
            if (jumperBody != null) {
                jumperBody.AddForce(new Vector2(xForce, jumpForce), ForceMode2D.Impulse); // Apply an upward force to the Rigidbody2D
                isGrounded = false; // Reset the grounded state
            } else {
                Debug.LogWarning("Rigidbody2D component not found on the GameObject.");
            }
        } else if (value == 1) {
            jumperAnimator.SetFloat("animSpeed", 0.0f); // Set the animation speed to 0 to stop the jump animation
        } else {
            Debug.LogWarning("Invalid value passed to JumpTrigger. Expected 0 or 1, received: " + value);
        }
    }


}
