using UnityEngine;

public class TestHitbox : MonoBehaviour
{
    private enum TestTriggerBox
    {
        Damage,
        Heal,
        ITEM
    }

    [SerializeField] private float delay = 3f;
    [SerializeField] private TestTriggerBox type;
    [SerializeField] private ItemData itemToGive;
    private float timer = 0f;

    private void OnTriggerStay(Collider other)
    {
        timer += Time.deltaTime;
        if(timer <= delay)
            return;
        
        switch(type)
        {
            case TestTriggerBox.Damage:
            {
                if(other.CompareTag("Enemy"))
                {
                    Enemy enemy = other.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(10, true);
                    }
                }
                if(other.CompareTag("Player"))
                {
                    PhysicsBasedCharacterController player = other.GetComponent<PhysicsBasedCharacterController>();
                    if(player != null)
                    {
                        player.TakeDamage(10, true);
                    }
                }
                break;
            }
            case TestTriggerBox.Heal:
            {
                if(other.CompareTag("Enemy"))
                {
                    Enemy enemy = other.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Heal(10);
                    }
                }
                if(other.CompareTag("Player"))
                {
                    PhysicsBasedCharacterController player = other.GetComponent<PhysicsBasedCharacterController>();
                    if(player != null)
                    {
                        player.Heal(10);
                    }
                }
                break;
            }
            case TestTriggerBox.ITEM:
            {
                if(other.CompareTag("Player"))
                {
                    PhysicsBasedCharacterController player = other.GetComponent<PhysicsBasedCharacterController>();
                    if(player != null && itemToGive != null)
                    {
                        // Add item to player
                        foreach (ItemList itemSlot in player.items)
                        {
                            if (itemSlot.GetName() == itemToGive.ItemName)
                            {
                                // Stack item
                                if (itemToGive.CanStack)
                                {
                                    itemSlot.stacks = Mathf.Min(itemSlot.stacks + 1, itemToGive.MaxStacks);
                                    itemSlot.item.ApplyPickupEffects(player);
                                    player.ShowCustomText(itemToGive.ItemName, Color.white);
                                    timer = 0f;
                                    return;
                                }
                                else
                                {
                                    timer = 0f;
                                    return;
                                }
                            }
                        }

                        // Add as new item
                        ItemList newItemSlot = new ItemList(itemToGive, 1);
                        player.items.Add(newItemSlot);
                        newItemSlot.item.ApplyPickupEffects(player);
                        player.ShowCustomText(itemToGive.ItemName, Color.white);
                    }
                }
                break;
            }
        }
        timer = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        timer = delay;
    }
}
