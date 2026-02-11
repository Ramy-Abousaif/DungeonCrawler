using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] LayerMask lm = default;
    [SerializeField] Transform body = default;
    [SerializeField] IKFootSolver otherFoot = default;
    [SerializeField] float speed = 1;
    [SerializeField] float stepDistance = 4;
    [SerializeField] float stepLength = 4;
    [SerializeField] float stepHeight = 1;
    [SerializeField] Vector3 footOffset = default;
    public float xSpacing;
    public float zSpacing;
    public GameObject impact;
    public GameObject impactDecal;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    float lerp;

    private void Start()
    {
        currentPosition = newPosition = oldPosition = transform.position;
        currentNormal = newNormal = oldNormal = transform.forward;
        lerp = 1;
    }

    // Update is called once per frame

    void Update()
    {
        transform.position = currentPosition;
        transform.forward = -currentNormal;

        Vector3 rayOrigin =
            body.position + Vector3.up * 0.5f +
            (body.right * xSpacing) +
            (body.forward * zSpacing);

        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit info, 10, lm.value))
        {
            // use horizontal distance (xz) so vertical differences don't prevent stepping
            float horizDist = Vector2.Distance(new Vector2(newPosition.x, newPosition.z), new Vector2(info.point.x, info.point.z));
            if (horizDist > stepDistance && !otherFoot.IsMoving() && lerp >= 1)
            {
                lerp = 0;
                int direction = body.InverseTransformPoint(info.point).z > body.InverseTransformPoint(newPosition).z ? 1 : -1;
                // stepLength should offset forward/back relative to the body, not up
                newPosition = info.point + (body.forward * stepLength * direction) + footOffset;
                newNormal = info.normal;
                if(impact != null)
                {
                    PoolManager.Instance.Spawn(impact, newPosition, Quaternion.Euler(newNormal));
                }
                if(impactDecal != null)
                {
                    Renderer rend = info.collider.GetComponent<Renderer>();
                    MeshCollider meshCollider = info.collider as MeshCollider;

                    if (rend != null && meshCollider != null)
                    {
                        Texture2D tex = rend.material.mainTexture as Texture2D;
                        Vector2 uv = info.textureCoord;

                        Color pixelColor = tex.GetPixelBilinear(uv.x, uv.y);
                        ParticleSystem.MainModule ps = impact.GetComponent<ParticleSystem>().main;
                        ps.startColor = pixelColor;
                    }
                    PoolManager.Instance.Spawn(impactDecal,newPosition + (newNormal.normalized * 0.1f), Quaternion.LookRotation(-newNormal));
                }
            }
        }

        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.5f);
    }

    public bool IsMoving()
    {
        return lerp < 1;
    }

}