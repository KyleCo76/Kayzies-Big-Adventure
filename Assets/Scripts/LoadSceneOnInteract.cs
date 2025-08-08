using UnityEngine;

public class LoadSceneOnInteract : MonoBehaviour
{
    [Tooltip("Name of the scene to load when the player interacts with the trigger")]
    [SerializeField] private string sceneToLoad = string.Empty;


    private void OnTriggerEnter2D(Collider2D other)
    {
        LoadScene(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        LoadScene(other);
    }

    private void LoadScene(Collider2D other)
    {
        Kayzie.Player.PlayerControllerV2 playerController = other.gameObject.GetComponent<Kayzie.Player.PlayerControllerV2>();
        if (other.gameObject.CompareTag("Player") && playerController != null && playerController.IsClimbing) {
            // Load the new scene when the player enters the trigger
            Managers.Instance.GameManager.LoadSceneWithLoadingScreen(sceneToLoad);
        }
    }
}
