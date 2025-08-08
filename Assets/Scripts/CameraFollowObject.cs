using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Transform playerTransform; // The object the camera will follow

    [Header("Flip Rotation Stats")]
    [SerializeField] private float flipYRotationTime = 1f; // Time to flip the camera's Y rotation

    [Header("Camera Settings")]
    [SerializeField] private float cameraVerticalOffset = 2.0f; // Vertical offset of the camera from the player


    private Kayzie.Player.PlayerControllerV2 playerController; // Reference to the PlayerController script

    private bool isFlipped; // Flag to check if the camera is facing right

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        playerController = playerTransform.GetComponent<Kayzie.Player.PlayerControllerV2>();

        isFlipped = playerController.IsFlipped;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    
    //}

    // Update is called once per frame
    void Update()
    {
        // Make the camera follow the player's position
        transform.position = playerTransform.position + new Vector3(0.0f, cameraVerticalOffset, 0.0f);
    }

    public void CallTurn()
    {
        LeanTween.rotateY(gameObject, isFlipped ? 0f : 180f, flipYRotationTime).setEaseInOutSine();
        isFlipped = !isFlipped; // Toggle the flipped state

    }
}
