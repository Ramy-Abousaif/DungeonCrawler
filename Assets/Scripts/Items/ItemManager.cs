using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Rarity Weight Distribution")]
    [Tooltip("Relative weight for common item rolls.")]
    public float commonWeight = 60f;
    [Tooltip("Relative weight for uncommon item rolls.")]
    public float uncommonWeight = 25f;
    [Tooltip("Relative weight for rare item rolls.")]
    public float rareWeight = 12f;
    [Tooltip("Relative weight for legendary item rolls.")]
    public float legendaryWeight = 3f;

    [System.Serializable]
    public class ItemEntry
    {
        public ItemData data;
        public ItemPickup pickupPrefab;

        public ItemEntry(ItemData itemData, ItemPickup prefab)
        {
            data = itemData;
            pickupPrefab = prefab;
        }
    }

    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();
    private Dictionary<string, ItemEntry> itemEntries = new Dictionary<string, ItemEntry>();
    private List<ItemEntry> commonItems = new List<ItemEntry>();
    private List<ItemEntry> uncommonItems = new List<ItemEntry>();
    private List<ItemEntry> rareItems = new List<ItemEntry>();
    private List<ItemEntry> legendaryItems = new List<ItemEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllItems();
    }

    void Start()
    {
        // SimulateDropChances();
    }

    private void OnValidate()
    {
        commonWeight = Mathf.Max(0f, commonWeight);
        uncommonWeight = Mathf.Max(0f, uncommonWeight);
        rareWeight = Mathf.Max(0f, rareWeight);
        legendaryWeight = Mathf.Max(0f, legendaryWeight);
    }

    private void LoadAllItems()
    {
        itemDatabase.Clear();
        itemEntries.Clear();
        commonItems.Clear();
        uncommonItems.Clear();
        rareItems.Clear();
        legendaryItems.Clear();

        GameObject[] allItems = Resources.LoadAll<GameObject>("Items");

        foreach (GameObject itemObject in allItems)
        {
            if (itemObject == null)
                continue;

            ItemPickup itemPickup = itemObject.GetComponent<ItemPickup>();
            if (itemPickup == null)
                continue;

            ItemData item = itemPickup.GetItemData();
            if (item == null)
                continue;

            ItemEntry entry = new ItemEntry(item, itemPickup);

            itemDatabase[item.ItemName] = item;
            itemEntries[item.ItemName] = entry;

            // Organize by rarity
            switch (item.Rarity)
            {
                case ItemRarity.COMMON:
                    commonItems.Add(entry);
                    break;
                case ItemRarity.UNCOMMON:
                    uncommonItems.Add(entry);
                    break;
                case ItemRarity.RARE:
                    rareItems.Add(entry);
                    break;
                case ItemRarity.LEGENDARY:
                    legendaryItems.Add(entry);
                    break;
            }
        }

        Debug.Log($"ItemManager: Loaded {itemEntries.Count} item entries from {allItems.Length} resources");
    }

    private void SimulateDropChances()
    {
        int trials = 100000;
        Dictionary<ItemRarity, int> rarityCounts = new Dictionary<ItemRarity, int>
        {
            { ItemRarity.COMMON, 0 },
            { ItemRarity.UNCOMMON, 0 },
            { ItemRarity.RARE, 0 },
            { ItemRarity.LEGENDARY, 0 }
        };

        for (int i = 0; i < trials; i++)
        {
            ItemEntry entry = GetRandomItemEntry();
            if (entry != null)
                rarityCounts[entry.data.Rarity]++;
        }

        Debug.Log("Simulated Drop Chances:");
        foreach (var kvp in rarityCounts)
        {
            float percentage = (float)kvp.Value / trials * 100f;
            Debug.Log($"{kvp.Key}: {percentage:F2}%");
        }
    }

    public ItemEntry GetRandomItemEntry()
    {
        float effectiveCommonWeight = commonItems.Count > 0 ? commonWeight : 0f;
        float effectiveUncommonWeight = uncommonItems.Count > 0 ? uncommonWeight : 0f;
        float effectiveRareWeight = rareItems.Count > 0 ? rareWeight : 0f;
        float effectiveLegendaryWeight = legendaryItems.Count > 0 ? legendaryWeight : 0f;

        float totalWeight =
            effectiveCommonWeight +
            effectiveUncommonWeight +
            effectiveRareWeight +
            effectiveLegendaryWeight;

        if (totalWeight > 0f)
        {
            float roll = Random.Range(0f, totalWeight);

            if (roll < effectiveCommonWeight)
                return commonItems[Random.Range(0, commonItems.Count)];

            roll -= effectiveCommonWeight;
            if (roll < effectiveUncommonWeight)
                return uncommonItems[Random.Range(0, uncommonItems.Count)];

            roll -= effectiveUncommonWeight;
            if (roll < effectiveRareWeight)
                return rareItems[Random.Range(0, rareItems.Count)];

            if (effectiveLegendaryWeight > 0f)
                return legendaryItems[Random.Range(0, legendaryItems.Count)];
        }

        // Fallback to any item
        var allItems = itemEntries.Values.ToList();
        return allItems.Count > 0 ? allItems[Random.Range(0, allItems.Count)] : null;
    }

    public ItemData GetRandomItem()
    {
        ItemEntry entry = GetRandomItemEntry();
        return entry != null ? entry.data : null;
    }

    public ItemPickup GetRandomPickupPrefab()
    {
        ItemEntry entry = GetRandomItemEntry();
        return entry != null ? entry.pickupPrefab : null;
    }

    public ItemData GetItem(string itemName)
    {
        return itemDatabase.ContainsKey(itemName) ? itemDatabase[itemName] : null;
    }

    public ItemEntry GetItemEntry(string itemName)
    {
        return itemEntries.ContainsKey(itemName) ? itemEntries[itemName] : null;
    }

    public List<ItemData> GetItemsByRarity(ItemRarity rarity)
    {
        List<ItemEntry> source = rarity switch
        {
            ItemRarity.COMMON => commonItems,
            ItemRarity.UNCOMMON => uncommonItems,
            ItemRarity.RARE => rareItems,
            ItemRarity.LEGENDARY => legendaryItems,
            _ => null
        };

        if (source == null)
            return new List<ItemData>();

        return source
            .Where(entry => entry != null && entry.data != null)
            .Select(entry => entry.data)
            .ToList();
    }

    public List<ItemEntry> GetItemEntriesByRarity(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.COMMON => new List<ItemEntry>(commonItems),
            ItemRarity.UNCOMMON => new List<ItemEntry>(uncommonItems),
            ItemRarity.RARE => new List<ItemEntry>(rareItems),
            ItemRarity.LEGENDARY => new List<ItemEntry>(legendaryItems),
            _ => new List<ItemEntry>()
        };
    }

    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        return itemDatabase.Values
            .Where(item => (item.Category & category) != 0)
            .ToList();
    }

    public List<ItemEntry> GetItemEntriesByCategory(ItemCategory category)
    {
        return itemEntries.Values
            .Where(entry => entry != null && entry.data != null && (entry.data.Category & category) != 0)
            .ToList();
    }

    public Dictionary<string, ItemData> GetAllItems()
    {
        return new Dictionary<string, ItemData>(itemDatabase);
    }

    public Dictionary<string, ItemEntry> GetAllItemEntries()
    {
        return new Dictionary<string, ItemEntry>(itemEntries);
    }
}