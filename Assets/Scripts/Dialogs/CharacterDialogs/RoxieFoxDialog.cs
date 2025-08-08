using System.Collections.Generic;
using UnityEngine;

public class RoxieFoxDialog : DialogSkeleton
{

    [SerializeField] InventoryManager inventoryManager;


    private void Awake()
    {
        className = "RoxieFoxDialog";
    }

    protected override void DialogEnded(DialogData dialogData)
    {
        base.DialogEnded(dialogData);
        if (variableStorage != null && variableStorage.TryGetValue("thanked", out object value1) && variableStorage.TryGetValue("convoCount", out object value2)) {
            if (value1 is bool thanked && thanked && value2 is int convoCount && convoCount == 1) { // Check if retrieved value is a boolean and if it is true
                inventoryManager.AddCash(100); // Add 100 cash to the player's inventory
            }
        }
        SneakThiefCheckAndDestroy();
    }

    protected override void LoadedScene()
    {
        SneakThiefCheckAndDestroy();
    }
    private void SneakThiefCheckAndDestroy()
    {
        if (variableStorage != null && variableStorage.TryGetValue("sneakThief", out object value)) {
            if (value is bool sneakThief && sneakThief) { // Check if retrieved value is a boolean and if it is true
                GameObject stamina = GameObject.Find("FredFoxStamina");
                if (stamina != null) {
                    Destroy(stamina); // Destroy the FredFoxStamina GameObject
                }
                GameObject health = GameObject.Find("FredFoxHealth");
                if (health != null) {
                    Destroy(health); // Destroy the FredFoxHealth GameObject
                }
                GameObject gold = GameObject.Find("FredFoxGold");
                if (gold != null) {
                    Destroy(gold); // Destroy the FredFoxGold GameObject
                }
            }
        }
    }
    protected override void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !other.TryGetComponent<CollectableManager>(out var collectableManager) || !other.gameObject.GetComponent<Kayzie.Player.PlayerControllerV2>().IsInteracting) {
            return;
        }
        variableStorage ??= new Dictionary<string, object>();

        // Ensure neccessary variables are initialized in the variableStorage dictionary and set their values based on CollectedItems
        if (!variableStorage.ContainsKey("thanked")) {
            variableStorage.Add("thanked", false);
        }
        if (!variableStorage.ContainsKey("convoCount")) {
            variableStorage.Add("convoCount", 0);
        }
        if (!variableStorage.ContainsKey("fredFoxGold")) {
            variableStorage.Add("fredFoxGold", false);
        }
        variableStorage["fredFoxGold"] = collectableManager.CollectedItems.Contains("fredFoxGold");
        if (!variableStorage.ContainsKey("fredFoxHealth")) {
            variableStorage.Add("fredFoxHealth", false);
        }
        variableStorage["fredFoxHealth"] = collectableManager.CollectedItems.Contains("fredFoxHealth");
        if (!variableStorage.ContainsKey("fredFoxStamina")) {
            variableStorage.Add("fredFoxStamina", false);
        }
        variableStorage["fredFoxStamina"] = collectableManager.CollectedItems.Contains("fredFoxStamina");

        base.OnTriggerStay2D(other); // Call the base class method to handle dialog interactions
    }
}
