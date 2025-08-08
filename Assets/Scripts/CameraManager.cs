using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{

    private CinemachineCamera[] allCameras;
    [Header("Contolls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float fallPanAmount = 0.25f;
    [SerializeField] private float fallPanTime = 0.35f;
    public float fallSpeedYDampingChangeThreashold = -1.5f;

    public bool IsLerpingYDamping { get; private set; } = false;

    public bool LerpedFromPlayerFalling { get; set; } = false;

    private CinemachineCamera currentCamera;
    private CinemachinePositionComposer positionComposer;

    private float normYPanAmount;

    private Coroutine lerpCoroutine;
    private Coroutine panCameraCoroutine;

    private Vector2 startingTrackedObjectOffset;
    private float originalZoom = 0f;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") {
            return;
        }
        UpdateCamera();

        // Set the YDamping amount so it's based on the inspector value
        normYPanAmount = positionComposer.Damping.y;

        // Set the starting tracked object offset
        startingTrackedObjectOffset = positionComposer.TargetOffset;
    }

    private void Update()
    {
        //for (int i = 0; i < allCameras.Length; i++) {
        //    if (allCameras[i].enabled) {
        //        currentCamera = allCameras[i];
        //        positionComposer = currentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        //        //positionComposer.Damping = new Vector3(100f,100f,100f); // Set initial damping values
        //        break;
        //    }
        //}
        if (currentCamera == null && SceneManager.GetActiveScene().name != "LoadingScene") {
            UpdateCamera();
        }
        //Debug.Log(currentCamera);
    }

    /// <summary>
    /// Updates the current active camera by searching for all cameras under the main camera tag.
    /// </summary>
    /// <remarks>This method locates all <see cref="CinemachineCamera"/> components that are children of the
    /// GameObject tagged as "MainCamera". It sets the first enabled camera it finds as the current camera and retrieves
    /// its position composer component.</remarks>
    public void UpdateCamera()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") {
            return;
        }
        GameObject cameraParent = GameObject.FindGameObjectWithTag("CameraParent");
        if (cameraParent == null) {
            Debug.LogWarning("CameraParent GameObject not found in the scene. Cannot update camera settings.");
            return;
        }
        allCameras = cameraParent.GetComponentsInChildren<CinemachineCamera>(true);
        //allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        for (int i = 0; i < allCameras.Length; i++) {
            var cam = allCameras[i];
            if (cam != null && cam.enabled) {
                currentCamera = cam;
                positionComposer = cam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
                //positionComposer.Damping = new Vector3(100f,100f,100f); // Set initial damping values
                break;
            }
        }
    }


    public void PanCameraOnContact(float panDistance, float panTime, Direction panDirection, bool panToStartingPos)
    {
        panCameraCoroutine = StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, Direction panDirection, bool panToStartingPos)
    {
        Vector2 endPos = Vector2.zero;

        // Handle pan from Trigger
        if (!panToStartingPos) {
            switch (panDirection) {
                case Direction.Left:
                    endPos = Vector2.left;
                    break;
                case Direction.Right:
                    endPos = Vector2.right;
                    break;
                case Direction.Up:
                    endPos = Vector2.up;
                    break;
                case Direction.Down:
                    endPos = Vector2.down;
                    break;
            }

            endPos *= panDistance;
            endPos += Vector2.zero;
        } else {
            endPos = startingTrackedObjectOffset;
        }

        // Handle the pan action
        float elapsedTime = 0f;
        while (elapsedTime < panTime) {
            elapsedTime += Time.deltaTime;
            // Lerp the target offset
            Vector3 lerpedOffset = Vector3.Lerp(Vector2.zero, endPos, elapsedTime / panTime);
            positionComposer.TargetOffset = lerpedOffset;
            yield return null; // Wait for the next frame
        }
    }

    public void SwapCameras(CinemachineCamera cameraFromLeft, CinemachineCamera cameraFromRight, Vector2 triggerExitDirection)
    {
        // If the current camer is the camera on the left and our trigger exit direction was on the right
        if (currentCamera == cameraFromLeft && triggerExitDirection.x > 0f) {
            // Activate the new camera
            cameraFromRight.enabled = true;
            // Deactivate the old camera
            cameraFromLeft.enabled = false;
            currentCamera = cameraFromRight;
        } else if (currentCamera == cameraFromRight && triggerExitDirection.x < 0f) {
            // Activate the new camera
            cameraFromLeft.enabled = true;
            // Deactivate the old camera
            cameraFromRight.enabled = false;
            currentCamera = cameraFromLeft;
        }
            positionComposer = currentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
    }

    public void SwapCameras(CinemachineCamera cameraFromLeft, CinemachineCamera cameraFromRight, Vector2 triggerExitDirection, Transform fixedCameraTarget, bool fixedCameraOnLeft)
    {
        // If the current camer is the camera on the left and our trigger exit direction was on the right
        if (currentCamera == cameraFromLeft && triggerExitDirection.x > 0f) {
            // Activate the new camera
            cameraFromRight.enabled = true;
            // Deactivate the old camera
            cameraFromLeft.enabled = false;

            currentCamera = cameraFromRight;
            positionComposer = currentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
            if (!fixedCameraOnLeft) {
                currentCamera.Follow = fixedCameraTarget;
            }
        } else if (currentCamera == cameraFromRight && triggerExitDirection.x < 0f) {
            // Activate the new camera
            cameraFromLeft.enabled = true;
            // Deactivate the old camera
            cameraFromRight.enabled = false;

            currentCamera = cameraFromLeft;
            positionComposer = currentCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
            if (fixedCameraOnLeft) {
                currentCamera.Follow = fixedCameraTarget;
            }
        }
    }

    public void ZoomCameraOnContact(float zoomAmount, float zoomTime)
    {
        // Start the zoom coroutine
        StartCoroutine(ZoomCamera(zoomAmount, zoomTime));
    }

    public void ZoomCameraOnContact(Collider2D _other, BoxCollider2D _triggerCollider, float _zoomAmount, float _zoomDistanceStart, float _zoomDistanceEnd)
    {
        if (Mathf.Approximately(originalZoom, 0f)) {
            // If originalZoom is not set, initialize it
            originalZoom = currentCamera.Lens.OrthographicSize;
        }
        float targetZoom = originalZoom * _zoomAmount;
        float distanceIntoCollider = GetDistanceIntoCollider(_other, _triggerCollider, Direction.Left, _zoomDistanceStart);

        float colliderWidth = _triggerCollider.bounds.size.x < _zoomDistanceEnd ? _triggerCollider.bounds.size.x : _zoomDistanceEnd;
        float percentIntoCollider = Mathf.Clamp01(distanceIntoCollider / colliderWidth);

        float zoomGap = targetZoom - originalZoom;
        float zoomAddition = zoomGap * percentIntoCollider;
        targetZoom = originalZoom + zoomAddition;
        currentCamera.Lens.OrthographicSize = targetZoom;
        if (Mathf.Approximately(targetZoom, originalZoom)) {
            // If the target zoom is approximately equal to the original zoom, reset originalZoom
            originalZoom = 0f;
        }
    }

    public void RemoveZoomCameraOnContact()
    {
        currentCamera.Lens.OrthographicSize = originalZoom;
    }

    private IEnumerator ZoomCamera(float zoomAmount, float zoomTime)
    {
        // Get the current field of view
        float startFOV = currentCamera.Lens.OrthographicSize;
        // Calculate the target field of view
        float targetFOV = startFOV * zoomAmount;
        // Perform the zoom action
        float elapsedTime = 0f;
        while (elapsedTime < zoomTime) {
            elapsedTime += Time.deltaTime;
            // Lerp the field of view
            currentCamera.Lens.OrthographicSize = Mathf.Lerp(startFOV, targetFOV, elapsedTime / zoomTime);
            yield return null; // Wait for the next frame
        }
    }

    /// <summary>
    /// Initiates a coroutine to smoothly adjust the Y-axis damping based on the player's falling state.
    /// </summary>
    /// <remarks>This method starts a coroutine to perform the damping adjustment over time.  Ensure that this
    /// method is called only when necessary to avoid overlapping coroutines.</remarks>
    /// <param name="isPlayerFalling">A value indicating whether the player is currently falling.  If <see langword="true"/>, the damping will be
    /// adjusted for a falling state;  otherwise, it will be adjusted for a non-falling state.</param>
    public void LerpYDamping(bool isPlayerFalling)
    {
        lerpCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    /// <summary>
    /// Smoothly interpolates the vertical damping value over a specified duration, adjusting the camera's behavior
    /// based on whether the player is falling.
    /// </summary>
    /// <remarks>This method performs a time-based interpolation of the camera's vertical damping value,
    /// allowing for smooth transitions between different damping states. It is designed to be used in scenarios where
    /// the camera needs to adjust its behavior dynamically based on the player's movement state. <para> While the
    /// interpolation is active, the <see cref="IsLerpingYDamping"/> property is set to <see langword="true"/>. Once the
    /// interpolation completes, the property is set to <see langword="false"/>. </para></remarks>
    /// <param name="isPlayerFalling">A value indicating whether the player is falling. If <see langword="true"/>, the damping value transitions to
    /// the falling pan amount; otherwise, it transitions to the normal vertical pan amount.</param>
    /// <returns></returns>
    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        // Grab the starting damping amount
        float startDampAmount = positionComposer.Damping.y;
        float endDampAmount = isPlayerFalling ? fallPanAmount : normYPanAmount;
        LerpedFromPlayerFalling = isPlayerFalling;

        // Lerp the pan amount
        float elapsedTime = 0f;
        while (elapsedTime < fallPanTime) {
            elapsedTime += Time.deltaTime;

            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, elapsedTime / fallPanTime);
            positionComposer.Damping = new Vector3(positionComposer.Damping.x, lerpedPanAmount, positionComposer.Damping.z);

            yield return null; // Wait for the next frame
        }
        IsLerpingYDamping = false;
    }

    public bool FadeCameraWithDistance(Collider2D other, BoxCollider2D collider, Image fadeImage, float fadeDistance, float maxDistance, Direction startingEdge)
    {
        float colliderWidth = collider.bounds.size.x;

        float distanceIntoCollider = GetDistanceIntoCollider(other, collider, startingEdge, fadeDistance);

        colliderWidth = maxDistance > colliderWidth ? colliderWidth : maxDistance;

        float percentIntoCollider = Mathf.Clamp01(distanceIntoCollider / colliderWidth);

        Color color = fadeImage.color;
        color.a = percentIntoCollider;
        fadeImage.color = color;
        if (percentIntoCollider >= 1f) {
            return true;
        } else {
            return false;
        }
    }

    public CinemachineCamera GetCurrentCamera()
    {
        if (currentCamera != null)
            return currentCamera;
        else {
            UpdateCamera();
            if (currentCamera != null)
                return currentCamera;
            else {
                Debug.LogError("currentCamer is null for save");
                return null;
            }
        }
    }

    private float GetDistanceIntoCollider(Collider2D other, BoxCollider2D collider, Direction startingEdge, float startingBuffer)
    {
        Bounds bounds = collider.bounds;
        Vector3 playerPos = other.transform.position;

        return startingEdge switch {
            Direction.Left => Mathf.Clamp(playerPos.x - (bounds.min.x + startingBuffer), 0f, bounds.size.x),
            Direction.Right => Mathf.Clamp((bounds.max.x - startingBuffer) - playerPos.x, 0f, bounds.size.x),
            Direction.Up => Mathf.Clamp((bounds.max.y - startingBuffer) - playerPos.y, 0f, bounds.size.y),
            Direction.Down => Mathf.Clamp(playerPos.y - (bounds.min.y + startingBuffer), 0f, bounds.size.y),
            _ => 0f,
        };
    }
}
