using System.Collections;
using UnityEngine;

public class Loot : Interactable
{
    private bool isOpened = false;
    public GameObject placeholderEffect;
    public GameObject[] possibleLootItems;

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        if (isOpened) 
            return;
        
        StartCoroutine(LootSequence(player));
    }

    IEnumerator LootSequence(PhysicsBasedCharacterController player)
    {
        isOpened = true;
        PoolManager.Instance.Spawn(placeholderEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(1f);
        Instantiate(possibleLootItems[Random.Range(0, possibleLootItems.Length)], transform.position + Vector3.up * 0.5f, Quaternion.identity);
        Destroy(this.gameObject);
    }
}
