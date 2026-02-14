using System.Collections.Generic;
using UnityEngine;

public class OcclusionFader : MonoBehaviour
{
    [Header("Fade Settings")]
    public Transform target;
    public float fadeSpeed = 5f;
    public float transparentAlpha = 0.2f;
    public float thickness = 0.5f;
    public LayerMask obstacleMask;

    // Internal tracking
    private Dictionary<Renderer, float> _fadeTargets = new();
    private Dictionary<Renderer, RoomVisualController> _rendererToController = new();

    void Update()
    {
        if (target == null)
            return;

        // Step 1: Cast a sphere to detect blocking walls
        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        Ray ray = new Ray(transform.position, dir.normalized);
        RaycastHit[] hits = Physics.SphereCastAll(ray, thickness, dist, obstacleMask);

        HashSet<Renderer> currentlyBlocking = new();

        foreach (var hit in hits)
        {
            Renderer r = hit.collider.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            currentlyBlocking.Add(r);

            // Get the RoomVisualController from this renderer
            if (!_rendererToController.ContainsKey(r))
            {
                RoomVisualController controller = r.GetComponentInParent<RoomVisualController>();
                if (controller != null)
                    _rendererToController[r] = controller;
            }

            // Only set fade target if we have a controller
            if (_rendererToController.TryGetValue(r, out var roomController))
            {
                _fadeTargets[r] = transparentAlpha;
            }
        }

        // Step 2: Restore non-blocking walls to full alpha
        foreach (var kvp in _rendererToController)
        {
            Renderer r = kvp.Key;
            if (!currentlyBlocking.Contains(r))
            {
                _fadeTargets[r] = 1f;
            }
        }

        // Step 3: Smoothly fade all tracked renderers
        foreach (var kvp in _fadeTargets)
        {
            Renderer r = kvp.Key;
            float targetAlpha = kvp.Value;

            if (_rendererToController.TryGetValue(r, out var controller))
            {
                // Smoothly interpolate alpha
                Color currentColor = controller.GetRendererColor(r); // We'll add this method in RoomVisualController
                float newAlpha = Mathf.MoveTowards(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);

                // Apply the new alpha via property block
                controller.FadeRenderer(r, newAlpha);
            }
        }
    }
}