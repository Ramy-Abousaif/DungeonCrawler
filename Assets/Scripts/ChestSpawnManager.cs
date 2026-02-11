using UnityEngine;

public class ChestSpawnManager : MonoBehaviour
{
    [Header("Chest")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private int chestCount = 20;

    [Header("Placement")]
    [SerializeField] private float maxSlope = 30f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private int maxAttemptsPerChest = 30;
    [SerializeField] private float edgePadding = 5f;

    private Bounds _worldBounds;
    private Vector3 chestHalfExtents;

    void Start()
    {
        if (WorldBoundsProvider.TryGetBounds(out Bounds worldBounds))
        {
            _worldBounds = worldBounds;            
        }
        else
        {
            Debug.LogError("Failed to get world bounds for chest spawning!");
            return;
        }

        // Get chest size automatically
        Renderer r = chestPrefab.GetComponent<Renderer>();
        chestHalfExtents = r.bounds.extents;

        SpawnChests();
    }

    void SpawnChests()
    {
        for (int i = 0; i < chestCount; i++)
        {
            for (int attempt = 0; attempt < maxAttemptsPerChest; attempt++)
            {
                if (TryFindChestPosition(out Vector3 pos, out Quaternion rot))
                {
                    Instantiate(chestPrefab, pos, rot, transform);
                    break;
                }
            }
        }
    }

    bool TryFindChestPosition(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        float x = Random.Range(_worldBounds.min.x + edgePadding, _worldBounds.max.x - edgePadding);
        float z = Random.Range(_worldBounds.min.z + edgePadding, _worldBounds.max.z - edgePadding);

        Vector3 rayStart = new Vector3(x, _worldBounds.max.y + 10f, z);

        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 500f, groundMask))
            return false;
    

        // slope check
        float slope = Vector3.Angle(hit.normal, Vector3.up);
        if (slope > maxSlope)
            return false;

        // overlap check to prevent clipping
        Vector3 checkPos = hit.point + Vector3.up * chestHalfExtents.y;
        if (Physics.OverlapBox(
            checkPos,
            chestHalfExtents,
            Quaternion.identity,
            blockingMask
        ).Length > 0)
            return false;

        position = checkPos;
        rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        return true;
    }
}
