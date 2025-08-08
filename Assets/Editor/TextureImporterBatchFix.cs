using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using System.Collections;

public class TextureImporterBatchFix : EditorWindow
{
#if UNITY_EDITOR
    private static EditorCoroutine currentCoroutine;
    private static bool cancelRequested = false;
#endif

    [MenuItem("Tools/Batch Fix Texture Filter Mode")]
    public static void FixAllTextures()
    {
#if UNITY_EDITOR
        if (currentCoroutine == null)
        {
            cancelRequested = false;
            currentCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(ProcessTextures());
        }
        else
        {
            Debug.LogWarning("Batch process is already running.");
        }
#else
        Debug.LogWarning("Editor Coroutines are not available in this Unity version.");
#endif
    }

    [MenuItem("Tools/Cancel Batch Fix Texture Filter Mode")]
    public static void CancelFixAllTextures()
    {
#if UNITY_EDITOR
        if (currentCoroutine != null)
        {
            cancelRequested = true;
            Debug.Log("Cancellation requested for batch texture fix.");
        }
        else
        {
            Debug.LogWarning("No batch process is running.");
        }
#endif
    }

#if UNITY_EDITOR
    private static IEnumerator ProcessTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture");
        int count = 0;
        int batchSize = 10;

        for (int i = 0; i < guids.Length; i++)
        {
            if (cancelRequested)
            {
                Debug.LogWarning("Batch texture fix cancelled by user.");
                break;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
                count++;
            }

            if (i % batchSize == 0)
            {
                yield return null;
            }
        }
        Debug.Log($"Batch fixed {count} textures.");
        currentCoroutine = null;
        cancelRequested = false;
    }
#endif
}
