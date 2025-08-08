using Sirenix.OdinInspector;
using UnityEngine;

[System.Flags]
public enum FoxState
{
    None = 0,
    LookLeft = 1 << 0,
    LookRight = 1 << 1,
    LookForward = 1 << 2,
    LookBack = 1 << 3,
    OpenIdle = 1 << 4
}

[RequireComponent(typeof(Animator))]
public class FoxFamilyController : MonoBehaviour
{
    [SerializeField] GameObject playerObject; // Reference to the player object

    [FoldoutGroup("Look Settings")]
    [Tooltip("Distance within which the character will start to look at the player.")]
    [SerializeField] float activationDistance = 2.0f;
    [FoldoutGroup("Look Settings")]
    [Tooltip("Buffer to allow for forward idle animation. If the player is within this distance, Fred will look forward instead of left/right.")]
    [SerializeField] float forwardBuffer = 0.4f;

    private Animator characterAnimator;
    FoxState state = FoxState.OpenIdle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerObject == null) {
            Debug.LogError("Player object not assigned!");
            return;
        }
        if (!TryGetComponent<Animator>(out characterAnimator)) {
            Debug.LogError("Animator component not found on FredFoxController!");
        }
    }

    private void FixedUpdate()
    {
        if (DistanceToPlayer(playerObject) < activationDistance) {
            LookToPlayer();
        } else {
            ClearFlags();
            state = FoxState.OpenIdle;
        }
    }

    /// <summary>
    /// Clears all animation flags and resets the character's state to <see cref="FredFoxState.None"/>.
    /// </summary>
    /// <remarks>This method iterates through the current animation states and disables the corresponding     
    /// animation flags in the character's animator. It ensures that all active states are cleared      and the
    /// character's state is fully reset.</remarks>
    private void ClearFlags()
    {
        while (state != FoxState.None) {
            switch (state) {
                case FoxState.LookLeft:
                    characterAnimator.SetBool("lookLeft", false);
                    state &= ~FoxState.LookLeft;
                    break;
                case FoxState.LookRight:
                    characterAnimator.SetBool("lookRight", false);
                    state &= ~FoxState.LookRight;
                    break;
                case FoxState.LookForward:
                    characterAnimator.SetBool("lookForward", false);
                    state &= ~FoxState.LookForward;
                    break;
                case FoxState.LookBack:
                    characterAnimator.SetBool("lookBackward", false);
                    state &= ~FoxState.LookBack;
                    break;
                default:
                    state = FoxState.None; // Clear all flags if none match
                    break;
            }
        }
    }

    /// <summary>
    /// Calculates the distance from the current object to the specified player.
    /// </summary>
    /// <param name="player">The player <see cref="GameObject"/> to measure the distance to. Must not be <see langword="null"/>.</param>
    /// <returns>The distance between the current object and the player's position as a <see cref="float"/>.  Returns <see
    /// cref="float.MaxValue"/> if <paramref name="player"/> is <see langword="null"/>.</returns>
    private float DistanceToPlayer(GameObject player)
    {
        if (player == null) return float.MaxValue; // Return a large value if player is null
        return Vector2.Distance(transform.position, player.transform.position);
    }

    /// <summary>
    /// Adjusts the character's orientation to face the player based on their relative position.
    /// </summary>
    /// <remarks>This method determines the direction to the player and updates the character's state and
    /// animation accordingly. If the player is within a predefined buffer area, the character will reset its state and
    /// look forward. Otherwise, the character will look left or right depending on the horizontal position of the
    /// player relative to the character.</remarks>
    private void LookToPlayer()
    {
        Vector2 directionToPlayer = (playerObject.transform.position - transform.position).normalized;
        if (InsideBufferArea()) {
            ClearFlags();
            state = FoxState.LookForward;
            characterAnimator.SetBool("lookForward", true);
        } else if (directionToPlayer.x < 0f && (state & FoxState.LookLeft) == 0) {
            ClearFlags();
            state = FoxState.LookLeft;
            characterAnimator.SetBool("lookLeft", true);
        } else if (directionToPlayer.x > 0f && (state & FoxState.LookRight) == 0) {
            ClearFlags();
            state = FoxState.LookRight;
            characterAnimator.SetBool("lookRight", true);
        }
        //} else if (directionToPlayer.y < 3f && (state & FredFoxState.LookBack) == 0) {
        //    ClearFlags();
        //    state = FredFoxState.LookBack;
        //    characterAnimator.SetBool("lookBackward", true);
        //}
    }
    
    /// <summary>
    /// Determines whether the player object is within the forward buffer area relative to this object's position.
    /// </summary>
    /// <remarks>The forward buffer area is defined as the range where the absolute horizontal distance
    /// between this object and the player object is less than the specified buffer value.</remarks>
    /// <returns><see langword="true"/> if the player object is within the forward buffer area; otherwise, <see
    /// langword="false"/>.</returns>
    private bool InsideBufferArea()
    {
        float distance = transform.position.x - playerObject.transform.position.x;
        if (Mathf.Abs(distance) < forwardBuffer) {
            return true;
        }
        return false;
    }
}
