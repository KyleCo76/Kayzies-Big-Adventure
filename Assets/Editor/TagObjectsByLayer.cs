using UnityEditor;
using UnityEngine;

public class TagObjectsByLayer : MonoBehaviour
{
    [MenuItem("Tools/Tag Platforms By Layer")]
    public static void TagPlatforms()
    {
        int platformsLayer = LayerMask.NameToLayer("Platforms");
        if (platformsLayer == -1) {
            Debug.LogError("Layer 'Platforms' does not exist.");
            return;
        }

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int changedCount = 0;
        foreach (GameObject obj in allObjects) {
            if (obj.layer == platformsLayer && obj.TryGetComponent<PlatformEffector2D>(out var effector)) {
                effector.surfaceArc = 45f;
                
            }
        }
        Debug.Log($"Tagged {changedCount} objects on the 'Platforms' layer with the 'Platforms' tag.");
    }
}
