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
    [SerializeField] private Items itemPickupType;
    private ItemPickup itemPickup;
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
                    if(player != null)
                    {
                        if(itemPickup == null)
                        {
                            itemPickup = gameObject.AddComponent<ItemPickup>();                            
                        }
                        itemPickup.itemDrop = itemPickupType;
                        itemPickup.item = itemPickup.AssignItem(itemPickup.itemDrop);
                        itemPickup.AddItem(player);
                        player.ShowCustomText(itemPickup.item.GiveName(), Color.white);
                        foreach (ItemList il in player.items)
                        {
                            if (il.name == itemPickup.item.GiveName())
                            {
                                il.item.OnPickup(player, il.stacks);
                                break;
                            }
                        }
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
