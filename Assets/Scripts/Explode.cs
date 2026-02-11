using Unity.Mathematics;
using UnityEngine;

public class Explode : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject explosionFX;
    private float ticker = 0f;
    [SerializeField] private float timer = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ticker = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(transform.position + Vector3.down * 0.5f, Vector3.down, 2f, groundLayer))
        {
            ticker += Time.deltaTime;
            if(ticker >= timer)
            {
                PoolManager.Instance.Spawn(explosionFX, transform.position, Quaternion.identity);
                PoolManager.Instance.Despawn(gameObject);
            }
        }
    }
}
