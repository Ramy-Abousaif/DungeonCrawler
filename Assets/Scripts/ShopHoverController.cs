using UnityEngine;
using UnityEngine.InputSystem;

public class ShopHoverController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera shopCamera;
    [SerializeField] private PhysicsBasedCharacterController player;

    [Header("Raycast")]
    [SerializeField] private LayerMask cardLayerMask = ~0;
    [SerializeField] private float maxDistance = 500f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private ShopItem hoveredItem;

    private void Start()
    {
        if (shopCamera == null)
            shopCamera = Camera.main;

        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.Player;
    }

    private void Update()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.Player;

        bool inShop = player != null && player.inShop;
        if (!inShop)
        {
            SetHoveredItem(null);
            return;
        }

        if (shopCamera == null)
            shopCamera = Camera.main;

        if (shopCamera == null || Mouse.current == null)
        {
            SetHoveredItem(null);
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = shopCamera.ScreenPointToRay(mousePos);

        ShopItem newHoveredItem = null;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, cardLayerMask, triggerInteraction) && hit.collider != null)
        {
            newHoveredItem = hit.collider.GetComponentInParent<ShopItem>();
            if (newHoveredItem != null && !newHoveredItem.isActiveAndEnabled)
                newHoveredItem = null;
        }

        SetHoveredItem(newHoveredItem);

        if (hoveredItem != null && Mouse.current.leftButton.wasPressedThisFrame)
            hoveredItem.TryPurchaseItem();
    }

    private void OnDisable()
    {
        SetHoveredItem(null);
    }

    private void SetHoveredItem(ShopItem newHoveredItem)
    {
        if (hoveredItem == newHoveredItem)
            return;

        if (hoveredItem != null)
            hoveredItem.SetHoveredState(false);

        hoveredItem = newHoveredItem;

        if (hoveredItem != null)
            hoveredItem.SetHoveredState(true);
    }
}
