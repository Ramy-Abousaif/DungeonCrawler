using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;
    
    [Header("Currency")]
    [SerializeField] private int gold = 0;
    
    [Header("Player Reference")]
    public PhysicsBasedCharacterController player;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        player = GameManager.Instance.Player;
    }

    public int GetGold()
    {
        return gold;
    }
    
    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"Gold changed by {amount}. Current gold: {gold}");
        
        // TODO: Maybe trigger UI update event
        OnGoldChanged?.Invoke(gold);
    }
    
    public void AddItem(ItemData itemData)
    {
        if (player == null)
        {
            Debug.LogWarning("Player reference not set in PlayerInventory!");
            return;
        }

        // Check if player already has this item
        foreach (ItemList itemSlot in player.items)
        {
            if (itemSlot.GetName() == itemData.ItemName)
            {
                // Stack item
                if (itemData.CanStack)
                {
                    itemSlot.stacks = Mathf.Min(itemSlot.stacks + 1, itemData.MaxStacks);
                    itemSlot.item.ApplyPickupEffects(player);
                    OnItemAdded?.Invoke(itemData);
                    Debug.Log($"Added {itemData.ItemName} to inventory (stacked)");
                    return;
                }
            }
        }

        // Add as new item
        ItemList newItemSlot = new ItemList(itemData, 1);
        player.items.Add(newItemSlot);
        newItemSlot.item.ApplyPickupEffects(player);
        OnItemAdded?.Invoke(itemData);
        Debug.Log($"Added {itemData.ItemName} to inventory");
    }
    
    public List<ItemList> GetPlayerItems()
    {
        return player != null ? player.items : new List<ItemList>();
    }
    
    public bool HasItem(ItemData itemData)
    {
        if (player == null) return false;
        
        foreach (ItemList itemSlot in player.items)
        {
            if (itemSlot.GetName() == itemData.ItemName)
                return true;
        }
        return false;
    }
    
    // Events for UI updates
    public System.Action<int> OnGoldChanged;
    public System.Action<ItemData> OnItemAdded;
}
