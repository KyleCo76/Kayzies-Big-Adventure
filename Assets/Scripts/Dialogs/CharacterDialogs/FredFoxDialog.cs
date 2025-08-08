using System.Collections.Generic;
using UnityEngine;


public class FredFoxDialog : DialogSkeleton
{
    [SerializeField] Transform houseDoor;
    private BoxCollider2D houseDoorCollider;


    protected void Awake()
    {
        if (houseDoor.TryGetComponent<BoxCollider2D>(out houseDoorCollider)) {
            houseDoorCollider.enabled = false;
        }
        className = "FredFoxDialog";
    }

    protected override void LoadedScene()
    {
        if (variableStorage.TryGetValue("doorOpen", out var value) && value is bool doorOpen && doorOpen) {
            OpenDoor();
        }        
    }

    protected override void DialogEnded(DialogData dialogData)
    {
        base.DialogEnded(dialogData);
        if (variableStorage != null && variableStorage.TryGetValue("hadConvoIndoors", out object value)) {
            if (value is bool hadConvoIndoors && hadConvoIndoors) { // Check if retrieved value is a boolean and if it is true
                OpenDoor();
            }
        }
    }

    private void OpenDoor()
    {
        houseDoor.rotation = Quaternion.Euler(houseDoor.rotation.eulerAngles.x, 70f, houseDoor.rotation.eulerAngles.z);
        houseDoor.TryGetComponent<Animator>(out Animator animator);
        houseDoorCollider.enabled = true;
        animator.enabled = false;
    }

    protected override void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !other.TryGetComponent<CollectableManager>(out var collectableManager) || !other.gameObject.GetComponent<Kayzie.Player.PlayerControllerV2>().IsInteracting) {
            return;
        }

        // Ensure neccessary variables are initialized in the variableStorage dictionary and set their values based on CollectedItems
        if (!variableStorage.ContainsKey("FredFoxGold")) {
            variableStorage.Add("FredFoxGold", false);
        }
        variableStorage["FredFoxGold"] = collectableManager.CollectedItems.Contains("FredFoxGold");
        if (!variableStorage.ContainsKey("FredFoxHealth")) {
            variableStorage.Add("FredFoxHealth", false);
        }
        variableStorage["FredFoxHealth"] = collectableManager.CollectedItems.Contains("FredFoxHealth");
        if (!variableStorage.ContainsKey("FredFoxStamina")) {
            variableStorage.Add("FredFoxStamina", false);
        }
        variableStorage["FredFoxStamina"] = collectableManager.CollectedItems.Contains("FredFoxStamina");

        base.OnTriggerStay2D(other); // Call the base class method to handle dialog interactions
    }
}
