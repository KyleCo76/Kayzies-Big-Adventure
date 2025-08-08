using UnityEngine;

public class Managers : MonoBehaviour
{
    public static Managers Instance { get; private set; }

    public CameraManager CameraManager { get; private set; }

    public GameManager GameManager { get; private set; }

    public DialogManager DialogManager { get; private set; }

    public SaveGameManagerV1 SaveGameManager { get; private set; }

    private void OnEnable()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CameraManager = GetComponentInChildren<CameraManager>();
        if (CameraManager == null) {
            Debug.LogError("CameraManager not found in children of Managers.");
        }
        GameManager = GetComponentInChildren<GameManager>();
        if (GameManager == null) {
            Debug.LogError("GameManager not found in children of Managers.");
        }
        DialogManager = GetComponentInChildren<DialogManager>();
        if (DialogManager == null) {
            Debug.LogError("DialogManager not found in children of Managers.");
        }
        SaveGameManager = GetComponentInChildren<SaveGameManagerV1>();
        if (SaveGameManager == null) {
            Debug.LogError("SaveGameManager not found in children of Managers.");
        }
    }
}
