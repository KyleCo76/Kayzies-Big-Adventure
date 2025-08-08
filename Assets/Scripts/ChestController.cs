using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class ChestController : RandomizedCollectable
{

    [ToggleGroup("spawnMultipleLoot", "Spawn Multiple Loot"), Tooltip("Should the chest spawn multiple of the prefabs?")]
    [SerializeField] private bool spawnMultipleLoot = false; // Should the chest spawn multiple of the prefabs?

    [ToggleGroup("spawnMultipleLoot"), Tooltip("Should the number of loot prefabs to spawn be randomized?")]
    [SerializeField] private bool randomizeLootCount = false; // Should the number of loot prefabs to spawn be randomized?

    private bool RandomSingle => spawnMultipleLoot && !randomizeLootCount; // Determines if the chest should spawn a single random loot prefab
    [ShowIf("RandomSingle"), ToggleGroup("spawnMultipleLoot"), Tooltip("The number of loot prefabs to spawn when the chest is opened.")]
    [SerializeField] private int lootCount = 1; // The number of loot prefabs to spawn when the chest is opened
    
    [ToggleGroup("spawnMultipleLoot"), ShowIf("randomizeLootCount"), Tooltip("The minimum number of loot prefabs to spawn when the chest is opened.")]
    [SerializeField] private int minLootCount = 1; // The minimum number of loot prefabs to spawn when the chest is opened
    
    [ToggleGroup("spawnMultipleLoot"), ShowIf("randomizeLootCount"), Tooltip("The maximum number of loot prefabs to spawn when the chest is opened.")]
    [SerializeField] private int maxLootCount = 3; // The maximum number of loot prefabs to spawn when the chest is opened
    
    [ToggleGroup("spawnMultipleLoot"), Tooltip("The prefabs to spawn multiple of when the chest is opened.")]
    [SerializeField] private GameObject[] lootPrefabsMultiple; // The prefabs to spawn multiple of when the chest is opened

    [Title("Settings")]
    [PropertyOrder(-5)]
    [FoldoutGroup("Settings")]
    [Tooltip("The delay before the chest is destroyed after being opened.")]
    [SerializeField] private float destroyDelay = 2f; // Delay before the chest is destroyed after being opened
    [FoldoutGroup("Settings")]
    [Tooltip("The sound to play when the chest is opened.")]
    [SerializeField] private AudioClip openSound; // Sound to play when the chest is opened
    [FoldoutGroup("Settings")]
    [Tooltip("The sound to play when the loot is dropped.")]
    [SerializeField] private AudioClip lootSound; // Sound to play when the chest is destroyed
    [FoldoutGroup("Settings")]
    [Tooltip("The volume of the loot sound when played.")]
    [SerializeField] private float lootSoundVolume = 1f; // Volume of the loot sound when played
    [FoldoutGroup("Settings")]
    [Tooltip("Should the chest set the stats of the loot prefabs?")]
    [SerializeField] private bool setLootStats = true; // Should the chest set the stats of the loot prefabs?
    [ShowIf("setLootStats")]
    [FoldoutGroup("Settings")]
    [Tooltip("Should the loot prefab stats be randomized?")]
    [SerializeField] private bool randomizeLootStats = false; // Should the loot prefab stats be randomized?

    [PropertyOrder(-1)]
    [Tooltip("Array of loot prefabs to instantiate when the chest is opened.")]
    [SerializeField] GameObject[] lootPrefabsSingle; // Array of loot prefabs to instantiate when the chest is opened

    private AudioSource audioComponent; // Reference to the AudioSource component for playing sounds


    private Animator animator; // Reference to the Animator component
    private bool isOpened = false;

    // Override Awake to prevent the base class from executing its Awake method
    protected override void Awake()
    {
    }

    // Override Update to prevent the base class from executing its Update method
    protected override void Update()
    {
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!TryGetComponent<Animator>(out animator)) // Try to get the Animator component
        {
            Debug.LogError("Animator component not found on the chest " + gameObject.name); // Log an error if not found
        }
        if (!TryGetComponent<AudioSource>(out audioComponent)) // Try to get the AudioSource component
        {
            Debug.LogError("AudioSource component not found on the chest " + gameObject.name + ", adding one."); // Log a warning if not found
        }
    }

    /// <summary>
    /// Opens the chest, triggering its animation and generating rewards if applicable.
    /// </summary>
    /// <remarks>This method ensures the chest is only opened once. If the chest is already opened, 
    /// subsequent calls to this method will have no effect. If an animator is assigned,  it triggers the "Open"
    /// animation and logs the chest opening. If no animator is set,  a warning is logged instead.</remarks>
    public void OpenChest()
    {
        if (isOpened) {
            return;
        }
        if (animator != null) {
            isOpened = true;
            animator.SetTrigger("Open");
            Debug.Log("Opening chest: " + gameObject.name); // Log the opening of the chest
            CreateRewards();
        }
        else {
            Debug.LogWarning("Animator is not set, cannot play open animation. " + gameObject.name);
        }
    }

    /// <summary>
    /// Initiates the destruction of an open chest after a specified delay.
    /// </summary>
    /// <remarks>This method starts a coroutine that destroys the chest after the delay defined by the
    /// <c>destroyDelay</c> field. Ensure that the chest is in an appropriate state to be destroyed before calling this
    /// method.</remarks>
    public void DestroyOpenChest()
    {
        StartCoroutine(DestroyChestWithDelay(destroyDelay)); // Start a coroutine to destroy the chest after a delay
    }

    /// <summary>
    /// Destroys the chest after a specified delay.
    /// </summary>
    /// <remarks>This method is a coroutine and must be started using <see
    /// cref="UnityEngine.MonoBehaviour.StartCoroutine"/>.</remarks>
    /// <param name="delay">The time, in seconds, to wait before destroying the chest. Must be a non-negative value.</param>
    /// <returns>An enumerator that performs the delay and then destroys the chest.</returns>
    private IEnumerator DestroyChestWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay
        DestroyChest(); // Call the method to destroy the chest
    }

    /// <summary>
    /// Destroys the chest GameObject.
    /// </summary>
    /// <remarks>This method removes the chest from the scene by destroying its associated GameObject. Ensure
    /// that any necessary cleanup or state updates are performed before calling this method, as the chest will no
    /// longer exist after this operation.</remarks>
    private void DestroyChest()
    {
        Destroy(gameObject); // Destroys the chest GameObject
    }

    /// <summary>
    /// Spawns loot items based on the configured loot prefabs and settings.
    /// </summary>
    /// <remarks>This method handles the instantiation of loot items at the chest's position. It supports
    /// spawning single or multiple loot items, with optional randomization of the number of items spawned. If no loot
    /// prefabs are assigned, the method logs a warning and exits without spawning any items.  Additional behaviors
    /// include: - Applying random offsets to the spawn position of each loot item. - Optionally setting loot stats
    /// using the <c>StatManager</c> method. - Moving spawned loot items using the <c>MoveObject</c> method. - Playing a
    /// loot sound effect if one is configured.</remarks>
    private void CreateRewards()
    {
        if (lootPrefabsSingle.Length == 0 && lootPrefabsMultiple.Length == 0) {
            Debug.LogWarning("No loot prefabs assigned to the chest " + gameObject.name);
            return; // Exit if no loot prefabs are assigned
        }
        Vector3 spawnOffset;
        foreach (GameObject lootPrefab in lootPrefabsSingle)
        {
            Debug.Log("Spawning loot prefab: " + lootPrefab.name); // Log the name of the loot prefab being spawned
            if (lootPrefab != null) {
                spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 1f), 0f); // Random spawn offset for the loot
                GameObject loot =  Instantiate(lootPrefab, transform.position + spawnOffset, Quaternion.identity); // Instantiate each loot prefab at the chest's position
                if (setLootStats)
                    StatManager(loot); // Call the StartSettingStats method to set the loot stats
                MoveObject(loot); // Call the MoveObject method to handle loot movement
            }
        }
        foreach (GameObject lootPrefab in lootPrefabsMultiple) {
            int lootSpawnCount = lootCount;
            if (randomizeLootCount) {
                lootSpawnCount = Random.Range(minLootCount, maxLootCount); // Randomize the number of loot prefabs to spawn
            }
            if (lootPrefab != null && spawnMultipleLoot) {
                for (int i = 0; i < lootSpawnCount; i++) { // Spawn multiple loot prefabs if spawnMultipleLoot is true
                    spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 1f), 0f); // Random spawn offset for the loot
                    GameObject loot = Instantiate(lootPrefab, transform.position + spawnOffset, Quaternion.identity); // Instantiate each loot prefab at the chest's position
                    if (setLootStats)
                        StatManager(loot); // Call the StartSettingStats method to set the loot stats
                    MoveObject(loot); // Call the MoveObject method to handle loot movement
                }
            }
        }
        PlaySound(lootSound, lootSoundVolume); // Play the loot sound if set
    }

    private void PlaySound(AudioClip audio, float volume)
    {
        if (audioComponent == null || audioComponent.isPlaying || audio == null) {
            return; // Exit if the AudioSource is not set, already playing, or the audio clip is null
        }

        audioComponent.clip = audio; // Set the audio clip to play
        audioComponent.volume = volume; // Set the volume of the audio clip
        audioComponent.Play(); // Play the audio clip
    }

    /// <summary>
    /// Configures the properties and stats of a loot object based on the current settings.
    /// </summary>
    /// <remarks>This method applies various configurations to the provided loot object, such as randomizing
    /// its sprite, size, or stats,  or setting predefined stats based on the current configuration flags. The behavior
    /// depends on the following conditions: <list type="bullet"> <item><description>If <see langword="true"/> for
    /// <c>randomizeSprite</c>, the sprite image of the loot is randomized.</description></item> <item><description>If
    /// <see langword="true"/> for <c>randomizeSpriteSize</c>, the size of the loot's sprite is
    /// randomized.</description></item> <item><description>If <c>setLootStats</c> is <see langword="true"/> and
    /// <c>randomizeStats</c> is <see langword="false"/>, predefined stats are applied to the loot.</description></item>
    /// <item><description>If <c>randomizeStats</c> is <see langword="true"/>, the stats of the loot are
    /// randomized.</description></item> </list> Ensure that the <paramref name="loot"/> object has a <see
    /// cref="CollectableStats"/> component attached; otherwise, the method will fail to configure stats.</remarks>
    /// <param name="loot">The loot object to configure. Must not be null and should have a <see cref="CollectableStats"/> component.</param>
    private void StatManager(GameObject loot)
    {
        if (randomizeSprite) {
            RandomizeSpriteImage(loot);
        }
        if (randomizeSpriteSize) {
            RandomizeSpriteSize(loot);
        }
        if (setLootStats && !randomizeStats) {
            Debug.Log("Setting loot stats for: " + loot.name); // Log the name of the loot prefab being set
            loot.GetComponent<CollectableStats>().SetStats(healthAmount, maxHealthBoost, staminaAmount, maxStaminaBoost, cashAmount); // Set the stats of the loot prefab
        } else if (randomizeStats) {
            Debug.Log("Randomizing loot stats for: " + loot.name); // Log the name of the loot prefab being randomized
            RandomizeStats(loot);
        }
    }

    /// <summary>
    /// Applies a random force to the specified loot object, causing it to move in a random direction.
    /// </summary>
    /// <remarks>If the specified <paramref name="loot"/> does not already have a <see cref="Rigidbody2D"/>
    /// component, one will be added dynamically. The applied force is randomized within predefined ranges for both
    /// direction and magnitude.</remarks>
    /// <param name="loot">The loot object to which the force will be applied. Must have a <see cref="Rigidbody2D"/> component, or one will
    /// be added automatically.</param>
    private void MoveObject(GameObject loot)
    {
        float xRangeMin = -0.1f;
        float xRangeMax = 0.1f;
        float yRangeMin = 0.2f;
        float yRangeMax = 0.6f;
        float multiplierMin = 0.1f;
        float multiplierMax = 0.2f;
        if (!loot.TryGetComponent<Rigidbody2D>(out var rb)) {
            Debug.LogWarning("Loot prefab does not have a Rigidbody2D component, adding one."); // Log a warning if the loot prefab does not have a Rigidbody2D component
            loot.AddComponent<Rigidbody2D>(); // Add a Rigidbody2D component to the loot prefab
            rb = loot.GetComponent<Rigidbody2D>(); // Get the newly added Rigidbody2D component
        }
        rb.AddForce(new Vector2(Random.Range(xRangeMin, xRangeMax), Random.Range(yRangeMin, yRangeMax)) * Random.Range(multiplierMin, multiplierMax), ForceMode2D.Impulse); // Apply a random force to the loot to make it fall
    }

    /// <summary>
    /// Validates and updates the state of the chest configuration based on the current settings.
    /// </summary>
    /// <remarks>This method is called to ensure that the chest-related properties are correctly synchronized 
    /// based on the values of <c>setLootStats</c>, <c>randomizeLootStats</c>, and other related fields.  It adjusts
    /// internal flags to control the visibility and behavior of options in the inspector,  such as stat randomization
    /// and sprite customization.</remarks>
    private void OnValidate()
    {
        isChest = true; // Set isChest to true for this class

        if (setLootStats) {
            if (randomizeLootStats) {
                chestRandomizer = true; // Show stat randomization options in the inspector
                if (!randomizeStats) {
                    statsRandomized = false; // Hide the default stats in the inspector
                } else {
                    statsRandomized = true; // Show the default stats in the inspector
                }
            } else {
                chestRandomizer = false; // Hide stat randomization options in the inspector
                statsRandomized = false; // Show the default stats in the inspector;
            }
        } else {
            chestRandomizer = false; // Hide stat randomization options in the inspector
            statsRandomized = true; // Hide the default stats in the inspector
            randomizeSprite = false; // Disable sprite randomization for chests
            randomizeSpriteSize = false; // Disable sprite size randomization for chests
        }
    }
}
