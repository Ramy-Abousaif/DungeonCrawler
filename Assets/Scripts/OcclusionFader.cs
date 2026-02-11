using System.Collections.Generic;
using UnityEngine;

public class OcclusionFader : MonoBehaviour
{
    public Transform target;
    public float fadeSpeed = 5f;
    public float transparentAlpha = 0.2f;
    public float thickness = 0.5f;
    public LayerMask obstacleMask;

    private Dictionary<Renderer, float> _fadeTargets = new();
    private Dictionary<Renderer, Material> _materials = new();

    void Update()
    {
        if (target == null)
            return;

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        Ray ray = new Ray(transform.position, dir.normalized);
        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            thickness,
            dist,
            obstacleMask
        );

        HashSet<Renderer> currentlyBlocking = new();

        foreach (var hit in hits)
        {
            Renderer r = hit.collider.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            currentlyBlocking.Add(r);

            if (!_materials.ContainsKey(r))
                _materials[r] = r.material;

            _fadeTargets[r] = transparentAlpha;
        }

        // Restore non-blocking
        foreach (var r in _materials.Keys)
        {
            if (!currentlyBlocking.Contains(r))
            {
                _fadeTargets[r] = 1f;
            }
        }

        // Smooth fade
        foreach (var kvp in _fadeTargets)
        {
            Renderer r = kvp.Key;
            float targetAlpha = kvp.Value;

            Material mat = _materials[r];

            Color c = mat.GetColor("_BaseColor");
            float newAlpha = Mathf.MoveTowards(
                c.a,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );

            c.a = newAlpha;
            mat.SetColor("_BaseColor", c);
        }
    }
}