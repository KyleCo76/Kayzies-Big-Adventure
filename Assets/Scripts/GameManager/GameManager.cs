using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class GameManager : MonoBehaviour
{
    //[SerializeField] private bool disableDialogs = false;
    [Header("Input Actions")]
    [FoldoutGroup("Game Management"), SerializeField, Tooltip("Action to pause game and open pause menu")]
    private InputAction pauseAction;
    [FoldoutGroup("Game Management"), SerializeField, Tooltip("Action to load a new scene when in pause menu")]
    private InputAction loadSceneReadyAction;
    [FoldoutGroup("Player Controls"), SerializeField, Tooltip("Action to move the player")]
    private InputAction moveAction;
    [FoldoutGroup("Player Controls"), SerializeField, Tooltip("Action to jump with the player")]
    private InputAction jumpAction;
    [FoldoutGroup("Player Controls"), SerializeField, Tooltip("Action to attack with the player")]
    private InputAction attackAction;
    [FoldoutGroup("Player Controls"), SerializeField, Tooltip("Action to interact with objects in the game")]
    private InputAction interactAction;
    [FoldoutGroup("Player Controls"), SerializeField, Tooltip("Action to sprint with the player")]
    private InputAction sprintAction;

    public delegate void MoveActionDelegate(Vector2 direction);
    public static event MoveActionDelegate OnMoveAction;
    public delegate void JumpActionDelegate();
    public static event JumpActionDelegate OnJumpAction;
    public delegate void AttackActionDelegate();
    public static event AttackActionDelegate OnAttackAction;
    public delegate void InteractActionDelegate(bool performed);
    public static event InteractActionDelegate OnInteractAction;
    public delegate void SprintActionDelegate(bool isSprinting);
    public static event SprintActionDelegate OnSprintAction;

    [HideInInspector] public static bool isPaused = false;

    private GameObject pauseMenuObject; // Reference to the PauseMenu GameObject
    private GameObject gameOverMenuObject; // Reference to the GameOver GameObject

    private AsyncOperation asyncLoad;

    public bool IsLoadingData { set; private get; } = false;
    private bool readyToLoadScene = false;

    private readonly static HashSet<string> assignedIDs = new();

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        pauseAction.performed += PauseActionPerformed;
        pauseAction.Enable();
        loadSceneReadyAction.performed += LoadSceneReady;
        loadSceneReadyAction.Enable();

        moveAction.performed += MoveActionPerformed;
        moveAction.canceled += MoveActionPerformed;
        moveAction.Enable();
        jumpAction.performed += JumpActionPerformed;
        jumpAction.Enable();
        attackAction.performed += AttackActionPerformed;
        attackAction.Enable();
        interactAction.performed += InteractActionPerformed;
        interactAction.canceled += InteractActionPerformed;
        interactAction.Enable();
        sprintAction.performed += SprintActionPerformed;
        sprintAction.canceled += SprintActionPerformed;
        sprintAction.Enable();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        pauseAction.Disable();
        pauseAction.performed -= PauseActionPerformed;
        loadSceneReadyAction.performed -= LoadSceneReady;
        loadSceneReadyAction.Disable();

        moveAction.performed -= MoveActionPerformed;
        moveAction.canceled -= MoveActionPerformed;
        moveAction.Disable();
        jumpAction.performed -= JumpActionPerformed;
        jumpAction.Disable();
        attackAction.performed -= AttackActionPerformed;
        attackAction.Disable();
        interactAction.performed -= InteractActionPerformed;
        interactAction.canceled -= InteractActionPerformed;
        interactAction.Disable();
        sprintAction.performed -= SprintActionPerformed;
        sprintAction.canceled -= SprintActionPerformed;
        sprintAction.Disable();
    }

    private void SprintActionPerformed(InputAction.CallbackContext _context)
    {
        OnSprintAction?.Invoke(_context.performed);
    }

    private void JumpActionPerformed(InputAction.CallbackContext _context)
    {
        OnJumpAction?.Invoke();
    }

    private void AttackActionPerformed(InputAction.CallbackContext _context)
    {
        OnAttackAction?.Invoke();
    }

    private void InteractActionPerformed(InputAction.CallbackContext _context)
    {
        bool performed = _context.performed;
        OnInteractAction?.Invoke(performed);
    }

    private void MoveActionPerformed(InputAction.CallbackContext _context)
    {
        Vector2 direction = _context.ReadValue<Vector2>();
        OnMoveAction?.Invoke(direction);
    }

    private void LoadSceneReady(InputAction.CallbackContext _context)
    {
        readyToLoadScene = true;
    }

    /// <summary>
    /// Toggles the game's paused state based on the current state.
    /// </summary>
    /// <remarks>If the game is currently paused, this method resumes the game. Otherwise, it pauses the
    /// game.</remarks>
    /// <param name="context">The callback context associated with the input action triggering this method.</param>
    private void PauseActionPerformed(InputAction.CallbackContext _context)
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    // Disable all dialog objects if the flag is set to true
    //    if (disableDialogs) {
    //        GameObject[] dialogObjects = GameObject.FindGameObjectsWithTag("Dialogs");
    //        foreach (GameObject dialogObject in dialogObjects)
    //        {
    //            dialogObject.SetActive(false);
    //        }
    //    }
    //    //OnSceneLoaded(SceneManager.GetActiveScene());
    //}


    public AsyncOperation LoadScene(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        Debug.Log($"Loading scene: {sceneName}");
        Time.timeScale = 1.0f; // Ensure time scale is reset before loading a new scene
        Scene scene = SceneManager.GetActiveScene();
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, loadMode);
        return asyncLoad;
    }

    public void LoadSceneWithLoadingScreen(string targetScene)
    {
        Time.timeScale = 1.0f;
        StartCoroutine(LoadSceneWithLoading(targetScene));
    }

    private IEnumerator LoadSceneWithLoading(string targetScene)
    {
        // Load the loading scene first
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single);

        // Wait one frame to ensure the loading scene is active
        yield return null;

        Debug.Log("Loading new scene");
        // Start loading the target scene asynchronously
        asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);

        // Optionally, prevent the scene from activating until ready
        //asyncLoad.allowSceneActivation = false;

        // Wait until the scene is loaded
        while (asyncLoad.progress < 0.9f) {
            Debug.Log("Waiting " + asyncLoad.progress);
            // You can update a progress bar here using asyncLoad.progress
            yield return null;
        }

        while (!readyToLoadScene || IsLoadingData) {
            yield return null;
        }

        // Activate the scene
        asyncLoad.allowSceneActivation = true;
        //SceneManager.UnloadSceneAsync("LoadingScene");
        readyToLoadScene = false;
    }

    /// <summary>
    /// Pauses the game by freezing time and displaying the pause menu.
    /// </summary>
    /// <remarks>This method sets the game's time scale to zero, effectively halting all in-game activity. It
    /// also activates the pause menu UI, if one is assigned.</remarks>
    public void PauseGame()
    {
        if (pauseMenuObject != null)
            pauseMenuObject.SetActive(true);
        isPaused = true;
        Time.timeScale = 0.0f;
        //SetupPauseMenu();
    }

    /// <summary>
    /// Resumes the game after a short delay.
    /// </summary>
    /// <remarks>This method initiates a coroutine to resume the game after a specified delay.  The delay
    /// duration is fixed and cannot be customized through this method.</remarks>
    public void ResumeGame()
    {
        StartCoroutine(ResumeDelay(0.2f));
    }

    public void RestartGame()
    {
        LoadScene("MainMenu");
    }

    public void NewGame()
    {
        LoadScene("KayzieHomeVillage");
    }
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    public void Die()
    {
        Time.timeScale = 0.0f; // Pause the game
        gameOverMenuObject.SetActive(true); // Show the GameOver menu
    }

    /// <summary>
    /// Resumes the game after a specified delay.
    /// </summary>
    /// <remarks>This method waits for the specified delay time in real-time (ignoring the game's time scale)
    /// before resuming the game by deactivating the pause menu, unpausing the game state, and resetting the time scale
    /// to normal.</remarks>
    /// <param name="delayTime">The duration, in seconds, to wait before resuming the game.</param>
    /// <returns>An enumerator that performs the delay operation.</returns>
    private IEnumerator ResumeDelay(float delayTime)
    {
        yield return new WaitForSecondsRealtime(delayTime);
        if (pauseMenuObject != null)
            pauseMenuObject.SetActive(false);
        isPaused = false;
        Time.timeScale = 1.0f;
    }

    public bool IsWithinGameWindow(Transform objectTransform, float buffer = 0.1f)
    {
        Camera camera = Camera.main;
        Vector3 viewportPoint = camera.WorldToViewportPoint(objectTransform.position);

        return viewportPoint.x >= -buffer && viewportPoint.x <= (1 + buffer) &&
               viewportPoint.y >= -buffer && viewportPoint.y <= (1 + buffer) &&
               viewportPoint.z > 0;
    }

    public bool RegisterCollectableID(string _id)
    {
        return assignedIDs.Add(_id);
    }
}
