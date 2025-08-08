using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public struct CollectableData
{
    [Tooltip("Amount of health to restore when collected.")]
    public float healthAmount;
    [Tooltip("Amount to increase player's max health when collected.")]
    public float maxHealthBoost;
    [Tooltip("Amount of stamina to restore when collected.")]
    public float staminaAmount;
    [Tooltip("Amount to increasase player's max stamina when collected.")]
    public float maxStaminaBoost;
    [Tooltip("Amount of coins to collect. Must be positive")]
    public int cashAmount;
    [Tooltip("Uniqufier for the collectable data, used for identification.")]
    public string uniqueIdentifier;
}
public class CollectableStats : MonoBehaviour
{

    protected bool isChest = false; // Used by derived classes to determine if the derived class is a chest
    protected bool changeStats = false; // Used by derived classes to determine if the stats should be changed

    [HideIf("isChest")]
    [SerializeField] private bool collectOnCollision = false;
    public bool CollectOnCollision => collectOnCollision;
    [ShowIf("collectOnCollision")]
    [FoldoutGroup("Collision Settings")]
    [Tooltip("Cooldown time in seconds before the collectable can be collected. Set to 0 for no cooldown.")]
    [SerializeField] private float collectableCooldown = 0.5f;
    private bool canCollect = false;
    public bool CanCollect => canCollect;
    private float collectableCooldownTimer = 0.0f;

    protected bool statsRandomized = false; // Used by derived classes to determine if stats are randomized
    [HideIf("statsRandomized")]
    [FoldoutGroup("Collectable Stats")]
    [SerializeField] protected float healthAmount = 0.0f;
    [HideIf("statsRandomized")]
    [FoldoutGroup("Collectable Stats")]
    [SerializeField] protected float maxHealthBoost = 0.0f;
    [HideIf("statsRandomized")]
    [FoldoutGroup("Collectable Stats")]
    [SerializeField] protected float staminaAmount = 0.0f;
    [HideIf("statsRandomized")]
    [FoldoutGroup("Collectable Stats")]
    [SerializeField] protected float maxStaminaBoost = 0.0f;
    [HideIf("statsRandomized")]
    [FoldoutGroup("Collectable Stats")]
    [SerializeField] protected int cashAmount = 0;
    [FoldoutGroup("Collectable Stats")]
    [Tooltip("Unique identifier for the collectable data, used for identification.")]
    public string UniqueIdentifier = string.Empty;
    public string GetUniqueID => UniqueIdentifier;
    public bool IDGenerated = false;

    /// <summary>
    /// Initializes the component and sets the initial state for collection behavior.
    /// </summary>
    /// <remarks>This method is called when the component is first initialized. If the  <see
    /// cref="collectableCooldown"/> is greater than 0.0 and <see cref="collectOnCollision"/>  is enabled, the object
    /// will be marked as not ready for collection and a cooldown timer  will be started.</remarks>
    protected virtual void Awake()
    {
        // Set canCollect based on the collectableCooldown value
        if (collectableCooldown > 0.0f && collectOnCollision) {
            canCollect = false;
            collectableCooldownTimer = collectableCooldown;
        } else {
            canCollect = true;
        }
        if (!IDGenerated) {
            UniqueIdentifier = GenerateUniqueID();
        }
    }

    private string GenerateUniqueID()
    {
        string newID;
        GameManager manager = GameObject.FindFirstObjectByType<GameManager>();
        if (manager == null) {
            Debug.LogError("CollectableStats: GameManager not found in the scene. Cannot generate unique ID.");
        }
        do {
            newID = Random.Range(100000000, 999999999).ToString();
        } while (!manager.RegisterCollectableID(newID));
        return newID;
    }

    /// <summary>
    /// Updates the state of the collectable cooldown timer and determines whether the collectable can be collected.
    /// </summary>
    /// <remarks>This method decreases the cooldown timer by the elapsed time since the last frame.  When the
    /// timer reaches zero, the collectable becomes available for collection, and the timer is reset to zero.</remarks>
    protected virtual void Update()
    {
        // Update the collectable cooldown timer if it is active and set canCollect to true when the timer reaches zero
        if (collectableCooldownTimer > 0.0f) {
            collectableCooldownTimer -= Time.deltaTime;
            if (collectableCooldownTimer <= 0.0f) {
                canCollect = true;
                collectableCooldownTimer = 0.0f;
            }
        }
    }

    /// <summary>
    /// Creates and returns a <see cref="CollectableData"/> object containing the current collectable values.
    /// </summary>
    /// <remarks>The returned <see cref="CollectableData"/> object includes only the non-zero values for
    /// health, stamina,  and cash-related properties. Properties with a value of zero are excluded from the
    /// result.</remarks>
    /// <returns>A <see cref="CollectableData"/> object populated with the current collectable values.  If all values are zero,
    /// the returned object will have no properties set.</returns>
    public CollectableData GetCollectableData()
    {
        CollectableData data = new() { };
        if (!Mathf.Approximately(healthAmount, 0.0f)) {
            data.healthAmount = healthAmount;
        }
        if (!Mathf.Approximately(maxHealthBoost, 0.0f)) {
            data.maxHealthBoost = maxHealthBoost;
        }
        if (!Mathf.Approximately(staminaAmount, 0.0f)) {
            data.staminaAmount = staminaAmount;
        }
        if (!Mathf.Approximately(maxStaminaBoost, 0.0f)) {
            data.maxStaminaBoost = maxStaminaBoost;
        }
        if (cashAmount > 0) {
            data.cashAmount = cashAmount;
        }
        if (!string.IsNullOrEmpty(UniqueIdentifier)) {
            data.uniqueIdentifier = UniqueIdentifier;
        }
        return data;
    }

    public bool SetStats(float healthAmount, float maxHealthBoost, float staminaAmount, float maxStaminaBoost, int cashAmount)
    {
        if (healthAmount < 0.0f || maxHealthBoost < 0.0f || staminaAmount < 0.0f || maxStaminaBoost < 0.0f || cashAmount < 0) {
            Debug.LogError("CollectableStats: Cannot set negative values for collectable stats.");
            return false; // Return false if any of the values are negative
        }
        Debug.Log("Setting stats: Health: " + healthAmount + ", Max Health Boost: " + maxHealthBoost +
            ", Stamina: " + staminaAmount + ", Max Stamina Boost: " + maxStaminaBoost + ", Cash: " + cashAmount);
        this.healthAmount = healthAmount;
        this.maxHealthBoost = maxHealthBoost;
        this.staminaAmount = staminaAmount;
        this.maxStaminaBoost = maxStaminaBoost;
        this.cashAmount = cashAmount;
        changeStats = true; // Set changeStats to true to indicate that the stats have been changed
        return true; // Return true to indicate that the stats have been set successfully
    }
}
