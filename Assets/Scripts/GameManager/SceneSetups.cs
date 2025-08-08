using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameManager : MonoBehaviour
{
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        Debug.Log("Loaded scene: " + scene.name);
        if (scene.name == "MainMenu") {
            SetupMainMenu();
            return;
        }
        //if (scene.name == "LoadingScene") {
        //    Debug.Log("Skipping setup in LoadingScene.");
        //    return; // Skip setup in the loading scene
        //}

        SetupPauseMenu();
        SetupGameOver();
        //DisableDialogs();
        var cameraManager = FindAnyObjectByType<CameraManager>();
        if (cameraManager != null) {
            cameraManager.UpdateCamera();
        } else {
            Debug.LogError("CameraManager not found in the scene. Cannot update camera settings.");
        }
        var InventoryManager = FindAnyObjectByType<InventoryManager>();
        if (InventoryManager != null) {
            InventoryManager.LoadedScene();
        } else {
            Debug.LogWarning("InventoryManager not found in the scene. Cannot update inventory settings.");
        }
    }

    private void DisableDialogs()
    {
        // Find all GameObjects with the "Dialog" tag and disable them
        GameObject[] dialogObjects = GameObject.FindGameObjectsWithTag("DialogBox");
        foreach (GameObject dialog in dialogObjects) {
            dialog.SetActive(false);
        }
    }
    /// <summary>
    /// Configures the pause menu by locating its GameObject in the scene, ensuring it is initially hidden,  and setting
    /// up event listeners for its buttons.
    /// </summary>
    /// <remarks>This method searches for a GameObject named "PauseMenu" in the currently loaded scene. If
    /// found,  it ensures the pause menu is inactive by default and attaches event listeners to the "ResumeButton"  and
    /// "QuitButton" within the pause menu. If the "PauseMenu" GameObject is not found, an error is logged.</remarks>
    private void SetupPauseMenu()
    {
        //if (SceneManager.GetActiveScene().name == "LoadingScene") {
        //    Debug.Log("Skipping PauseMenu setup in LoadingScene.");
        //    return; // Skip setup in the loading scene
        //}
        // Find the PauseMenu GameObject by name and tag
        pauseMenuObject = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go =>
                go.name == "PauseMenu" &&
                go.scene.IsValid());

        if (pauseMenuObject == null) {
            Debug.LogWarning("PauseMenu GameObject not found in the loaded scene.");
            return; // Exit if the pause menu is not found
        }
        pauseMenuObject.SetActive(false); // Ensure the pause menu is hidden when a new scene is loaded
        Transform resumeButton = pauseMenuObject.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "ResumeButton" && t != pauseMenuObject.transform);
        if (resumeButton != null) {
            resumeButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ResumeGame);
        }
        Transform restartButton = pauseMenuObject.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "RestartButton" && t != pauseMenuObject.transform);
        if (restartButton != null) {
            restartButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(RestartGame);
        }
        Transform saveButton = pauseMenuObject.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "SaveButton" && t != pauseMenuObject.transform);
        Debug.Log("SaveButton found: " + (saveButton != null));
        if (saveButton != null) {
            SaveGameManagerV1 saveManager = FindAnyObjectByType<SaveGameManagerV1>();
            saveButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(saveManager.SaveGame);
        }
        Transform quitButton = pauseMenuObject.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "QuitButton" && t != pauseMenuObject.transform);
        if (quitButton != null) {
            quitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(QuitGame);
        }
    }

    /// <summary>
    /// Configures the Game Over menu by locating its GameObject in the scene,  setting it to inactive, and attaching
    /// event listeners to its buttons.
    /// </summary>
    /// <remarks>This method searches for a GameObject named "GameOver" in the currently loaded scene. If
    /// found, it ensures the GameObject is inactive and assigns click event handlers to the  "RestartButton" and
    /// "QuitButton" child elements, if they exist. If the GameObject is not  found, an error is logged.</remarks>
    private void SetupGameOver()
    {
        // Find the PauseMenu GameObject by name and tag
        gameOverMenuObject = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go =>
                go.name == "GameOver" &&
                go.scene.IsValid());

        if (gameOverMenuObject == null) {
            Debug.LogWarning("GameOver GameObject not found in the loaded scene.");
            return; // Exit if the game over menu is not found
        }
        gameOverMenuObject.SetActive(false); // Ensure the pause menu is hidden when a new scene is loaded
        Transform restartButton = gameOverMenuObject.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "RestartButton" && t != gameOverMenuObject.transform);
        if (restartButton != null) {
            restartButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(RestartGame);
        }
        Transform quitButton = gameOverMenuObject.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "QuitButton" && t != gameOverMenuObject.transform);
        if (quitButton != null) {
            quitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(QuitGame);
        }
    }

    /// <summary>
    /// Configures the main menu by locating its GameObject in the loaded scene and attaching event listeners to the
    /// "NewGameButton" and "QuitButton" if they are present.
    /// </summary>
    /// <remarks>This method searches for a GameObject named "MainMenu" in the currently loaded scene. If
    /// found, it attempts to locate child buttons named "NewGameButton" and "QuitButton" within the main menu
    /// hierarchy. Event listeners are added to these buttons to invoke the <see cref="NewGame"/> and <see
    /// cref="QuitGame"/> methods, respectively. If the "MainMenu" GameObject is not found, an error is
    /// logged.</remarks>
    private void SetupMainMenu()
    {
        GameObject mainMenu = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go =>
                go.name == "MainMenu" &&
                go.scene.IsValid());

        if (mainMenu == null) {
            Debug.LogError("MainMenu GameObject not found in the loaded scene.");
        } else {
            // Resume button setup
            Transform resumeButton = mainMenu.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "NewGameButton" && t != mainMenu.transform);
            if (resumeButton != null) {
                resumeButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(NewGame);
            }
            // Quit button setup
            Transform quitButton = mainMenu.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "QuitButton" && t != mainMenu.transform);
            if (quitButton != null) {
                quitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(QuitGame);
            }
            
            // Load button setup
            var saveManager = FindAnyObjectByType<SaveGameManagerV1>();
            if (saveManager == null) {
                Debug.LogError("SaveGameManager not found in the scene.");
                return;
            }
            bool showLoadButton = saveManager.IsSaveGameAvailable(); // Check if a save file exists
            Transform loadGameButton = mainMenu.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "LoadGameButton" && t != mainMenu.transform);
            if (loadGameButton != null) {
                if (!showLoadButton) {
                    GameObject parent = loadGameButton.parent.gameObject;
                    parent = parent.transform.parent.gameObject; // Navigate to the parent GameObject
                    parent.SetActive(false);
                } else {
                    GameObject parent = loadGameButton.parent.gameObject;
                    parent = parent.transform.parent.gameObject; // Navigate to the parent GameObject
                    parent.SetActive(true);
                    loadGameButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                        saveManager.LoadGame();
                    });
                }
            }
        }
    }
}
