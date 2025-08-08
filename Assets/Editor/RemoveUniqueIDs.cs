using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RemoveUniqueIDs : MonoBehaviour
{
    private static readonly HashSet<string> assignedIDs = new();
    private static readonly string idFilePath = "Assets/Editor/UniqueIDs.json";

    static RemoveUniqueIDs()
    {
        LoadAssignedIDs();
    }

    [MenuItem("Tools/Generate Unique IDs")]
    public static void GenerateUniqueIDs()
    {
        LoadAssignedIDs(); // Always load latest before generating

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects) {
            if (obj.CompareTag("Collectables") || obj.CompareTag("Chest")) {
                Transform[] children = obj.GetComponentsInChildren<Transform>();
                foreach (Transform child in children) {
                    if (child.TryGetComponent<CollectableStats>(out var stats) && !stats.IDGenerated) {
                        stats.UniqueIdentifier = GenerateUniqueID();
                        stats.IDGenerated = true;
                    } else if (stats && stats.IDGenerated) {
                        assignedIDs.Add(stats.UniqueIdentifier);
                    }
                }
            }
        }

        SaveAssignedIDs(); // Save after generating
    }

    private static string GenerateUniqueID()
    {
        string newID;
        do {
            newID = Random.Range(100000000, 999999999).ToString();
        } while (!assignedIDs.Add(newID)); // Add returns false if already present
        return newID;
    }

    private static void LoadAssignedIDs()
    {
        assignedIDs.Clear();
        if (File.Exists(idFilePath)) {
            var json = File.ReadAllText(idFilePath);
            var list = JsonUtility.FromJson<IdListWrapper>(json);
            if (list != null && list.ids != null) {
                foreach (var id in list.ids) {
                    assignedIDs.Add(id);
                }
            }
        }
    }

    private static void SaveAssignedIDs()
    {
        var list = new IdListWrapper { ids = new List<string>(assignedIDs) };
        var json = JsonUtility.ToJson(list, true);
        File.WriteAllText(idFilePath, json);
        AssetDatabase.Refresh();
    }

    [System.Serializable]
    private class IdListWrapper
    {
        public List<string> ids;
    }
}
