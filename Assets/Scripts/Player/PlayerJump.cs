using UnityEngine;

public class PlayerJump : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Rigidbody2D playerBody = animator.GetComponent<Rigidbody2D>();
        Kayzie.Player.PlayerControllerV2 playerController = animator.GetComponent<Kayzie.Player.PlayerControllerV2>();
        playerBody.AddForce(Vector2.up * playerController.JumpForce, ForceMode2D.Impulse); // Apply an upward force for jumping
    }
}
