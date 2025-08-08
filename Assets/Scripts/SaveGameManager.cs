using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Kayzie.Player;

[Serializable]
public class SaveData
{
    public string sceneName;
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public bool playerIsFlipped;
    public float playerHealth;
    public float playerMaxHealth;
    public float playerStamina;
    public float playerMaxStamina;
    public int playerGold;
    public string[] collectedItems;
    public Vector3 oktoPosition;
    public Quaternion oktoRotation;
    public float oktoHealth;
    public float oktoMaxHealth;
    public string cameraName;
    public Vector3 cameraPosition;
    public float cameraOrthographicSize;
    public Dictionary<string, Dictionary<string, object>> dialogVariableStorage = new();
    public string[] enemies;
}

public class SaveGameManager : MonoBehaviour
{

    public void SaveGame()
    {
        string saveFile = GetSaveLocation(true);
        string data = GetDataForSave();

        File.WriteAllText(saveFile, data);
        Debug.Log($"Game saved to: {saveFile}");
    }

    //public void LoadGame()
    //{
    //    if (!TryGetComponent<GameManager>(out GameManager gameManager))
    //    {
    //        Debug.LogError("GameManager component not found in the scene.");
    //        return;
    //    }
    //    if (!File.Exists(GetSaveLocation(false)))
    //    {
    //        Debug.LogWarning("No save file found. Please save the game first.");
    //        return;
    //    }

    //    // Load the save data
    //    SaveData loadedData = GetSaveData();
    //    if (loadedData == null) {
    //        Debug.LogError("No save data found or failed to load.");
    //        return;
    //    }
    //    gameManager.LoadGame(loadedData);
    //}

    private SaveData GetSaveData()
    {
        string saveFile = GetSaveLocation(false);
        if (!File.Exists(saveFile))
        {
            Debug.LogWarning($"Save file not found at: {saveFile}");
            return null;
        }

        string json = File.ReadAllText(saveFile);
        SaveData loadedData = JsonConvert.DeserializeObject<SaveData>(json);
        Debug.Log("Game loaded from: " + saveFile);
        return loadedData;
    }

    public string GetSaveLocation(bool _forSave)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string savedGamesPath = Path.Combine(Application.persistentDataPath, "Kayzie's Big Adventure");
        string gameFolder;
        try {
            savedGamesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            gameFolder = Path.Combine(savedGamesPath, "Kayzie's Big Adventure");
        } catch {
            gameFolder = savedGamesPath;
        }
        if (_forSave && !Directory.Exists(gameFolder))
            Directory.CreateDirectory(gameFolder);

        return Path.Combine(gameFolder, "savefile.kay");
#else
        return Path.Combine(Application.persistentDataPath, "savefile.kay");
#endif
    }

    private string GetDataForSave()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject okto = GameObject.FindGameObjectWithTag("Okto");
        if (player == null || okto == null)
        {
            Debug.LogError("Player or Okto not found in the scene.");
            return string.Empty;
        }
        CameraManager cameraManager = FindAnyObjectByType<CameraManager>();
        if (cameraManager == null)
        {
            Debug.LogError("CameraManager not found in the scene.");
            return string.Empty;
        }
        DialogManager dialogManager = FindAnyObjectByType<DialogManager>();
        if (dialogManager == null)
        {
            Debug.LogError("DialogManager not found in the scene.");
            return string.Empty;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        string[] enemyNames = new string[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            enemyNames[i] = enemies[i].name;
        }
        SaveData currentSaveData = new() {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            playerPosition = player.transform.position,
            playerRotation = player.transform.rotation,
            playerIsFlipped = player.GetComponent<PlayerControllerV2>().IsFlipped,
            //playerHealth = player.GetComponent<PlayerHealthManager>().GetCurrentHealth,
            //playerMaxHealth = player.GetComponent<PlayerHealthManager>().GetMaxHealth,
            playerStamina = player.GetComponent<PlayerControllerV2>().GetCurrentStamina,
            playerMaxStamina = player.GetComponent<PlayerControllerV2>().GetMaxStamina,
            playerGold = player.GetComponent<InventoryManager>().GetCurrentGold,
            collectedItems = player.GetComponent<CollectableManager>().CollectedItems.ToArray(),
            oktoPosition = okto.transform.position,
            oktoRotation = okto.transform.rotation,
            oktoHealth = okto.GetComponent<OktoHealthManager>().GetCurrentHealth,
            oktoMaxHealth = okto.GetComponent<OktoHealthManager>().GetMaxHealth,
            cameraName = cameraManager.GetCurrentCamera().name,
            cameraPosition = cameraManager.GetCurrentCamera().transform.localPosition,
            cameraOrthographicSize = cameraManager.GetCurrentCamera().Lens.OrthographicSize,
            dialogVariableStorage = dialogManager.GetDialogVariableStorage(),
            enemies = enemyNames
        };

        var settings = new JsonSerializerSettings {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        ContractResolver = new FieldsOnlyContractResolver()
        };
        return JsonConvert.SerializeObject(currentSaveData, Formatting.Indented, settings);
    }
}

// Add this custom contract resolver class to the file
public class FieldsOnlyContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        // Only serialize public and private instance fields
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var properties = new List<JsonProperty>();
        foreach (var field in fields)
        {
            var property = base.CreateProperty(field, memberSerialization);
            property.Readable = true;
            property.Writable = true;
            properties.Add(property);
        }
        return properties;
    }
}
