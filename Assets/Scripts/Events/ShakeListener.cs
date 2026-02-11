using UnityEngine;

public class ShakeListener : MonoBehaviour
{
    public GameObject cinemachineFreeLook;

    public void ShakeCamera()
    {
        Camera.main.GetComponent<CameraShake>().ShakeCamera(1, 1, 0.2f);
    }
}
