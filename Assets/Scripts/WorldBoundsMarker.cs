using UnityEngine;

public class WorldBoundsMarker : MonoBehaviour
{
    private BoxCollider col;

    void Awake()
    {
        col = GetComponent<BoxCollider>();
        if (col == null)
            Debug.LogError("WorldBoundsMarker requires a BoxCollider");
    }

    public Bounds GetBounds()
    {
        return col.bounds;
    }
}