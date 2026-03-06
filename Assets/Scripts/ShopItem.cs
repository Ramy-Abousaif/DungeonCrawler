using UnityEngine;
using ProPixelizer;
using UnityEngine.InputSystem;
using TMPro;

public class ShopItem : MonoBehaviour
{
    [SerializeField] private Shopkeeper shopkeeper;

    [Header("Item Data")]
    public ItemData itemData;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Hover Visuals")]
    [SerializeField] private Transform hoverVisual;
    [SerializeField] private float hoverLift = 0.2f;
    [SerializeField] private float maxTilt = 8f;
    [SerializeField] private float tiltScreenRange = 180f;
    [SerializeField] private float positionLerpSpeed = 12f;
    [SerializeField] private float rotationLerpSpeed = 12f;
    [SerializeField] private Vector3 faceRotationOffset;

    private Material mat;
    private bool isHighlighted = false;
    private Camera shopCamera;
    private PhysicsBasedCharacterController player;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Transform HoverVisual => hoverVisual != null ? hoverVisual : transform;

    private void Awake()
    {
        Transform visual = HoverVisual;
        baseLocalPosition = visual.localPosition;
        baseLocalRotation = visual.localRotation;
    }
    
    private void Start()
    {
        mat = GetComponent<Renderer>()?.material;
        
        if(itemData == null)
            itemData = ItemManager.Instance.GetRandomItem();
            
        shopCamera = Camera.main;
        player = GameManager.Instance.Player;

        if (titleText != null)
            titleText.text = itemData.ItemName;
        
        if (descriptionText != null)
            descriptionText.text = itemData.Description;
    }
    
    private void Update()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.Player;

        bool inShop = player != null && player.inShop;

        if (!inShop)
        {
            if (isHighlighted)
                SetHoveredState(false);

            UpdateHoverVisual(false);
            return;
        }

        UpdateHoverVisual(isHighlighted);
    }

    private void OnDisable()
    {
        if (isHighlighted)
            SetHoveredState(false);

        Transform visual = HoverVisual;
        visual.localPosition = baseLocalPosition;
        visual.localRotation = baseLocalRotation;
    }

    public void SetHoveredState(bool hovered)
    {
        if (isHighlighted == hovered)
            return;

        HighlightItem(hovered);

        if (hovered)
        {
            ShopTooltip.Instance?.Show(itemData);
        }
        else
        {
            ShopTooltip.Instance?.Hide();
        }
    }
    
    private void HighlightItem(bool highlight)
    {
        if (mat != null)
            mat.SetFloat("_Selected", highlight ? 1f : 0f);

        isHighlighted = highlight;
    }

    private void UpdateHoverVisual(bool hovered)
    {
        Transform visual = HoverVisual;

        Vector3 targetLocalPosition = baseLocalPosition;
        Quaternion targetLocalRotation = baseLocalRotation;

        if (hovered)
        {
            if (shopCamera == null)
                shopCamera = Camera.main;

            if (shopCamera != null)
            {
                targetLocalPosition += Vector3.up * hoverLift;

                Vector2 mousePos = Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : (Vector2)shopCamera.WorldToScreenPoint(visual.position);

                Vector3 cardScreenPos = shopCamera.WorldToScreenPoint(visual.position);
                Vector2 mouseDelta = mousePos - new Vector2(cardScreenPos.x, cardScreenPos.y);
                float normalizedX = Mathf.Clamp(mouseDelta.x / tiltScreenRange, -1f, 1f);
                float normalizedY = Mathf.Clamp(mouseDelta.y / tiltScreenRange, -1f, 1f);

                float tiltX = -normalizedY * maxTilt;
                float tiltY = normalizedX * maxTilt;

                Vector3 toCamera = shopCamera.transform.position - visual.position;
                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    Quaternion worldFaceRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up) * Quaternion.Euler(faceRotationOffset);
                    Quaternion parentRotation = visual.parent != null ? visual.parent.rotation : Quaternion.identity;

                    targetLocalRotation = Quaternion.Inverse(parentRotation) * worldFaceRotation;
                    targetLocalRotation *= Quaternion.Euler(tiltX, tiltY, 0f);
                }
            }
        }

        visual.localPosition = Vector3.Lerp(visual.localPosition, targetLocalPosition, Time.deltaTime * positionLerpSpeed);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, targetLocalRotation, Time.deltaTime * rotationLerpSpeed);
    }
    
    public void TryPurchaseItem()
    {
        int playerGold = PlayerInventory.Instance?.GetGold() ?? 0;
        
        if (playerGold >= itemData.Price)
        {
            PlayerInventory.Instance?.AddGold(-itemData.Price);
            PlayerInventory.Instance?.AddItem(itemData);
            
            Debug.Log($"Purchased {itemData.ItemName} for {itemData.Price} gold!");
            // TODO: Add purchase confirmation message or sound effect
            string dialogue = shopkeeper.ResolveDialogue(player, ShopInteractionResult.Purchased);
            UIManager.Instance.SetShopDialogue(dialogue);
            
            ShopTooltip.Instance?.Hide();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"Not enough gold! Need {itemData.Price}, have {playerGold}");
            // TODO: Show error message or sound effect
            string dialogue = shopkeeper.ResolveDialogue(player, ShopInteractionResult.CannotAfford);
            UIManager.Instance.SetShopDialogue(dialogue);
        }
    }
}
