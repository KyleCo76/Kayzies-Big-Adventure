using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RandomizedCollectable : CollectableStats
{
    protected bool chestRandomizer = false;
    private bool ShowStats => !isChest || chestRandomizer;
    [ShowIf("ShowStats"), ToggleGroup("randomizeSprite"), SerializeField]
    protected bool randomizeSprite = false;
    [ShowIf("ShowStats"), ToggleGroup("randomizeSprite"), SerializeField, Tooltip("Resource folder containing sprites for the collectable.")]
    protected string spriteResourceFolder = null;
    [ShowIf("ShowStats"), ToggleGroup("randomizeSprite"), SerializeField, Tooltip("Randomize the size of the sprite.")]
    protected bool randomizeSpriteSize = false;
    [ShowIf("ShowStats"), ShowIf("randomizeSpriteSize"), ToggleGroup("randomizeSprite"), SerializeField, Tooltip("Minimum size for the sprite.")]
    protected Vector2 minSpriteSize = new(0.5f, 0.5f);
    [ShowIf("ShowStats"), ShowIf("randomizeSpriteSize"), ToggleGroup("randomizeSprite"), SerializeField, Tooltip("Maximum size for the sprite.")]
    protected Vector2 maxSpriteSize = new(1.5f, 1.5f);

    [ShowIf("ShowStats"), ToggleGroup("randomizeStats"), SerializeField]
    protected bool randomizeStats = false;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Minimum value for health amount.")]
    protected float minHealthAmount = 0.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Maximum value for health amount.")]
    protected float maxHealthAmount = 100.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Minimum value for max health boost.")]
    protected float minMaxHealthBoost = 0.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Maximum value for max health boost.")]
    protected float maxMaxHealthBoost = 50.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Minimum value for stamina amount.")]
    protected float minStaminaAmount = 0.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Maximum value for stamina amount.")]
    protected float maxStaminaAmount = 100.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Minimum value for max stamina boost.")]
    protected float minMaxStaminaBoost = 0.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Maximum value for max stamina boost.")]
    protected float maxMaxStaminaBoost = 50.0f;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Minimum value for cash amount. Must be positive.")]
    protected int minCashAmount = 0;
    [ShowIf("randomizeStats"), ToggleGroup("randomizeStats"), SerializeField, Tooltip("Maximum value for cash amount. Must be positive.")]
    protected int maxCashAmount = 1000;

    [ShowIf("ShowStats")]
    private SpriteRenderer spriteRenderer;


    protected override void Awake()
    {
        if (!TryGetComponent<SpriteRenderer>(out spriteRenderer)) {
            Debug.LogError("RandomizedCollectable requires a SpriteRenderer component. Please add one to " + gameObject.name);
        }
        if (randomizeSprite) {
            RandomizeSpriteImage();
        }
        if (randomizeSpriteSize) {
            RandomizeSpriteSize();
        }
        if (randomizeStats) {
            RandomizeStats();
        }
        base.Awake();
    }

    /// <summary>
    /// Ensures the object's state is consistent by validating and updating the value of the <see
    /// cref="statsRandomized"/> field based on the <see cref="randomizeStats"/> property.
    /// </summary>
    /// <remarks>This method is automatically invoked by the Unity Editor when the object's properties are
    /// modified in the Inspector. It synchronizes the <see cref="statsRandomized"/> field to reflect the current state
    /// of <see cref="randomizeStats"/>.</remarks>
    private void OnValidate()
    {
        if (randomizeStats) {
            statsRandomized = true;
        } else {
            statsRandomized = false;
        }
    }

    /// <summary>
    /// Randomizes the sprite displayed by the associated <see cref="SpriteRenderer"/>  by selecting a random sprite
    /// from the specified resource folder.
    /// </summary>
    /// <remarks>This method loads all sprites from the folder specified by <c>spriteResourceFolder</c>  and
    /// assigns one at random to the <see cref="SpriteRenderer"/> component attached to the GameObject.  If no <see
    /// cref="SpriteRenderer"/> is found, a warning is logged and no sprite is applied.</remarks>
    private void RandomizeSpriteImage()
    {
        // Ensure the spriteResourceFolder is not null or empty before loading sprites
        if (string.IsNullOrEmpty(spriteResourceFolder)) {
            return;
        }
        Sprite[] assets = Resources.LoadAll<Sprite>(spriteResourceFolder);
        if (assets.Length == 0) {
            Debug.LogWarning("No sprites found in the specified resource folder: " + spriteResourceFolder + ". On " + gameObject.name);
            return;
        }

        int spriteIndex = Random.Range(0, assets.Length);
        if (spriteRenderer != null) {
            spriteRenderer.sprite = assets[spriteIndex];
        } else {
            Debug.LogWarning("No SpriteRenderer found on " + gameObject.name + ". Randomized sprite will not be applied.");
        }
    }
    protected void RandomizeSpriteImage(GameObject loot)
    {
        // Ensure the spriteResourceFolder is not null or empty before loading sprites
        if (string.IsNullOrEmpty(spriteResourceFolder)) {
            return;
        }
        Sprite[] assets = Resources.LoadAll<Sprite>(spriteResourceFolder);
        if (assets.Length == 0) {
            Debug.LogWarning("No sprites found in the specified resource folder: " + spriteResourceFolder + ". On " + loot.name);
            return;
        }

        int spriteIndex = Random.Range(0, assets.Length);
        if (loot.TryGetComponent<SpriteRenderer>(out var renderer)) {
            renderer.sprite = assets[spriteIndex];
        } else {
            Debug.LogWarning("No SpriteRenderer found on " + loot.name + ". Randomized sprite will not be applied.");
        }
    }

    /// <summary>
    /// Randomizes the size of the sprite associated with the <see cref="SpriteRenderer"/> component on the current
    /// GameObject, within the specified minimum and maximum size ranges.
    /// </summary>
    /// <remarks>This method retrieves the <see cref="SpriteRenderer"/> component attached to the GameObject
    /// and assigns a random size to its <see cref="SpriteRenderer.size"/> property. The random size is determined by
    /// generating random values for the width and height within the ranges defined by <c>minSpriteSize</c> and
    /// <c>maxSpriteSize</c>. <para> If no <see cref="SpriteRenderer"/> is found on the GameObject, a warning is logged,
    /// and no changes are made. </para></remarks>
    private void RandomizeSpriteSize()
    {
        Vector2 randomSize = new(
            Random.Range(minSpriteSize.x, maxSpriteSize.x),
            Random.Range(minSpriteSize.y, maxSpriteSize.y)
        );
        this.transform.localScale = randomSize;
    }
    /// <summary>
    /// Randomizes the size of the specified loot object's sprite within a defined range.
    /// </summary>
    /// <remarks>The method adjusts the <see cref="Transform.localScale"/> of the provided loot object to a
    /// random size. The size is determined by generating random values within the ranges defined by
    /// <c>minSpriteSize</c> and <c>maxSpriteSize</c>. Ensure that <c>minSpriteSize</c> and <c>maxSpriteSize</c> are
    /// properly initialized before calling this method.</remarks>
    /// <param name="loot">The loot object whose sprite size will be randomized. Must not be <see langword="null"/>.</param>
    protected void RandomizeSpriteSize(GameObject loot)
    {
        Vector2 randomSize = new(
            Random.Range(minSpriteSize.x, maxSpriteSize.x),
            Random.Range(minSpriteSize.y, maxSpriteSize.y)
        );
        loot.transform.localScale = randomSize;
    }

    /// <summary>
    /// Randomizes the character's stats within predefined ranges.
    /// </summary>
    /// <remarks>This method assigns random values to the character's health, stamina, and cash amounts,  as
    /// well as their respective maximum boost values. The random values are generated within  the specified minimum and
    /// maximum ranges for each stat.</remarks>
    private void RandomizeStats() { 
        healthAmount = Random.Range(minHealthAmount, maxHealthAmount);
        maxHealthBoost = Random.Range(minMaxHealthBoost, maxMaxHealthBoost);
        staminaAmount = Random.Range(minStaminaAmount, maxStaminaAmount);
        maxStaminaBoost = Random.Range(minMaxStaminaBoost, maxMaxStaminaBoost);
        cashAmount = Random.Range(minCashAmount, maxCashAmount + 1); // Add 1 to include maxCashAmount in the range due to Random.Range's exclusive upper bound behavior with integers
    }

    /// <summary>
    /// Randomizes the stats of the specified loot item.
    /// </summary>
    /// <remarks>This method assigns random values to the properties of the <see
    /// cref="RandomizedCollectable"/> component attached to the specified loot item. The random values are generated
    /// within predefined ranges for each stat. If the <paramref name="loot"/> does not have a <see
    /// cref="RandomizedCollectable"/> component, no changes are made.</remarks>
    /// <param name="loot">The loot item whose stats will be randomized. Must have a <see cref="RandomizedCollectable"/> component.</param>
    protected void RandomizeStats(GameObject loot) {
        if (loot.TryGetComponent<RandomizedCollectable>(out var lootStats)) {
            lootStats.healthAmount = Random.Range(minHealthAmount, maxHealthAmount);
            lootStats.maxHealthBoost = Random.Range(minMaxHealthBoost, maxMaxHealthBoost);
            lootStats.staminaAmount = Random.Range(minStaminaAmount, maxStaminaAmount);
            lootStats.maxStaminaBoost = Random.Range(minMaxStaminaBoost, maxMaxStaminaBoost);
            lootStats.cashAmount = Random.Range(minCashAmount, maxCashAmount + 1);// Add 1 to include maxCashAmount in the range due to Random.Range's exclusive upper bound behavior with integers
        }
    }

}
