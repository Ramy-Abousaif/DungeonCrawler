using System.Collections;
using UnityEngine;

public class Loot : Interactable
{
    private bool isOpened = false;
    public GameObject placeholderEffect;
    
    [Header("Launch Settings")]
    [Tooltip("Height of the arc peak")]
    public float arcHeight = 3f;
    [Tooltip("Distance the loot travels horizontally")]
    public float launchDistance = 3f;
    [Tooltip("Duration of the launch animation in seconds")]
    public float launchDuration = 0.8f;
    [Tooltip("Random spread angle in degrees")]
    public float randomSpread = 30f;
    [Tooltip("Height offset from where the loot spawns")]
    public float spawnHeight = 0.5f;
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = -1;
    [Tooltip("Initial scale multiplier when loot launches (0-1)")]
    public float startScaleMultiplier = 0.2f;
    [Tooltip("Offset above ground to prevent clipping")]
    public float groundOffset = 0.1f;

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
        yield return new WaitForSeconds(0.5f);
        
        ItemPickup randomItemPrefab = ItemManager.Instance.GetRandomPickupPrefab();
        
        if (randomItemPrefab == null)
        {
            Debug.LogError("No items available in ItemManager!");
            Destroy(gameObject);
            yield break;
        }
        
        GameObject lootItem = Instantiate(
            randomItemPrefab.gameObject,
            transform.position + Vector3.up * spawnHeight,
            Quaternion.identity
        );
        
        yield return StartCoroutine(AnimateLootBezier(lootItem));
        
        Destroy(this.gameObject);
    }

    IEnumerator AnimateLootBezier(GameObject lootItem)
    {
        Vector3 originalScale = lootItem.transform.localScale;
        Vector3 startScale = originalScale * startScaleMultiplier;
        lootItem.transform.localScale = startScale;
        
        Collider[] colliders = lootItem.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        float randomAngle = Random.Range(0f, 360f);
        Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        
        direction = Quaternion.Euler(
            Random.Range(-randomSpread, randomSpread), 
            Random.Range(-randomSpread, randomSpread), 
            0
        ) * direction;
        direction.Normalize();
        
        Vector3 startPoint = lootItem.transform.position;
        Vector3 endPoint = startPoint + direction * launchDistance;
        
        if (Physics.Raycast(endPoint + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            endPoint = hit.point + hit.normal * groundOffset;
        else
            endPoint.y = groundOffset;

        
        Vector3 controlPoint = (startPoint + endPoint) / 2f + Vector3.up * arcHeight;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < launchDuration && lootItem != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / launchDuration;
            
            Vector3 position = Mathf.Pow(1 - t, 2) * startPoint +
                             2 * (1 - t) * t * controlPoint +
                             Mathf.Pow(t, 2) * endPoint;
            
            lootItem.transform.position = position;
            lootItem.transform.localScale = Vector3.Lerp(startScale, originalScale, t);
            lootItem.transform.Rotate(Random.insideUnitSphere * 200f * Time.deltaTime, Space.World);
            
            yield return null;
        }
        
        if (lootItem != null)
        {
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
            
            lootItem.transform.position = endPoint;
            lootItem.transform.localScale = originalScale;
        }
    }
}
