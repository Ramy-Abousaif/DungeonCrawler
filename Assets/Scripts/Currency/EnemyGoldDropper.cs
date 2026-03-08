using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyGoldDropper : MonoBehaviour
{
    [Header("Drop Setup")]
    [SerializeField] private GoldPickup goldPickupPrefab;
    [SerializeField] [Min(0)] private int minDropCount = 2;
    [SerializeField] [Min(0)] private int maxDropCount = 6;
    [SerializeField] [Min(1)] private int goldPerPickup = 1;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnValidate()
    {
        minDropCount = Mathf.Max(0, minDropCount);
        maxDropCount = Mathf.Max(minDropCount, maxDropCount);
        goldPerPickup = Mathf.Max(1, goldPerPickup);
    }

    private void OnEnable()
    {
        if (enemy == null)
            enemy = GetComponent<Enemy>();

        if (enemy != null)
            enemy.OnDeath += SpawnGold;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.OnDeath -= SpawnGold;
    }

    private void SpawnGold()
    {
        if (goldPickupPrefab == null)
        {
            Debug.LogWarning("EnemyGoldDropper has no GoldPickup prefab assigned.", gameObject);
            return;
        }

        int dropCount = Random.Range(minDropCount, maxDropCount + 1);
        for (int i = 0; i < dropCount; i++)
        {
            GoldPickup pickup = Instantiate(goldPickupPrefab, transform.position, Quaternion.identity);
            pickup.Initialize(goldPerPickup, transform.position);
        }
    }
}
