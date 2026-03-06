using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShopTooltip : MonoBehaviour
{
    public static ShopTooltip Instance;
    
    [Header("UI References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;
    public Image iconImage;
    
    [Header("Settings")]
    public Vector2 offset = new Vector2(20f, -20f);
    public bool followMouse = true;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        rectTransform = tooltipPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        Hide();
    }
    
    private void Update()
    {
        if (tooltipPanel.activeSelf && followMouse)
        {
            UpdatePosition();
        }
    }
    
    public void Show(ItemData itemData)
    {
        if (itemData == null) return;
        
        itemNameText.text = itemData.ItemName;
        descriptionText.text = itemData.Description;
        priceText.text = $"{itemData.Price} Gold";
        
        if (iconImage != null && itemData.Icon != null)
        {
            iconImage.sprite = itemData.Icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
        
        tooltipPanel.SetActive(true);
        UpdatePosition();
    }
    
    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
    
    private void UpdatePosition()
    {
        if (canvas == null) return;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        // Convert mouse position to canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );
        
        // Apply offset
        localPoint += offset;
        
        // Keep tooltip within screen bounds
        Vector2 sizeDelta = rectTransform.sizeDelta;
        RectTransform canvasRect = canvas.transform as RectTransform;
        
        float maxX = canvasRect.rect.width / 2 - sizeDelta.x / 2;
        float maxY = canvasRect.rect.height / 2 - sizeDelta.y / 2;
        
        localPoint.x = Mathf.Clamp(localPoint.x, -maxX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, -maxY, maxY);
        
        rectTransform.localPosition = localPoint;
    }
}
