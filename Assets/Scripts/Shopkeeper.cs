using UnityEngine;

public class Shopkeeper : Interactable
{
    private bool inShop = false;
    [SerializeField] private Transform view;
    [SerializeField] private Vector3 camRotOffset = new Vector3(42f, 180f, 0f);
    void Start()
    {
        
    }

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        if (!inShop)
        {
            CameraManager.Instance.shopCam.transform.position = view.position;
            CameraManager.Instance.shopCam.Follow = view;
            CameraManager.Instance.shopCam.transform.localEulerAngles = transform.parent.localEulerAngles + camRotOffset;
            CameraManager.Instance.SwitchState(() => 
            {
                UIManager.Instance.ToggleShopUI(true);
                inShop = true;
            });
        }
        else
        {
            CameraManager.Instance.SwitchState(() => 
            {
                UIManager.Instance.ToggleShopUI(false);
                inShop = false;
            });
        }
    }
}
