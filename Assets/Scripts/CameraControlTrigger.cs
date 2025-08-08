using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class CameraControlTrigger : MonoBehaviour
{
    [SerializeField] private bool destroyOnTriggerExit = false; // Flag to indicate if the trigger should be destroyed on exit

    [ToggleGroup("swapCameras"), SerializeField, Tooltip("Should cameras be swapped on contact?")]
    private bool swapCameras = false; // Flag to indicate if cameras should be swapped
    [ToggleGroup("swapCameras"), SerializeField, Tooltip("Camera to activate when exiting on left")]
    private CinemachineCamera cameraOnLeft; // Camera to activate when on the left side
    [ToggleGroup("swapCameras"), SerializeField, Tooltip("Camera to activate when exiting on right")]
    private CinemachineCamera cameraOnRight; // Camera to activate when on the right side
    [ToggleGroup("swapCameras"), SerializeField,]
    private bool useFixedCameraFollowObject = false;
    [ToggleGroup("swapCameras"), SerializeField]
    private Transform fixedCameraFollowObject;
    [ToggleGroup("swapCameras"), SerializeField]
    private bool fixedCameraIsOnLeft = false;

    [ToggleGroup("panCameraOnContact"), SerializeField]
    private bool panCameraOnContact = false;
    [ToggleGroup("panCameraOnContact"), SerializeField]
    private float panDistance = 3f;
    [ToggleGroup("panCameraOnContact"), SerializeField]
    private float panTime = 0.35f;
    [ToggleGroup("panCameraOnContact"), SerializeField]
    private Direction panDirection;
    [ToggleGroup("panCameraOnContact"), SerializeField, Tooltip("Direction in which the pan effect should be triggered")]
    private Direction TriggerDirection = Direction.All;

    [ToggleGroup("cameraZoomOnContact"), SerializeField, Tooltip("Should the camera zoom in or out on contact?")]
    private bool cameraZoomOnContact = false;
    [ToggleGroup("cameraZoomOnContact"), SerializeField, Tooltip("Zoom factor for the camera. Values greater than 0 will zoom out and values below 0 will zoom in")]
    private float cameraZoomFactor = 1.2f;
    [ToggleGroup("cameraZoomOnContact"), SerializeField, Tooltip("Should the zoom effect be based on distance into collider or time based?")]
    private bool zoomWithDistance = false;
    [ToggleGroup("cameraZoomOnContact"), SerializeField, Tooltip("Duration of the zoom effect in seconds"), HideIf("zoomWithDistance")]
    private float cameraZoomDuration = 0.5f;
    [ToggleGroup("cameraZoomOnContact"), SerializeField, Tooltip("Distance the zoom effect should begin"), ShowIf("zoomWithDistance")]
    private float cameraZoomStartDistance = 0.5f;
    [ToggleGroup("cameraZoomOnContact"), SerializeField, Tooltip("Distance the zoom will be in full effect. Extreme values will trigger full zoom on collider exit"), ShowIf("zoomWithDistance")]
    private float cameraZoomMaxDistance = 4.0f;

    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Should the camera fade in or out based on distance?")]
    private bool fadeCameraWithDistance = false;
    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Distance at which the camera should start fading")]
    private float fadeDistance = 0.5f;
    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Max Distance the player can walk into the collider before the camera goes black. Extreme values will trigger full black on collider exit")]
    private float maxFadeDistance = 4.0f;
    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Canvas object that will be used to fade screen")]
    private Image fadeCanvasImage;
    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Edge that distance should be measured from")]
    private Direction fadeDistanceEdge = Direction.None;
    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Should a new scene be loaded when fade reaches 100%?")]
    private bool loadSceneOnFade = false;
    [ToggleGroup("fadeCameraWithDistance"), SerializeField, Tooltip("Name of the scene to load when fade reaches 100%"), ShowIf("loadSceneOnFade")]
    private string sceneToLoadOnFade = string.Empty;
    private bool triggerNewScene = false;
    [SerializeField] private float colliderWidth = 0f;

    [Button("Calculate Collider Width")]
    private void CalculateAndLogColliderWidth()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null) {
            colliderWidth = collider.bounds.size.x;
        } else {
            Debug.LogWarning("No Collider2D attached to this GameObject.");
        }
    }

    private Collider2D thisCollider;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisCollider = GetComponent<Collider2D>();
        if (fadeCameraWithDistance && fadeCanvasImage == null) {
            Debug.LogError("Fade Canvas Image is not assigned but fadeCameraWithDistance is enabled on " + gameObject.name);
        }
        if (swapCameras && (cameraOnLeft == null || cameraOnRight == null)) {
            Debug.LogError("Cameras are not assigned but swapCameras is enabled on " + gameObject.name);
        }
    }

    private void Update()
    {
        // Check if a new scene should be loaded based on the fade effect
        if (triggerNewScene && loadSceneOnFade) {
            Managers.Instance.GameManager.LoadSceneWithLoadingScreen(sceneToLoadOnFade);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
            if (cameraZoomOnContact && !zoomWithDistance) {
                Managers.Instance.CameraManager.ZoomCameraOnContact(cameraZoomFactor, cameraZoomDuration);
            } else if (cameraZoomOnContact && zoomWithDistance) {
                Managers.Instance.CameraManager.ZoomCameraOnContact(other, thisCollider as BoxCollider2D, cameraZoomFactor, cameraZoomStartDistance, cameraZoomMaxDistance);
            }
            if (panCameraOnContact) {
                Managers.Instance.CameraManager.PanCameraOnContact(panDistance, panTime, panDirection, false);
            }
            if (TriggerDirection != Direction.All && TriggerDirection != GetCollisionDirection(other)) {
                return; // Exit if the direction does not match the trigger direction
            }
            if (fadeCameraWithDistance) {
                triggerNewScene = Managers.Instance.CameraManager.FadeCameraWithDistance(other, thisCollider as BoxCollider2D, fadeCanvasImage, fadeDistance, maxFadeDistance, fadeDistanceEdge);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) {
            if (swapCameras && cameraOnLeft != null && cameraOnRight != null) {
                // Swap Cameras
                Vector2 exitDirection = (other.transform.position - thisCollider.bounds.center).normalized;
                if (!useFixedCameraFollowObject) {
                    Managers.Instance.CameraManager.SwapCameras(cameraOnLeft, cameraOnRight, exitDirection);
                } else {
                    Managers.Instance.CameraManager.SwapCameras(cameraOnLeft, cameraOnRight, exitDirection, fixedCameraFollowObject, fixedCameraIsOnLeft);
                }
            }
            if (panCameraOnContact) {
                Managers.Instance.CameraManager.PanCameraOnContact(panDistance, panTime, panDirection, true);
            }
            if (cameraZoomOnContact && !zoomWithDistance) {
                Managers.Instance.CameraManager.ZoomCameraOnContact(1 / cameraZoomFactor, cameraZoomDuration); // Zoom back to original state
            } else if (cameraZoomOnContact && zoomWithDistance) {
                Managers.Instance.CameraManager.ZoomCameraOnContact(other, thisCollider as BoxCollider2D, cameraZoomFactor, cameraZoomStartDistance, cameraZoomMaxDistance);
            }
            if (destroyOnTriggerExit) {
                Destroy(gameObject); // Destroy the trigger object on exit
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (fadeCameraWithDistance) {
                triggerNewScene = Managers.Instance.CameraManager.FadeCameraWithDistance(other, thisCollider as BoxCollider2D, fadeCanvasImage, fadeDistance, maxFadeDistance, fadeDistanceEdge);
            }
            if (cameraZoomOnContact && zoomWithDistance) {
                Managers.Instance.CameraManager.ZoomCameraOnContact(other, thisCollider as BoxCollider2D, cameraZoomFactor, cameraZoomStartDistance, cameraZoomMaxDistance);
            }
        }
    }

    private Direction GetCollisionDirection(Collider2D other)
    {
        // Calculate the direction from the trigger's center to the player's position
        Vector2 entryDirection = (other.transform.position - thisCollider.bounds.center).normalized;

        // Determine the primary axis of entry by comparing the absolute values of the direction vector
        if (Mathf.Abs(entryDirection.x) > Mathf.Abs(entryDirection.y)) {
            // Entry is primarily horizontal (left or right)
            if (entryDirection.x > 0) {
                return Direction.Right;
            } else {
                return Direction.Left;
            }
        } else {
            // Entry is primarily vertical (top or bottom)
            if (entryDirection.y > 0) {
                return Direction.Up;
            } else {
                return Direction.Down;
            }
        }
    }
}

public enum Direction
{
    None,
    Left,
    Right,
    Up,
    Down,
    All
}