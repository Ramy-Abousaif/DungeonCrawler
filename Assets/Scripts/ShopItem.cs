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
    private Material mat;
    private bool isHighlighted = false;
    private Camera shopCamera;
    private PhysicsBasedCharacterController player;
    
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
        if(!player.inShop)
            return;

        CheckMouseHover();
        CheckMouseClick();
    }
    
    private void CheckMouseHover()
    {
        if (shopCamera == null) return;
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = shopCamera.ScreenPointToRay(mousePos);
        
        bool isHovering = Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject;
        
        if (isHovering && !isHighlighted)
        {
            HighlightItem(true);
            ShopTooltip.Instance?.Show(itemData);
        }
        else if (!isHovering && isHighlighted)
        {
            HighlightItem(false);
            ShopTooltip.Instance?.Hide();
        }
    }
    
    private void CheckMouseClick()
    {
        if (!isHighlighted) return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPurchaseItem();
        }
    }
    
    private void HighlightItem(bool highlight)
    {
        mat.SetFloat("_Selected", highlight ? 1f : 0f);
        isHighlighted = highlight;
    }
    
    private void TryPurchaseItem()
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
