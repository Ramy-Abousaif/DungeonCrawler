using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.VFX;

public class ExplosionEffect : MonoBehaviour
{
    private CameraShake shake;
    
    [SerializeField]private VisualEffect sparksEffect;

    private void Awake()
    {
        shake = Camera.main.GetComponent<CameraShake>();
        StartAOEEffect();
    }

    private void StartAOEEffect()
    {
        sparksEffect.Play();
        shake.ShakeCamera(2f, 1f, 0.3f);
    }
}
