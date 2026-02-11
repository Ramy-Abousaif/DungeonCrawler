using UnityEngine;

[System.Serializable]
public class ItemList
{
    [SerializeReference]
    public Item item;
    public string name;
    public int stacks;

    public ItemList(Item newItem, string newName, int newStacks)
    {
        item = newItem;
        name = newName;
        stacks = newStacks;
    }
}