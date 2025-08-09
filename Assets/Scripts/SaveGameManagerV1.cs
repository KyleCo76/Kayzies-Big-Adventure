using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveGameManagerV1 : MonoBehaviour
{
    [SerializeField] private int maxSaveCount = 5;

    public delegate void SaveGameEventHandler(string saveName);
    public event SaveGameEventHandler OnSaveGame;
    public delegate void LoadGameEventHandler(SaveGameData[] savedData);
    public event LoadGameEventHandler OnLoadGame;

    private int objectsToManageCount = 0; // Tracks how many objects are subscribed to OnSaveGame
    public int ObjectsManaged { get; set; } = 0; // Tracks how many objects have been saved/loaded
    private readonly List<string> savedObjectNames = new(); // List to track names of saved objects for debugging
    private string dbPath;
    private string saveName;
    private readonly object saveLock = new();

    private static AsyncOperation sceneTask;

    public void SaveGame()
    {
        // Implement your save game logic here
        Debug.Log($"Saving game with name: {saveName}");

        objectsToManageCount = OnSaveGame.GetInvocationList().Length;
        if (objectsToManageCount == 0) {
            Debug.LogWarning("No objects to save.");
            return;
        }

        // Create the initial row for the save game
        saveName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string sceneName = SceneManager.GetActiveScene().name;
        SaveGameData initialRow = new()
        {
            SceneName = sceneName,
            SaveName = saveName,
            CharacterName = "InitialRow",
            Health = 0,
            MaxHealth = 0,
            Stamina = 0,
            MaxStamina = 0,
            Level = 0,
            PositionX = 0f,
            PositionY = 0f,
            PositionZ = 0f,
            RotationX = 0f,
            RotationY = 0f,
            RotationZ = 0f,
            RotationW = 1f,
            InventoryItems = "[]",
            IsInvicible = false
        };

        string fileName = saveName + ".db";
        dbPath = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(dbPath))
            File.Delete(dbPath);

        using (var db = new SQLiteConnection(dbPath))
        {
            db.CreateTable<SaveGameData>();
            db.Insert(initialRow);
        }

        OnSaveGame?.Invoke(saveName);
        StartCoroutine(WaitWhileSaveLoad());
    }

    private void DeleteOldSaves()
    {
        string[] files = GetAllFileNames();
        if (files.Length >= maxSaveCount) {
            string[] sortedFiles = files.OrderByDescending(f => File.GetCreationTime(f)).ToArray();
            int filesToDelete = files.Length - maxSaveCount + 1; // Keep the latest save
            foreach (string file in sortedFiles) {
                if (File.Exists(file)) {
                    Debug.Log($"Deleting old save file: {file}");
                    File.Delete(file);
                }
                filesToDelete--;
                if (filesToDelete <= 0)
                    break; // Stop deleting after reaching the limit
            }
        }
    }

    public void LoadGame()
    {
        string[] files = GetAllFileNames();
        string latestFile = files[0];
        dbPath = Path.Combine(Application.persistentDataPath, latestFile);

        Managers.Instance.GameManager.IsLoadingData = true;
        Managers.Instance.GameManager.LoadScene("LoadingScene");
        _ = Task.Run(() => LoadSave());
    }

    private async Task LoadSave()
    {
        var db = new SQLiteAsyncConnection(dbPath);
        await db.CreateTableAsync<SaveGameData>();
        SaveGameData[] savedData = await db.Table<SaveGameData>().ToArrayAsync();
        
        if (savedData == null || savedData.Length == 0) {
            UnityMainThreadDispatcher.Enqueue(() => Debug.LogError("Failed to load save data, no data found."));
            return;
        }

        // Load the scene specified in the save data
        UnityMainThreadDispatcher.Enqueue(() => StartCoroutine(SaveLoaded(savedData)));
    }

    private IEnumerator SaveLoaded(SaveGameData[] savedData)
    {
        sceneTask = Managers.Instance.GameManager.LoadScene(savedData[0].SceneName, LoadSceneMode.Additive);
        objectsToManageCount = savedData.Length - 1; // Account for the initial row
        saveName = savedData[0].SaveName;

        while (!sceneTask.isDone)
        {
            Debug.Log("Loading scene: " + savedData[0].SceneName);
            yield return null; // Wait until the scene is loaded
        }
        SceneManager.UnloadSceneAsync("LoadingScene");
        Debug.Log("Scene loaded: " + savedData[0].SceneName);
        OnLoadGame?.Invoke(savedData);
        StartCoroutine(WaitWhileSaveLoad());
    }

    public void SaveObjectData(SaveGameData saveData)
    {
        // Save the object data to a database or file
        Debug.Log($"Saving object data for {saveData.CharacterName} at position ({saveData.PositionX}, {saveData.PositionY}, {saveData.PositionZ})");

        if (saveData.SaveName != saveName) return;

        _ = Task.Run(() => SaveDataAsync(saveData));
    }

    private async Task SaveDataAsync(SaveGameData saveData)
    {
        if (dbPath == null) {
            return;
        }

        var db = new SQLiteAsyncConnection(dbPath);
        await db.CreateTableAsync<SaveGameData>();
        await db.InsertAsync(saveData);
        lock (saveLock) {
            ObjectsManaged++;
            savedObjectNames.Add(saveData.CharacterName);
        }
    }

    private string[] GetAllFileNames()
    {
        string saveDirectory = Application.persistentDataPath;
        if (!Directory.Exists(saveDirectory)) {
            Debug.LogError("Save directory does not exist: " + saveDirectory);
            return null;
        }
        var files = Directory.GetFiles(saveDirectory, "*.db");
        return files.OrderByDescending(f => File.GetCreationTime(f)).ToArray();
    }

    public bool IsSaveGameAvailable()
    {
        string[] files = GetAllFileNames();
        if (files == null || files.Length == 0) {
            return false;
        }
        return true;
    }

    private IEnumerator WaitWhileSaveLoad()
    {
        // Pause the game while loading
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.1f); // Wait for a short time to ensure pause is effective

        float timeElapsed = 0f;
        Debug.Log("Game paused for loading...");
        while (ObjectsManaged < objectsToManageCount)
        {
            timeElapsed += Time.unscaledDeltaTime;
            // Wait for a maximum of 5 seconds
            if (timeElapsed >= 15f) {
                Debug.LogWarning("Unable to load all game objects, loaded " + ObjectsManaged + "/" + objectsToManageCount);
                Debug.LogWarning("Loaded objects: " + string.Join(", ", savedObjectNames));
                break;
            }
            yield return null; // Wait until all objects are saved
        }

        DeleteOldSaves();

        // Resume the game after loading
        Time.timeScale = 1f;
        // Find the PauseMenu GameObject by name and tag
        GameObject pauseMenuObject = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(go =>
                go.name == "PauseMenu" &&
                go.scene.IsValid());
        if (pauseMenuObject != null) {
            pauseMenuObject.SetActive(false);
        }
        Debug.Log("Game resumed after loading.");

        Managers.Instance.GameManager.IsLoadingData = false;
        savedObjectNames.Clear(); // Clear the list of saved object names after loading
    }
}

public class SaveGameData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string SceneName { get; set; }
    public string SaveName { get; set; }
    public string CharacterName { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Stamina { get; set; }
    public int MaxStamina { get; set; }
    public int Level { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public float RotationW { get; set; }
    public string InventoryItems { get; set; } // JSON string of inventory items
    public bool IsInvicible { get; set; }
}
