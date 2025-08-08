using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            Debug.Log("PauseMenu instance created and set as singleton.");
        } else {
            Destroy(gameObject); // Prevent duplicates
            Debug.LogWarning("PauseMenu instance already exists. Destroying duplicate.");
        }
    }
}
