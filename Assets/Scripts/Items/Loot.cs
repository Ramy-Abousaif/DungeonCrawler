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
        
        yield return StartCoroutine(PickupMotionUtility.AnimateBezierLaunch(lootItem, CreateLaunchSettings()));
        
        Destroy(this.gameObject);
    }

    PickupBezierLaunchSettings CreateLaunchSettings()
    {
        return new PickupBezierLaunchSettings
        {
            arcHeight = arcHeight,
            launchDistance = launchDistance,
            launchDuration = launchDuration,
            randomSpread = randomSpread,
            groundLayer = groundLayer,
            startScaleMultiplier = startScaleMultiplier,
            groundOffset = groundOffset
        };
    }
}
