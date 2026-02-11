using UnityEngine;
using UnityEngine.AI;

public class SpiderLocomotionIK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bodyPivot;
    [SerializeField] private Transform BRFoot;
    [SerializeField] private Transform BLFoot;
    [SerializeField] private Transform FRFoot;
    [SerializeField] private Transform FLFoot;

    [Header("Body Settings")]
    [SerializeField] private float bodyHeightOffset = 1.4f;
    [SerializeField] private float bodyPositionSpring = 25f;
    [SerializeField] private float bodyRotationSpring = 20f;

    private float maxSink = 0.3f; // how far below navmesh visuals can go
    private float maxLift = 0.5f; // how far above navmesh visuals can go


    // Second-order dynamics state
    private Vector3 bodyPosVelocity;
    private Vector3 bodyUpVelocity;
    private Vector3 smoothedUp = Vector3.up;


    private void Update()
    {
        UpdateBodyPosition();
        UpdateBodyRotation();
    }

    // ---------------- BODY POSITION ----------------

    private void UpdateBodyPosition()
    {
        float avgFootHeight =
            (BRFoot.position.y +
            BLFoot.position.y +
            FRFoot.position.y +
            FLFoot.position.y) * 0.25f;

        if (!TryGetNavmeshHeight(out float navY))
            navY = transform.position.y;

        float targetWorldY = Mathf.Clamp(
            avgFootHeight,
            navY - maxSink,
            navY + maxLift
        );

        Vector3 worldTarget = new Vector3(
            bodyPivot.position.x,
            targetWorldY,
            bodyPivot.position.z
        );

        Vector3 localTarget = bodyPivot.parent.InverseTransformPoint(worldTarget) + bodyHeightOffset * Vector3.up;

        bodyPivot.localPosition = Vector3.SmoothDamp(
            bodyPivot.localPosition,
            localTarget,
            ref bodyPosVelocity,
            1f / bodyPositionSpring,
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    // ---------------- BODY ROTATION ----------------

    private void UpdateBodyRotation()
    {
        Vector3 v1 = FRFoot.position - BLFoot.position;
        Vector3 v2 = BRFoot.position - FLFoot.position;

        Vector3 groundNormal = Vector3.Cross(v1, v2).normalized;
        if (groundNormal.sqrMagnitude < 0.001f)
            groundNormal = Vector3.up;

        smoothedUp = Vector3.SmoothDamp(
            smoothedUp,
            groundNormal,
            ref bodyUpVelocity,
            1f / bodyRotationSpring,
            Mathf.Infinity,
            Time.deltaTime
        );

        Quaternion targetRotation =
            Quaternion.LookRotation(-transform.forward, smoothedUp);

        bodyPivot.rotation = Quaternion.Slerp(
            bodyPivot.rotation,
            targetRotation,
            Time.deltaTime * bodyRotationSpring
        );

        Debug.DrawRay(bodyPivot.position, smoothedUp, Color.red);
    }

    bool TryGetNavmeshHeight(out float height)
    {
        if (NavMesh.SamplePosition(transform.position, out var hit, 1.0f, NavMesh.AllAreas))
        {
            height = hit.position.y;
            return true;
        }

        height = transform.position.y;
        return false;
    }
}