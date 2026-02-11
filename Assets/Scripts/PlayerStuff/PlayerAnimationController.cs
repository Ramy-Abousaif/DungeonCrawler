using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator anim;
    public bool isAllowedToUseFootIK = true;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask layer = default;
    [SerializeField] private TwoBoneIKConstraint leftFootIK;
    [SerializeField] private TwoBoneIKConstraint rightFootIK;
    [SerializeField] private Transform leftFootIKTarget;
    [SerializeField] private Transform rightFootIKTarget;
    [SerializeField] private Transform leftFootRotationIKConstraintTarget;
    [SerializeField] private Transform rightFootRotationIKConstraintTarget;
    [SerializeField] private MultiPositionConstraint pelvisConstraint;
    [SerializeField] private MultiRotationConstraint leftFootConstraintTarget;
    [SerializeField] private MultiRotationConstraint rightFootConstraintTarget;
    [SerializeField] private Vector3 leftFootRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 rightFootRotationOffset = Vector3.zero;
    [SerializeField, Range(0f, 1.0f)] private float distanceToGround = 0.5f;
    [SerializeField] private Transform pelvisIKTarget;
    [SerializeField] private float baseHipsPositionY = 0f;
    private float hipsCurrentY = 0f;
    private float hipsTargetY = 0f;
    private float lowestFootY = 0f;
    private Vector3 currentHipsPosition = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void SetIsAllowedToUseFootIK(bool val)
    {
        isAllowedToUseFootIK = val;
        if(!val)
        {
            leftFootIK.weight = 0f;
            rightFootIK.weight = 0f;
            pelvisConstraint.weight = 0f;
        }
        else
        {
            leftFootIK.weight = anim.GetFloat("IK_LeftFootWeight");
            rightFootIK.weight = anim.GetFloat("IK_RightFootWeight");
        }
    }

    private void LateUpdate()
    {
        UpdateFootIK();
    }

    private void UpdateFootIK()
    {
        Ray rayLeft = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        bool isRayCastHitLeftFoot = Physics.Raycast(rayLeft, out RaycastHit leftFootHit, distanceToGround + 2, layer);

        Ray rayRight = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
        bool isRayCastHitRightFoot = Physics.Raycast(rayRight, out RaycastHit rightFootHit, distanceToGround + 2, layer);

        if(isAllowedToUseFootIK)
        {
            SetWeightOfConstraint(leftFootHit, rightFootHit);
            FootIK1(isRayCastHitLeftFoot, isRayCastHitRightFoot, leftFootHit, rightFootHit);
            HipsIK1(isRayCastHitLeftFoot, isRayCastHitRightFoot, leftFootHit, rightFootHit);
        }
    }

    private float smoothedHipsWeight = 0f;
    private float smoothedLeftFootWeight = 0f;
    private float smoothedRightFootWeight = 0f;

    public void SetWeightOfConstraint(RaycastHit leftFootHit, RaycastHit rightFootHit)
    {
        float leftSlope = Vector3.Angle(Vector3.up, leftFootHit.normal);
        float rightSlope = Vector3.Angle(Vector3.up, rightFootHit.normal);
        float averageSlopeAngle = (leftSlope + rightSlope) * 0.5f;
        float slopeNormalizedValue = averageSlopeAngle / 90;

        float targetWeight = Mathf.Clamp01(Mathf.Abs(leftFootHit.point.y - rightFootHit.point.y) / 0.3f); // 0.3f is max height difference

        if(targetWeight < 0.01f) targetWeight = 0f;

        smoothedHipsWeight = Mathf.Lerp(smoothedHipsWeight, targetWeight, Time.deltaTime * 10f);

        float currentLeftFootY = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
        float currentRightFootY = anim.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
        float leftFootWeight = Mathf.Clamp01(anim.GetFloat("IK_LeftFootWeight") + slopeNormalizedValue);
        float rightFootWeight = Mathf.Clamp01(anim.GetFloat("IK_RightFootWeight") + slopeNormalizedValue);

        if(currentLeftFootY < leftFootHit.point.y)
        {
            Debug.Log("ClampL");
            smoothedLeftFootWeight = Mathf.Lerp(smoothedLeftFootWeight, 1, Time.deltaTime * 20f);
        }
        else
        {
            smoothedLeftFootWeight = Mathf.Lerp(smoothedLeftFootWeight, leftFootWeight, Time.deltaTime * 20f);
        }

        if(currentRightFootY < rightFootHit.point.y)
        {
            Debug.Log("ClampR");
            smoothedRightFootWeight = Mathf.Lerp(smoothedRightFootWeight, 1, Time.deltaTime * 20f);
        }
        else
        {
            smoothedRightFootWeight = Mathf.Lerp(smoothedRightFootWeight, rightFootWeight, Time.deltaTime * 20f);
        }

        pelvisConstraint.weight = Mathf.Clamp(smoothedHipsWeight, 0, 0.95f);
        leftFootIK.weight = Mathf.Clamp(smoothedLeftFootWeight, 0, 0.95f);
        rightFootIK.weight = Mathf.Clamp(smoothedRightFootWeight, 0, 0.95f);
        
        leftFootConstraintTarget.weight = Mathf.Clamp(smoothedLeftFootWeight, 0, 0.95f);
        rightFootConstraintTarget.weight = Mathf.Clamp(smoothedRightFootWeight, 0, 0.95f);
    }


    private void FootIK1(bool isRayCastHitLeftFoot, bool isRayCastHitRightFoot, RaycastHit leftFootHit, RaycastHit rightFootHit)
    {
        if(!anim) return;
        if(isRayCastHitLeftFoot)
        {
            float currentLeftFootY = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
            Vector3 footPosition = leftFootHit.point;
            if(currentLeftFootY < leftFootHit.point.y)
            {
                footPosition.y += distanceToGround + 0.1f;
            }
            else
            {
                footPosition.y += distanceToGround;
            }
            leftFootIKTarget.position = footPosition;

            Quaternion footRotationOffset = Quaternion.Euler(leftFootRotationOffset);
            Vector3 footForward = Vector3.ProjectOnPlane(player.forward, leftFootHit.normal).normalized;

            Quaternion footRotation = Quaternion.LookRotation(footForward, leftFootHit.normal) * footRotationOffset;

            leftFootRotationIKConstraintTarget.transform.rotation = Quaternion.Slerp(leftFootRotationIKConstraintTarget.transform.rotation, footRotation, Time.deltaTime * 10f);
        }
        if(isRayCastHitRightFoot)
        {
            float currentRightFootY = anim.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
            Vector3 footPosition = rightFootHit.point;
            if(currentRightFootY < rightFootHit.point.y)
            {
                footPosition.y += distanceToGround + 0.1f;
            }
            else
            {
                footPosition.y += distanceToGround;
            }
            rightFootIKTarget.position = footPosition;

            Quaternion footRotationOffset = Quaternion.Euler(rightFootRotationOffset);
            Vector3 footForward = Vector3.ProjectOnPlane(player.forward, rightFootHit.normal).normalized;

            Quaternion footRotation = Quaternion.LookRotation(footForward, rightFootHit.normal) * footRotationOffset;

            rightFootRotationIKConstraintTarget.transform.rotation = Quaternion.Slerp(rightFootRotationIKConstraintTarget.transform.rotation, footRotation, Time.deltaTime * 10f);
        }
    }

    public bool affectHip = true;

    private void HipsIK1(bool isRayCastHitLeftFoot, bool isRayCastHitRightFoot, RaycastHit leftFootHit, RaycastHit rightFootHit)
    {
        if(!affectHip) return;

        if(anim)
        {
            if(isRayCastHitLeftFoot && isRayCastHitRightFoot)
            {
                float leftY = leftFootHit.point.y;
                float rightY = rightFootHit.point.y;

                lowestFootY = Mathf.Min(leftY, rightY);
                hipsTargetY = baseHipsPositionY + lowestFootY;

                if(Mathf.Abs(hipsTargetY - hipsCurrentY) > 0.01f)
                {
                    hipsCurrentY = Mathf.Lerp(hipsCurrentY, hipsTargetY, Time.deltaTime * 15f);
                }

                pelvisIKTarget.position = new Vector3(pelvisIKTarget.position.x, hipsCurrentY, pelvisIKTarget.position.z);

                currentHipsPosition = anim.bodyPosition;
            }
        }
    }
}
