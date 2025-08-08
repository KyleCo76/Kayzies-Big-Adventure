using Ink.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DialogManager : MonoBehaviour
{

    public delegate void DialogEventHandler(DialogData dialogData);

    public event DialogEventHandler OnDialogStart;
    public event DialogEventHandler OnDialogEnd;

    [FoldoutGroup("Dialog Images")]
    [Tooltip("The image for Kayzie to be used with the dialog selection screen")]
    [SerializeField] private Sprite kayzieImage;
    [FoldoutGroup("Dialog Images")]
    [SerializeField] private Sprite OktoImage;

    [FoldoutGroup("Input Actions")]
    [SerializeField] protected InputAction nextLineAction;
    [FoldoutGroup("Input Actions")]
    [SerializeField] protected InputAction cancelDialogAction;

    private GameObject dialogObject;
    private TextMeshProUGUI dialogBoxText;
    private UnityEngine.UI.Image dialogBoxImage;

    [FoldoutGroup("Text Speed Control")]
    [Tooltip("Initial speed of text writing when the dialog starts.")]
    [SerializeField] private float textSpeedStart = 0.3f;
    [FoldoutGroup("Text Speed Control")]
    [Tooltip("Final speed of text writing when the dialog ends.")]
    [SerializeField] private float textSpeedEnd = 0.12f;
    [FoldoutGroup("Text Speed Control")]
    [Tooltip("Speed at which each line is written in the dialog box. First line is first value, second line is second value, etc.")]
    [SerializeField] private float dialogLineTime;

    private Story currentStory; // Reference to the Ink story object that contains the dialog data
    private bool dialogIsPlaying = false;
    private GameObject[] choices;
    private UnityEngine.UI.Image kayzieBox;
    private TextMeshProUGUI[] choiceTexts; // Array to hold the text components for each choice

    private LTDescr dialogTween; // Reference to the LeanTween description for dialog animations
    private float writeSpeed; // Used by Tween to control the speed of text writing

    private DialogData currentDialog;
    private string currentLine;

    private bool displayingChoices = false; // Used to block NextLineTriggered if choices are being displayed
    private bool blockSelection = false; // Used to block selection of choices for a short duration

    private readonly Dictionary<string, Dictionary<string, object>> variableStorage = new(); // Dictionary to store dialog variables, parent string is the class name

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        nextLineAction.performed += NextLineTriggered;
        nextLineAction.Enable();
        cancelDialogAction.performed += ExitDialogTriggered;
        cancelDialogAction.Enable();
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        nextLineAction.performed -= NextLineTriggered;
        nextLineAction.Disable();
        cancelDialogAction.performed -= ExitDialogTriggered;
        cancelDialogAction.Disable();
    }

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == "LoadingScene" || SceneManager.GetActiveScene().name == "MainMenu") {
            return; // Skip initialization if the scene is the loading scene or main menu
        }
        FindComponents();
    }

    private void Start()
    {
        bool skipChoices = false;
        if (SceneManager.GetActiveScene().name == "LoadingScene" || SceneManager.GetActiveScene().name == "MainMenu") {
            skipChoices = true;
        }
        // Initialize the choices UI and its text components
        if (choices != null && !skipChoices) {
            int index = 0;
            foreach (GameObject choice in choices) {
                choiceTexts[index] = choices[index].GetComponentInChildren<TextMeshProUGUI>();
                index++;
            }
            if (choiceTexts.Length == 0) {
                Debug.LogError("No TextMeshProUGUI components found in choices UI!");
            }
        } else if (!skipChoices) {
            Debug.LogError("Choices UI is not assigned in the DialogManager!");
        }

        // Ensure dialog object is initially inactive
        if (dialogObject != null) {
            dialogObject.SetActive(false);
        }
    }

    /// <summary>
    /// Handles the initialization and setup of dialog components when a new scene is loaded.
    /// </summary>
    /// <remarks>This method is called automatically when a new scene is loaded. It finds and initializes the
    /// dialog box and its components, resets the dialog state, and prepares the dialog system for use in the new scene.
    /// The dialog box is initially hidden and the text is cleared.</remarks>
    /// <param name="scene">The scene that has been loaded.</param>
    /// <param name="mode">The mode in which the scene was loaded.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneManager.GetActiveScene().name == "LoadingScene" || SceneManager.GetActiveScene().name == "MainMenu") {
            return; // Skip initialization if the scene is the loading scene
        }
        FindComponents();
        PopulateButtons();
        // Reset dialog state when a new scene is loaded
        if (dialogObject != null) {
            dialogBoxText.text = "";
            dialogObject.SetActive(false);
        }
        currentStory = null;
        currentDialog = null;
        dialogIsPlaying = false;
        writeSpeed = textSpeedStart; // Reset write speed to initial value
        //HideChoices();
    }

    private void FindComponents()
    {
        dialogObject = GameObject.FindGameObjectWithTag("DialogBox");
        if (dialogObject == null) {
            Debug.LogWarning("Dialog box object not found in the scene. Ensure it has the tag 'DialogBox'.");
            return;
        }
        dialogBoxText = dialogObject.GetComponentInChildren<TextMeshProUGUI>();
        if (dialogBoxText == null) {
            Debug.LogError("Dialog box text component not found in the dialog object. Ensure it has a TextMeshProUGUI component.");
            return;
        }
        kayzieBox = GameObject.FindGameObjectWithTag("KayzieImageBox").GetComponent<UnityEngine.UI.Image>();
        if (kayzieBox == null) {
            Debug.LogError("Kayzie image box not found in the scene. Ensure it has the tag 'KayzieImageBox'.");
            return;
        }
        dialogBoxImage = GameObject.FindGameObjectWithTag("DialogImage").GetComponent<UnityEngine.UI.Image>();
        if (dialogBoxImage == null) {
            Debug.LogError("Dialog box image component not found in the dialog object. Ensure it has a Image component.");
            return;
        }
        GameObject buttonsParent = GameObject.FindGameObjectWithTag("DialogButtons");
        if (buttonsParent == null) {
            Debug.LogError("Dialog buttons parent object not found in the scene. Ensure it has the tag 'DialogButtons'.");
            return;
        }
        choices = buttonsParent.GetComponentsInChildren<UnityEngine.UI.Button>()
            .Select(button => button.gameObject)
            .ToArray();
        if (choices.Length == 0) {
            Debug.LogError("No dialog buttons found in the scene. Ensure buttons are children of the object with tag 'DialogButtons'.");
            return;
        }
    }

    /// <summary>
    /// Initializes and configures button components within a dialog object.
    /// </summary>
    /// <remarks>This method searches for button components named "Button0", "Button1", etc., within the
    /// specified dialog object. It assigns text components and configures click event listeners for each button found.
    /// If a button is not found, a warning is logged, and the method exits early. Ensure that buttons are correctly
    /// named to match the expected pattern.</remarks>
    private void PopulateButtons()
    {
        choiceTexts = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++) {
            string buttonName = $"Button{i}";
            choices[i] = FindChildByName(dialogObject, buttonName);
            if (choices[i] == null) {
                Debug.LogWarning($"Button {buttonName} not found in the dialog object. Ensure buttons are named correctly.");
                return;
            }
            choiceTexts[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();
            if (choices[i].TryGetComponent<UnityEngine.UI.Button>(out var buttonObject)) {
                int choiceIndex = i;
                buttonObject.onClick.RemoveAllListeners();
                buttonObject.onClick.AddListener(() => MakeChoice(choiceIndex));
            }
        }
    }

    /// <summary>
    /// Searches for a child GameObject by name within the specified parent GameObject's hierarchy.
    /// </summary>
    /// <remarks>This method performs a recursive search through all descendants of the parent
    /// GameObject.</remarks>
    /// <param name="parent">The parent GameObject to search within.</param>
    /// <param name="name">The name of the child GameObject to find.</param>
    /// <returns>The first GameObject found with the specified name, or <see langword="null"/> if no such GameObject exists.</returns>
    private GameObject FindChildByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform) {
            if (child.name == name)
                return child.gameObject;
            GameObject result = FindChildByName(child.gameObject, name);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Initiates a dialog sequence using the specified dialog data.
    /// </summary>
    /// <remarks>This method sets up the dialog environment by initializing the story, setting dialog
    /// variables, and displaying the dialog box with the specified image. It also triggers the dialog start event and
    /// begins the dialog sequence if the story can continue.</remarks>
    /// <param name="dialogData">The data containing the dialog information, including the Ink JSON story, dialog image, and optional variables.</param>
    public void StartDialog(DialogData dialogData)
    {
        if (dialogIsPlaying) {
            Debug.LogWarning("Dialog is already playing. Cannot start a new dialog until the current one ends.");
            return;
        }
        currentStory = dialogData.InkJSON; // Set the current story to the one provided in dialogData
        if (!currentStory.canContinue) {
            Debug.LogError("Invalid dialog data: The story cannot continue.");
            return;
        }
        // Trigger the dialog start event
        OnDialogStart?.Invoke(dialogData);


        // Initilize the Ink story variables if they exist
        if (dialogData.Variables != null && currentStory.variablesState != null) {
            foreach (var variable in dialogData.Variables) {
                // Only set the variable if it exists in the Ink script
                if (currentStory.variablesState.GlobalVariableExistsWithName(variable.Key)) {
                    currentStory.variablesState[variable.Key] = variable.Value;
                }

            }
            // Update the manager's variable storage if it exists, else create it
            var keys = dialogData.Variables.Keys.ToList();
            foreach (var variable in keys) {
                if (!variableStorage.ContainsKey(dialogData.className)) {
                    variableStorage[dialogData.className] = new Dictionary<string, object>();
                }
                variableStorage[dialogData.className][variable] = dialogData.Variables[variable]; // Store the variable in the dialog's class storage
            }
        }
        dialogBoxImage.sprite = dialogData.DialogImage;
        if (!dialogObject.activeInHierarchy) {
            dialogBoxText.text = ""; // Clear the dialog box text before showing new dialog
            dialogObject.SetActive(true);
        }

        ShowKayzieImage(false); // Hide Kayzie image at the start of the dialog

        currentDialog = dialogData;
        dialogIsPlaying = true;
        StartCoroutine(ShowLine());
    }

    /// <summary>
    /// Displays the next line of dialog from the story with a typewriter effect.
    /// </summary>
    /// <remarks>This method retrieves the next line of dialog from the current story and displays it
    /// character by character in the dialog box. The speed of the text display is controlled by the <c>writeSpeed</c>
    /// variable. Any ongoing text animation is canceled before starting a new one. Choices are hidden before displaying
    /// the line and can be updated after the line is fully displayed.</remarks>
    /// <returns></returns>
    private IEnumerator ShowLine()
    {
        HideChoices();
        currentLine = currentStory.Continue(); // Get the next line of dialog from the story
        if (string.IsNullOrEmpty(currentLine)) {
            Debug.LogWarning("Current line is empty or null. Ending dialog.");
            EndDialog(currentDialog);
            yield break; // Exit the coroutine if there is no line to display
        }

        // Check for Ink Tags to activate Kayzie Image for dialog response
        CheckForHeroDialog();

        dialogBoxText.text = "";
        // Call to get the tween for the current line
        if (dialogTween != null) LeanTween.cancel(dialogTween.id);
        StartCoroutine(WaitTillFrame());
        LineTween();

        // Display each character in the line with a delay based on textSpeed
        foreach (char letter in currentLine.ToCharArray()) {
            dialogBoxText.text += letter;
            yield return new WaitForSeconds(writeSpeed);
        }
        //DisplayChoices(); // Update choices UI after displaying the line
    }

    private void CheckForHeroDialog()
    {
        if (currentStory.currentTags.Count > 0) {
            bool showKayzie = false; // Flag to determine if Kayzie image should be shown
            foreach (string tag in currentStory.currentTags) {
                if (tag.Equals("Kayzie")) {
                    ShowOktoImage(false);
                    ShowKayzieImage(true); // Show Kayzie image if the tag is present
                    showKayzie = true;
                    break;
                } else if (tag.Equals("Okto")) {
                    ShowKayzieImage(false); // Hide Kayzie image if the tag is Okto
                    ShowOktoImage(true);
                    showKayzie = true;
                    break;
                }
                if (!showKayzie) {
                    ShowKayzieImage(false); // Hide Kayzie image if no relevant tag is found
                }
            }
        } else {
            ShowKayzieImage(false);
        }
    }

    /// <summary>
    /// Waits until the end of the current frame before continuing execution.
    /// </summary>
    /// <returns>An enumerator that can be used to control the coroutine's execution.</returns>
    private IEnumerator WaitTillFrame()
    {
        yield return new WaitForEndOfFrame();
    }

    /// <summary>
    /// Animates the transition of the text writing speed for the current dialog line.
    /// </summary>
    /// <remarks>The method adjusts the <c>writeSpeed</c> property over time based on the current line index.
    /// The speed of the transition is determined by predefined values for each line index.</remarks>
    private void LineTween()
    {
        dialogTween = LeanTween.value(gameObject, textSpeedStart, textSpeedEnd, dialogLineTime).setOnUpdate(val => writeSpeed = val);
    }

    /// <summary>
    /// Advances to the next line of dialog in the sequence.
    /// </summary>
    /// <remarks>If the current line is not the last in the dialog sequence, this method increments the line
    /// index  and displays the next line. If the last line has already been displayed, the dialog is hidden.</remarks>
    public void NextLine()
    {
        // If there are more lines to display, show the next line
        if (currentStory.canContinue) {
            StartCoroutine(ShowLine());
        }
        // If all lines have been displayed, hide the dialog
        else {
            EndDialog(currentDialog);
        }
    }

    /// <summary>
    /// Ends the current dialog session and updates the provided dialog data with the current state.
    /// </summary>
    /// <remarks>This method collects all variables from the current story and assigns them to the specified
    /// <paramref name="dialogData"/>. It then triggers the dialog end event and deactivates the dialog
    /// interface.</remarks>
    /// <param name="dialogData">The dialog data object to be updated with the current dialog's variables.</param>
    public void EndDialog(DialogData dialogData)
    {
        // Get all variables from the current story and assign them to the dialogData
        Dictionary<string, object> allVariables = new();
        if (currentStory?.variablesState != null) {
            foreach (string variableName in currentStory.variablesState) {
                allVariables[variableName] = currentStory.variablesState[variableName];
            }
        }

        // Update the master variable storage
        dialogData.Variables = allVariables;
        if (!variableStorage.TryGetValue(dialogData.className, out var storage)) {
            variableStorage.Add(dialogData.className, new());
            storage = variableStorage[dialogData.className];
        }
        foreach (var key in allVariables.Keys) {
            if (!storage.TryGetValue(key, out var value)) {
                storage.Add(key, value);
            }
            storage[key] = allVariables[key];
        }
        // Trigger the dialog end event
        OnDialogEnd?.Invoke(dialogData);

        dialogObject.SetActive(false);
        dialogBoxText.text = "";
        dialogIsPlaying = false;
    }

    /// <summary>
    /// Handles the event triggered when the user advances to the next line of dialog.
    /// </summary>
    /// <remarks>If the current line of dialog is fully displayed, this method advances to the next line.  If
    /// the current line is still being displayed, it immediately completes the display of the current line.</remarks>
    /// <param name="context">The callback context containing information about the input action that triggered the event.</param>
    public void NextLineTriggered(InputAction.CallbackContext context)
    {
        if (displayingChoices || !dialogIsPlaying) {
            return;
        }
        if (currentStory.currentChoices.Count > 0 && dialogBoxText.text == currentLine) {
            DisplayChoices();
            return;
        } else if (currentStory.currentChoices.Count > 0 && dialogBoxText.text != currentLine) {
            StopAllCoroutines();
            LeanTween.cancel(dialogTween.id);
            dialogBoxText.text = currentLine;
            return;
        }

        // If the current line is fully displayed, go to the next line
        if (dialogBoxText.text == currentLine) {
            NextLine();
        }
        // If the current line is still being displayed, finish it immediately
        else {
            StopAllCoroutines();
            LeanTween.cancel(dialogTween.id);
            dialogBoxText.text = currentLine;
        }
    }

    /// <summary>
    /// Handles the event triggered when an exit dialog action occurs.
    /// </summary>
    /// <param name="context">The context of the input action that triggered the exit dialog.</param>
    public void ExitDialogTriggered(InputAction.CallbackContext context)
    {
        if (!dialogIsPlaying) return;

        EndDialog(currentDialog); // Trigger a notification that the current dialog is ended
    }

    /// <summary>
    /// Displays the current set of choices to the user and updates the UI accordingly.
    /// </summary>
    /// <remarks>This method ensures that the UI elements are properly activated and updated to reflect the
    /// current choices available in the story. It also initiates a delay to prevent accidental selection and sets the
    /// first choice as the selected option. If there are more choices than available UI elements, an error is
    /// logged.</remarks>
    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        // Start a delay if there are choices to display to prevent accidental selection
        if (currentChoices.Count > 0) {
            StartCoroutine(ChoiceDisplaySelectionDelay());
            ShowKayzieImage(true);
        }

        // Safety check to ensure we have enough choice UI elements
        if (currentChoices.Count > choices.Length) {
            Debug.LogError("Not enough choice UI elements to display all choices.");
            return;
        }

        // Show the choices UI and update the text for each choice
        int index = 0;
        foreach (Choice choice in currentChoices) {
            choices[index].SetActive(true);
            choiceTexts[index].text = choice.text;
            index++;
        }

        dialogBoxText.text = ""; // Clear the dialog box text when displaying choices

        HideChoices(index);
        EventSystem.current.SetSelectedGameObject(choices[0]); // Set the first choice as selected
    }

    /// <summary>
    /// Controls the visibility of the Kayzie image within the dialog interface.
    /// </summary>
    /// <remarks>If the Kayzie image or box is not assigned, a warning is logged. When the Kayzie image is
    /// shown, the dialog box image is hidden, and vice versa.</remarks>
    /// <param name="showImage">A boolean value indicating whether to display the Kayzie image.  <see langword="true"/> to show the Kayzie
    /// image; otherwise, <see langword="false"/> to hide it.</param>
    private void ShowKayzieImage(bool showImage)
    {
        if (kayzieBox == null || kayzieImage == null)
            Debug.LogWarning("Kayzie image or box is not assigned in the DialogManager.");

        if (showImage) {
            kayzieBox.enabled = true;
            kayzieBox.sprite = kayzieImage; // Set the Kayzie image in the dialog box
            dialogBoxImage.enabled = false; // Hide the dialog box image to show Kayzie's image instead
        } else {
            kayzieBox.enabled = false;
            dialogBoxImage.enabled = true; // Show the dialog box image
        }
    }

    private void ShowOktoImage(bool showImage)
    {
        if (kayzieBox == null || kayzieImage == null)
            Debug.LogWarning("Kayzie image or box is not assigned in the DialogManager.");

        if (showImage) {
            kayzieBox.enabled = true;
            kayzieBox.sprite = OktoImage; // Set the Kayzie image in the dialog box
            dialogBoxImage.enabled = false; // Hide the dialog box image to show Kayzie's image instead
        } else {
            kayzieBox.enabled = false;
            dialogBoxImage.enabled = true; // Show the dialog box image
        }
    }

    /// <summary>
    /// Temporarily blocks selection for a short duration.
    /// </summary>
    /// <remarks>This method delays the ability to make a selection by 0.5 seconds, during which the selection
    /// is blocked.</remarks>
    /// <returns></returns>
    private IEnumerator ChoiceDisplaySelectionDelay()
    {
        blockSelection = true;
        yield return new WaitForSecondsRealtime(0.5f);
        blockSelection = false;
    }

    /// <summary>
    /// Hides the choices starting from the specified index.
    /// </summary>
    /// <remarks>If <paramref name="startIndex"/> is greater than 0, the method sets the internal state to
    /// indicate that choices are being displayed.</remarks>
    /// <param name="startIndex">The index from which to start hiding choices. Defaults to 0.</param>
    private void HideChoices(int startIndex = 0)
    {
        displayingChoices = startIndex > 0;

        for (int i = startIndex; i < choices.Length; i++) {
            choices[i].SetActive(false);
        }
    }

    /// <summary>
    /// Selects a choice from the current story based on the specified index.
    /// </summary>
    /// <remarks>If the selection is blocked, the method returns immediately without making a
    /// choice.</remarks>
    /// <param name="choiceIndex">The zero-based index of the choice to select. Must be within the range of available choices.</param>
    public void MakeChoice(int choiceIndex)
    {
        if (blockSelection) return;

        var choicesList = currentStory.currentChoices;
        if (choiceIndex < 0 || choiceIndex >= choicesList.Count) {
            return;
        }
        currentStory.ChooseChoiceIndex(choiceIndex);
        HideChoices();
        ShowKayzieImage(false);
        NextLine();
    }

    public Dictionary<string, Dictionary<string, object>> GetDialogVariableStorage()
    {
        return variableStorage;
    }

    public void SetDialogVariableStorage(Dictionary<string, Dictionary<string, object>> storage)
    {
        if (storage != null) {
            variableStorage.Clear();
            foreach (var entry in storage) {
                variableStorage[entry.Key] = new Dictionary<string, object>(entry.Value);
            }

            // Attempt to get a reference to the classes defined in the variable storage and update ther variables
            foreach (string className in variableStorage.Keys) {
                Type type = Type.GetType(className);
                if (type == null) {
                    Debug.LogError($"Type {className} not found. Ensure it is defined in the project.");
                    continue;
                }
                var classScripts = GameObject.FindObjectsByType(type, FindObjectsSortMode.None);
                if (classScripts.Length != 1) {
                    Debug.LogError($"No instances of class {className} found in the scene.");
                    continue;
                }
                var classScript = classScripts[0];
                var methodName = "SetDialogVariableStorage";
                var method = classScript.GetType().GetMethod(methodName);
                if (method == null) {
                    Debug.LogError($"Method {methodName} not found in class {className}. Ensure it is defined correctly.");
                    continue;
                }
                object[] parameters = { variableStorage[className] };

                // Send the variable dictionary to the class instance
                method.Invoke(classScript, parameters);

            }
        }
    }
}

