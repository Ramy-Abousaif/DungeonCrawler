using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class BodyLookAt : MonoBehaviour
{
    private Rig rig;
    private float targetWeight = 1f;
    [SerializeField] private Transform target;
    [SerializeField] private Camera lookCamera;
    [SerializeField] private float aimDistance = 10f;
    [SerializeField] private bool useRaycast = false;
    [SerializeField] private LayerMask raycastMask = ~0;
    [Tooltip("How often (seconds) the target selection updates. Lower = more frequent.")]
    [SerializeField] private float updateInterval = 0.05f;

    private bool createdRuntimeTarget;

    void Start()
    {
        rig = GetComponent<Rig>();
        if (lookCamera == null) lookCamera = Camera.main;

        if (target == null)
        {
            GameObject go = new GameObject("BodyLookTarget");
            go.transform.SetParent(null);
            target = go.transform;
            createdRuntimeTarget = true;
        }

        StartCoroutine(UpdateTargetLoop());
    }

    void OnDestroy()
    {
        if (createdRuntimeTarget && target != null)
            Destroy(target.gameObject);
    }

    void Update()
    {
        rig.weight = Mathf.Lerp(rig.weight, targetWeight, Time.deltaTime * 5f);
    }

    private IEnumerator UpdateTargetLoop()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (true)
        {
            UpdateTargetOnce();
            yield return wait;
        }
    }

    private void UpdateTargetOnce()
    {
        if (lookCamera == null) return;

        if (useRaycast)
        {
            Ray ray = new Ray(lookCamera.transform.position, lookCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, raycastMask.value))
            {
                target.position = hit.point;
            }
            else
            {
                target.position = lookCamera.transform.position + lookCamera.transform.forward * aimDistance;
            }
        }
        else
        {
            target.position = lookCamera.transform.position + lookCamera.transform.forward * aimDistance;
        }

        targetWeight = 1f;
    }
}
