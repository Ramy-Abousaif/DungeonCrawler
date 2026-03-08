using System.Collections;
using UnityEngine;

[System.Serializable]
public class PickupBezierLaunchSettings
{
    [Tooltip("Height of the bezier arc peak")]
    public float arcHeight = 3f;

    [Tooltip("Distance the pickup travels horizontally")]
    public float launchDistance = 3f;

    [Tooltip("Duration of the launch animation in seconds")]
    public float launchDuration = 0.8f;

    [Tooltip("Random spread angle in degrees")]
    public float randomSpread = 30f;

    [Range(0f, 1f)]
    [Tooltip("Initial scale multiplier when launch starts")]
    public float startScaleMultiplier = 0.2f;

    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = ~0;

    [Tooltip("Offset above ground to prevent clipping")]
    public float groundOffset = 0.1f;

    [Tooltip("Raycast start height used for landing detection")]
    public float groundProbeHeight = 10f;

    [Tooltip("Raycast distance used for landing detection")]
    public float groundProbeDistance = 100f;

    [Tooltip("Spin speed applied while flying")]
    public float spinSpeed = 200f;
}

public static class PickupMotionUtility
{
    public static IEnumerator AnimateBezierLaunch(GameObject pickupObject, PickupBezierLaunchSettings settings)
    {
        if (pickupObject == null)
            yield break;

        settings ??= new PickupBezierLaunchSettings();

        Transform pickupTransform = pickupObject.transform;
        Vector3 originalScale = pickupTransform.localScale;
        Vector3 startScale = originalScale * Mathf.Clamp01(settings.startScaleMultiplier);
        pickupTransform.localScale = startScale;

        Collider[] colliders = pickupObject.GetComponentsInChildren<Collider>(true);
        bool[] colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null)
                continue;

            colliderStates[i] = col.enabled;
            col.enabled = false;
        }

        Vector3 startPoint = pickupTransform.position;
        Vector3 direction = BuildRandomDirection(settings.randomSpread);
        Vector3 endPoint = ResolveGroundedEndPoint(startPoint, direction, settings);
        Vector3 controlPoint = (startPoint + endPoint) * 0.5f + Vector3.up * settings.arcHeight;

        float duration = Mathf.Max(0.01f, settings.launchDuration);
        float elapsedTime = 0f;
        Vector3 spinAxis = Random.onUnitSphere;
        if (spinAxis.sqrMagnitude <= 0.0001f)
            spinAxis = Vector3.up;

        while (elapsedTime < duration && pickupObject != null)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            pickupTransform.position = EvaluateQuadraticBezier(startPoint, controlPoint, endPoint, t);
            pickupTransform.localScale = Vector3.Lerp(startScale, originalScale, t);
            pickupTransform.Rotate(spinAxis, settings.spinSpeed * Time.deltaTime, Space.World);

            yield return null;
        }

        if (pickupObject != null)
        {
            pickupTransform.position = endPoint;
            pickupTransform.localScale = originalScale;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;

                col.enabled = colliderStates[i];
            }
        }
    }

    public static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float clampedT = Mathf.Clamp01(t);
        float oneMinusT = 1f - clampedT;

        return oneMinusT * oneMinusT * start
             + 2f * oneMinusT * clampedT * control
             + clampedT * clampedT * end;
    }

    public static Vector3 BuildRandomDirection(float spreadDegrees)
    {
        float spread = Mathf.Abs(spreadDegrees);
        float randomYaw = Random.Range(0f, 360f);

        Vector3 direction = Quaternion.Euler(0f, randomYaw, 0f) * Vector3.forward;
        direction = Quaternion.Euler(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f) * direction;

        if (direction.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }

    public static Vector3 ResolveGroundedEndPoint(Vector3 startPoint, Vector3 direction, PickupBezierLaunchSettings settings)
    {
        settings ??= new PickupBezierLaunchSettings();

        Vector3 normalizedDirection = direction.sqrMagnitude <= 0.0001f
            ? Vector3.forward
            : direction.normalized;

        Vector3 endPoint = startPoint + normalizedDirection * settings.launchDistance;
        Vector3 rayStart = endPoint + Vector3.up * settings.groundProbeHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, settings.groundProbeDistance, settings.groundLayer))
            return hit.point + hit.normal * settings.groundOffset;

        endPoint.y = settings.groundOffset;
        return endPoint;
    }
}
