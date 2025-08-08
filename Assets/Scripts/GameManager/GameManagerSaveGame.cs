//using Kayzie.Player;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using Unity.Cinemachine;
//using Unity.Transforms;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public partial class GameManager : MonoBehaviour
//{

//    private void SaveGame()
//    {
//        if (!TryGetComponent<SaveGameManager>(out SaveGameManager saveGameManager)) {
//            Debug.LogError("SaveGameManager component not found on GameManager.");
//            return;
//        }
//        saveGameManager.SaveGame(); // Call the SaveGame method from SaveGameManager
//    }

//    public void LoadGame(SaveData saveData)
//    {
//        if (saveData == null) {
//            Debug.LogError("SaveData is null. Cannot load game.");
//            return;
//        }
//        isLoadingData = true;
//        // Load the scene specified in the save data
//        LoadSceneWithLoadingScreen(saveData.sceneName);
//        StartCoroutine(LoadGame2(saveData));
//    }

//    private IEnumerator LoadGame2(SaveData saveData)
//    {
//        // Wait until asyncLoad is assigned
//        //while (asyncLoad == null)
//        yield return null;
//        yield return null;

//        // Wait until the scene is fully loaded
//        while (!asyncLoad.isDone) {
//            Debug.Log("waiting");
//            yield return null;
//        }

//        // Optionally, set the scene as active
//        Scene targetScene = SceneManager.GetSceneByName(saveData.sceneName);
//        if (!targetScene.IsValid() || !targetScene.isLoaded) {
//            Debug.LogError($"Scene '{saveData.sceneName}' is not loaded or not valid.");
//            yield break;
//        }
//        //SceneManager.SetActiveScene(targetScene);

//        Debug.Log("LoadGame2: Scene loaded and active");

//        int found = 0;
//        GameObject[] rootObjects = targetScene.GetRootGameObjects();

//        foreach (GameObject obj in rootObjects) {
//            if (obj.CompareTag("Player")) {
//                SetPlayerData(obj, saveData);
//                found++;
//            } else if (obj.CompareTag("Okto")) {
//                SetOktoData(obj, saveData);
//                found++;
//            } else if (obj.CompareTag("MainCamera")) {
//                CinemachineCamera[] cameras = obj.GetComponentsInChildren<CinemachineCamera>(true);
//                foreach (var cinemachineCamera in cameras) {
//                    if (cinemachineCamera.name == saveData.cameraName) {
//                        cinemachineCamera.enabled = true;
//                        //cinemachineCamera.transform.SetPositionAndRotation(saveData.cameraPosition, Quaternion.identity);
//                        //cinemachineCamera.transform.localPosition = saveData.cameraPosition;
//                        cinemachineCamera.Lens.OrthographicSize = saveData.cameraOrthographicSize;
//                        found++;
//                    } else {
//                        cinemachineCamera.enabled = false;
//                    }
//                }
//            } else if (obj.CompareTag("Collectables")) {
//                var children = obj.GetComponentsInChildren<Transform>();
//                foreach (var child in children) {
//                    if (child.TryGetComponent<CollectableStats>(out var collectable)) {
//                        if (saveData.collectedItems.Contains(collectable.GetUniqueID)) {
//                            Destroy(child.gameObject);
//                            continue;
//                        }
//                    }
//                }
//            } else if (obj.CompareTag("Enemy") && saveData.enemies != null) {
//                var children = obj.GetComponentsInChildren<Transform>();
//                foreach (var child in children) {
//                    if (!saveData.enemies.Contains(child.name)) {
//                        Destroy(child.gameObject);
//                    }
//                }
//            } else if (obj.CompareTag("Chest")) {
//                if (obj.TryGetComponent<CollectableStats>(out var collectable)) {
//                    if (saveData.collectedItems.Contains(collectable.GetUniqueID)) {
//                        Destroy(obj);
//                        continue;
//                    }
//                }
//            }
//        }
//        DialogManager dialogManager = GameObject.FindAnyObjectByType<DialogManager>();
//        if (dialogManager != null) {
//            dialogManager.SetDialogVariableStorage(saveData.dialogVariableStorage);
//            Debug.Log("Dialog variable storage set successfully.");
//        } else {
//            Debug.LogError("DialogManager component not found in Managers object.");
//        }

//        if (found != 3) {
//            Debug.LogError("Failed to find Player, Okto, or main Cinemachine camera objects in the loaded scene.");
//        } else {
//            Debug.Log("Successfully loaded game data for Player and Okto.");
//        }

//        isLoadingData = false;
//    }

//    private void SetPlayerData(GameObject obj, SaveData saveData)
//    {
//        if (obj.TryGetComponent<PlayerControllerV2>(out var playerController)) {
//            playerController.SetHealth(saveData.playerHealth);
//            playerController.SetMaxHealth(saveData.playerMaxHealth);
//            playerController.SetStamina(saveData.playerStamina);
//            playerController.SetMaxStamina(saveData.playerMaxStamina);
//            playerController.SetFlippedState(saveData.playerIsFlipped);
//            Debug.Log("Stamina set to: " + saveData.playerStamina + " . Max Stamina set to " + saveData.playerMaxStamina);
//        } else {
//            Debug.LogError("PlayerController component not found on Player object.");
//        }
//        if (obj.TryGetComponent<InventoryManager>(out var inventoryManager)) {
//            inventoryManager.SetCash(saveData.playerGold);
//        } else {
//            Debug.LogError("InventoryManager component not found on Player object.");
//        }
//        if (obj.TryGetComponent<CollectableManager>(out var collectableManager)) {
//            collectableManager.SetCollectedItems(saveData.collectedItems);
//        } else {
//            Debug.LogError("CollectableManager component not found on Player object.");
//        }
//        obj.transform.SetPositionAndRotation(saveData.playerPosition, saveData.playerRotation);
//    }

//    private void SetOktoData(GameObject obj, SaveData saveData)
//    {
//        if (obj.TryGetComponent<OktoHealthManager>(out var oktoHealth)) {
//            oktoHealth.SetHealth(saveData.oktoHealth);
//            oktoHealth.SetMaxHealth(saveData.oktoMaxHealth);
//        } else {
//            Debug.LogError("OktoController component not found on Okto object.");
//        }
//        obj.transform.SetPositionAndRotation(saveData.oktoPosition, saveData.oktoRotation);
//    }
//}
