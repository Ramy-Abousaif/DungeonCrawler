using UnityEngine;

public class DestroyObject : MonoBehaviour
{
    public float delay = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(this.gameObject, delay);
    }
}
