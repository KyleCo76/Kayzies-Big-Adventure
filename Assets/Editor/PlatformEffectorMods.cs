using UnityEditor;
using UnityEngine;

public class PlatformEffectorMods : MonoBehaviour
{
    [MenuItem("Tools/Platform Effector Mods")]
    public static void PlatformMods()
    {
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int changedCount = 0;
        foreach (GameObject obj in allObjects) {
            if (obj.CompareTag("Platforms")) {
                if (obj.TryGetComponent<PlatformEffector2D>(out PlatformEffector2D effector)) {
                    effector.useOneWay = true;
                    effector.useOneWayGrouping = true;
                    effector.surfaceArc = 45f;
                    effector.rotationalOffset = 0f;
                    effector.useColliderMask = false;
                } else {
                    Debug.LogWarning($"GameObject '{obj.name}' does not have a PlatformEffector2D component.");
                }
                changedCount++;
            }
        }
        Debug.Log($"Tagged {changedCount} objects on the 'Platforms' layer with the 'Platforms' tag.");
    }
}
