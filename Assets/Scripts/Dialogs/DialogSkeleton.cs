using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogSkeleton : MonoBehaviour
{
    [SerializeField] protected Sprite dialogImage; // Image to display in the dialog box, if needed
    [SerializeField] protected string characterName;
    [SerializeField] protected TextAsset inkJSON;

    protected bool hasDialog = false; // Flag to indicate if a dialog is currently active

    protected Dictionary<string, object> variableStorage = new();

    protected string className; // Class name for the dialog, used for identification


    protected virtual void OnEnable()
    {
        Managers.Instance.DialogManager.OnDialogStart += DialogStarted;
        Managers.Instance.DialogManager.OnDialogEnd += DialogEnded;
    }

    protected virtual void OnDisable()
    {
        Managers.Instance.DialogManager.OnDialogStart -= DialogStarted;
        Managers.Instance.DialogManager.OnDialogEnd -= DialogEnded;
    }

    protected virtual void DialogStarted(DialogData dialogData)
    {
    }

    /// <summary>
    /// Handles the completion of a dialog for the specified character.
    /// </summary>
    /// <remarks>This method updates the internal state by merging dialog-specific variables into the existing
    /// variable storage if the dialog is associated with the current character.</remarks>
    /// <param name="dialogData">The data associated with the dialog that has ended, including the character name and any dialog-specific
    /// variables.</param>
    protected virtual void DialogEnded(DialogData dialogData)
    {
        if (dialogData.DialogCharacterName != characterName) return;

        if (dialogData.Variables != null) {
            MergeDictionaries(dialogData.Variables, variableStorage ??= new Dictionary<string, object>());
            hasDialog = false;
        }
    }

    /// <summary>
    /// Merges the entries from the source dictionary into the target dictionary.
    /// </summary>
    /// <remarks>If a key from the source dictionary already exists in the target dictionary, the value in the
    /// target dictionary will be updated with the value from the source dictionary. If a key does not exist in the
    /// target dictionary, it will be added along with its value.</remarks>
    /// <param name="source">The dictionary containing the entries to be merged into the target.</param>
    /// <param name="target">The dictionary that will receive the entries from the source dictionary.</param>
    private void MergeDictionaries(Dictionary<string, object> source, Dictionary<string, object> target)
    {
        foreach (var kvp in source) {
            if (!target.ContainsKey(kvp.Key)) {
                target.Add(kvp.Key, kvp.Value);
            } else {
                target[kvp.Key] = kvp.Value; // Update existing key with new value
            }
        }
    }

    /// <summary>
    /// Handles the interaction when a player remains within the trigger collider.
    /// </summary>
    /// <remarks>This method initiates a dialog sequence if the object within the trigger is tagged as
    /// "Player"  and the player is interacting. It ensures that the dialog starts only once by checking the 
    /// <c>hasDialog</c> flag.</remarks>
    /// <param name="other">The <see cref="Collider2D"/> of the object that remains within the trigger area.</param>
    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (!hasDialog && other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<Kayzie.Player.PlayerControllerV2>().IsInteracting) {
            hasDialog = true;
            Managers.Instance.DialogManager.StartDialog(new DialogData(dialogImage, new Ink.Runtime.Story(inkJSON.text), variableStorage, characterName, className));
        }
    }

    public void SetDialogVariableStorage(Dictionary<string, object> storage)
    {
        variableStorage = storage;
        LoadedScene();
    }
    
    protected virtual void LoadedScene()
    {

    }
}
