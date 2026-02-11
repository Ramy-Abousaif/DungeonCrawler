using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public CinemachineCamera cam;
    private float shakeTime;

    void Update()
    {
        if(shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            if (shakeTime <= 0f)
            {
                var shake = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
                shake.AmplitudeGain = 0f;
            }
        }
    }

    public void ShakeCamera(float intensity, float frequency, float duration)
    {
        var shake = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        shakeTime = duration;
        shake.AmplitudeGain = intensity;
        shake.FrequencyGain = frequency;
    }
}
