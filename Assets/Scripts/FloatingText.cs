using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private Transform cam;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public Vector3 randomizeIntensity = new Vector3(0.5f, 0.5f, 0.5f);

    void Start()
    {
        cam = Camera.main.transform;
    }

    void OnEnable()
    {
        transform.localPosition += offset;
        transform.localPosition += new Vector3(
            Random.Range(-randomizeIntensity.x, randomizeIntensity.x),
            Random.Range(-randomizeIntensity.y, randomizeIntensity.y),
            Random.Range(-randomizeIntensity.z, randomizeIntensity.z)
        );
    }

    void Update()
    {
        transform.LookAt(transform.position + cam.forward);
    }
}
