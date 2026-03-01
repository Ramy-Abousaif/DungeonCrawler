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
    private Dictionary<WallSegment, float> _fadeTargets = new();
    private Dictionary<WallSegment, RoomVisualController> _segmentToController = new();

    void Update()
    {
        if (target == null)
            return;

        // Step 1: Cast a sphere to detect blocking walls
        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        Ray ray = new Ray(transform.position, dir.normalized);
        RaycastHit[] hits = Physics.SphereCastAll(ray, thickness, dist, obstacleMask);

        HashSet<WallSegment> currentlyBlocking = new();

        // Step 2: Detect all the wall segments that are being blocked
        foreach (var hit in hits)
        {
            WallSegment wallSegment = hit.collider.GetComponentInParent<WallSegment>();
            if (wallSegment != null)
            {
                currentlyBlocking.Add(wallSegment);

                // Ensure that we store the RoomVisualController for the wall segment
                if (!_segmentToController.ContainsKey(wallSegment))
                {
                    RoomVisualController controller = wallSegment.GetComponentInParent<RoomVisualController>();
                    if (controller != null)
                    {
                        _segmentToController[wallSegment] = controller;
                    }
                }

                // Set the fade target for the wall segment
                if (_segmentToController.TryGetValue(wallSegment, out var roomController))
                {
                    _fadeTargets[wallSegment] = transparentAlpha;
                }
            }
        }

        // Step 3: Restore non-blocking wall segments to full visibility (alpha = 1)
        foreach (var segment in _segmentToController.Keys)
        {
            if (!currentlyBlocking.Contains(segment))
            {
                _fadeTargets[segment] = 1f;
            }
        }

        // Step 4: Smoothly fade all tracked wall segments (from current alpha to target)
        foreach (var kvp in _fadeTargets)
        {
            WallSegment segment = kvp.Key;
            float targetAlpha = kvp.Value;

            // Get the current alpha of the wall segment from the RoomVisualController
            RoomVisualController controller = _segmentToController[segment];
            Color currentColor = controller.GetWallSegmentColor(segment);
            float currentAlpha = currentColor.a;

            // Smoothly interpolate the alpha value
            float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

            // Apply the new alpha value to the wall segment using the RoomVisualController
            controller.SetWallSegmentVisibility(segment, newAlpha);
        }
    }
}