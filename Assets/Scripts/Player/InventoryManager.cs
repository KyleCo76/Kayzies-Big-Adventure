using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private int cashAmount = 0;
    [SerializeField] private GameObject floatingText; // Prefab for floating text
    [SerializeField] private GameObject coinsText;

    public int GetCurrentGold => cashAmount; // Property to get the current cash amount


    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowCoinsText(cashAmount); // Initialize the coins text display with the starting cash amount
    }

    public void LoadedScene()
    {
        ShowCoinsText(cashAmount);
    }

    public void AddCash(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot add negative cash amount.");
            return;
        }
        cashAmount += amount;
        ShowCoinsText(cashAmount); // Show the amount of coins added as floating text
        ShowCoinsBuff(amount); // Update the coins text display
        Debug.Log($"Added {amount} cash. Total cash: {cashAmount}");
    }

    public void SetCash(int _amount)
    {
        cashAmount = _amount;
    }

    private void ShowCoinsBuff(int amount)
    {
        if (floatingText != null) // Check if the floating text prefab is assigned  
        {
            Vector3 spawnPosition = new(transform.position.x + 0.5f, transform.position.y + 2f, transform.position.z); // Calculate the spawn position for the floating text  
            GameObject text = Instantiate(floatingText, spawnPosition, Quaternion.identity); // Instantiate the floating text prefab at the player's position
            TextMeshPro floatingChild = text.GetComponentInChildren<TextMeshPro>(); // Get the TextMeshPro component from the instantiated text prefab
            if (floatingChild != null) // Use TryGetComponent correctly  
            {
                floatingChild.text = amount > 0 ? $"+{amount}" : $"-{amount}"; // Format the message based on the amount
                floatingChild.color = new Color(1f, 0.84f, 0f); // Set color to gold (RGB: 255, 215, 0)
                SpriteRenderer floatingSprite = floatingChild.GetComponentInChildren<SpriteRenderer>(); // Get the SpriteRenderer component from the instantiated text prefab
                if (floatingSprite != null) // Check if the SpriteRenderer component exists  
                {
                    Sprite coinSprite = Resources.Load<Sprite>("Sprites/Icons/CoinsIcon"); // Load the coin sprite from Resources folder
                    floatingSprite.sprite = coinSprite; // Assign the coin sprite to the SpriteRenderer
                }
            } else {
                Debug.LogWarning("Floating text prefab does not have a TextMeshPro component attached.");
            }
                Destroy(text, 1f); // Destroy the floating text after 1 second  
        }
    }

    private void ShowCoinsText(int amount)
    {
        if (coinsText != null) // Check if the coins text GameObject is assigned  
        {
            coinsText.GetComponent<TextMeshProUGUI>().text = "$" + amount; // Update the text to show the current cash amount  
        }
        else
        {
            Debug.LogWarning("Coins text GameObject is not assigned in the InventoryManager.");
        }
    }
}
