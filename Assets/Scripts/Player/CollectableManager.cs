using Kayzie.Player;
using System.Collections.Generic;
using UnityEngine;

public class CollectableManager : MonoBehaviour
{
    private readonly List<string> collectedItems = new();
    public List<string> CollectedItems => collectedItems;

    public void Collect(CollectableData _data)
    {
        PlayerControllerV2 playerController = this.gameObject.GetComponent<PlayerControllerV2>();
        // Process the collected data
        if (!Mathf.Approximately(_data.healthAmount, 0.0f))
        {
            playerController.AdjustHealth(true, _data.healthAmount);
            Debug.Log($"Restored {_data.healthAmount} health.");
        }
        
        if (!Mathf.Approximately(_data.maxHealthBoost, 0.0f))
        {
            playerController.ModifyMaxHealth(_data.maxHealthBoost);
            Debug.Log($"Increased max health by {_data.maxHealthBoost}.");
        }
        
        if (!Mathf.Approximately(_data.staminaAmount, 0.0f))
        {
            playerController.ChangeStamina(_data.staminaAmount);
            Debug.Log($"Restored {_data.staminaAmount} stamina.");
        }
        
        if (!Mathf.Approximately(_data.maxStaminaBoost, 0.0f))
        {
            playerController.ChangeMaxStamina(_data.maxStaminaBoost);
            Debug.Log($"Increased max stamina by {_data.maxStaminaBoost}.");
        }
        
        if (!Mathf.Approximately(_data.cashAmount, 0.0f))
        {
            this.gameObject.GetComponent<InventoryManager>().AddCash(_data.cashAmount);
            Debug.Log($"Collected {_data.cashAmount} coins.");
        }
        if (_data.uniqueIdentifier != null && _data.uniqueIdentifier != "")
        {
            ProcessUniqueID(_data.uniqueIdentifier);
        }
    }

    private void ProcessUniqueID(string _collectedItem)
    {
        collectedItems.Add(_collectedItem);
    }

    public void SetCollectedItems(string[] _items)
    {
        if (_items != null) {
            collectedItems.Clear();
            foreach (string item in _items)
            {
                if (!string.IsNullOrEmpty(item)) {
                    collectedItems.Add(item);
                }
            }
        }
    }

    public void SetCollectedItems(string _items)
    {
        collectedItems.Clear();
        if (!string.IsNullOrEmpty(_items))
        {
            string[] itemsArray = _items.Split(',');
            foreach (string item in itemsArray)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    collectedItems.Add(item.Trim());
                }
            }
        }
    }

    public string GetInventoryAsJson()
    {
        return JsonUtility.ToJson(new { collectedItems });
    }
}