/// <summary>
/// Represents the data associated with a dialog, including the dialog image, character name, and story content.
/// </summary>
/// <remarks>The <see cref="DialogData"/> class is used to encapsulate all necessary information for a dialog
/// sequence, including the associated image, character name, and story content in the form of an Ink story. It provides
/// constructors to initialize dialog data with or without additional variable statistics.</remarks>
public class DialogData
{
    private readonly int maxDialogCharacterCount = 256; // Maximum number of characters allowed in a dialog line

    [Tooltip("The image associated with the dialog.")]
    public Sprite DialogImage { get; }
    [Tooltip("The Ink story JSON containing the dialog content.")]
    public Story InkJSON { get; }
    [Tooltip("The name of the character associated with the dialog.")]
    public string DialogCharacterName { get; }
    [Tooltip("A dictionary containing variable statistics for the dialog, if any.")]
    public Dictionary<string, object> Variables;
    public string className;


    /// <summary>
    /// Initializes a new instance of the <see cref="DialogData"/> class with the specified dialog image, story, and
    /// character name.
    /// </summary>
    /// <remarks>If the dialog lines in <paramref name="inkStory"/> do not pass validation, the constructor
    /// will return without initializing the instance.</remarks>
    /// <param name="imageForDialog">The image associated with the dialog.</param>
    /// <param name="inkStory">The story content for the dialog. Must pass validation for dialog line length.</param>
    /// <param name="dialogCharacterName">The name of the character associated with the dialog.</param>
    public DialogData(Sprite imageForDialog, Story inkStory, string dialogCharacterName, string nameOfClass)
    {
        if (!ValidateDialogLinesLength(inkStory)) {
            return;
        }
        DialogImage = imageForDialog;
        InkJSON = inkStory;
        Variables = null; // Initialize with an empty array if no variables are provided
        DialogCharacterName = dialogCharacterName;
        className = nameOfClass;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogData"/> class with the specified dialog image, story,
    /// variables, and character name.
    /// </summary>
    /// <remarks>The constructor validates the length of dialog lines in the provided <paramref
    /// name="inkStory"/>. If the validation fails, the constructor returns without initializing the instance.</remarks>
    /// <param name="imageForDialog">The image associated with the dialog.</param>
    /// <param name="inkStory">The story content for the dialog, represented as an <see cref="Story"/> object.</param>
    /// <param name="variableStats">A dictionary containing variable statistics relevant to the dialog.</param>
    /// <param name="dialogCharacterName">The name of the character associated with the dialog.</param>
    public DialogData(Sprite imageForDialog, Story inkStory, Dictionary<string, object> variableStats, string dialogCharacterName, string nameOfClass)
    {
        if (!ValidateDialogLinesLength(inkStory)) {
            return;
        }
        DialogImage = imageForDialog;
        InkJSON = inkStory;
        Variables = variableStats;
        DialogCharacterName = dialogCharacterName;
        className = nameOfClass;
    }

    /// <summary>
    /// Validates that each dialog line in the given story does not exceed the maximum character count.
    /// </summary>
    /// <remarks>This method checks each line of dialog in the story and logs an error if any line exceeds the
    /// specified character limit. The story's state is preserved and restored after validation.</remarks>
    /// <param name="inkStory">The story to validate, represented by an instance of the <see cref="Story"/> class.</param>
    /// <returns><see langword="true"/> if all dialog lines are within the allowed character limit; otherwise, <see
    /// langword="false"/>.</returns>
    private bool ValidateDialogLinesLength(Story inkStory)
    {
        // Save current story state
        var originalState = inkStory.state.ToJson();

        int lineNumber = 1;
        while (inkStory.canContinue)
        {
            string line = inkStory.Continue();
            if (line.Length > maxDialogCharacterCount)
            {
                Debug.LogError($"Dialog line {lineNumber} exceeds the maximum character count of {maxDialogCharacterCount} characters.");
                return false;
            }
            lineNumber++;
        }
        inkStory.state.LoadJson(originalState); // Restore original state after validation
            return true;
    }

}
