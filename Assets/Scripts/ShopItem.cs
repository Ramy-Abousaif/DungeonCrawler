using UnityEngine;
using ProPixelizer;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [SerializeField] private Shopkeeper shopkeeper;

    [Header("Item Data")]
    public ItemData itemData;
    [SerializeField] private GameObject wholeCardGO;
    [SerializeField] private TMP_Text titleText;

    [Header("Hover Visuals")]
    [SerializeField] private float hoverLift = 0.2f;
    [SerializeField] private float maxTilt = 8f;
    [SerializeField] private float tiltScreenRange = 180f;
    [SerializeField] private float positionLerpSpeed = 12f;
    [SerializeField] private float rotationLerpSpeed = 12f;
    [SerializeField] private Vector3 faceRotationOffset;

    [Header("Material References Used For Rarity")]
    [SerializeField] private Image[] rarityImages;
    [SerializeField] private TMP_Text[] rarityTextTargets;
    [SerializeField] private Material[] rarityMaterials;

    private static readonly string[] RarityKeywords = System.Array.ConvertAll(
        System.Enum.GetNames(typeof(ItemRarity)),
        rarityName => rarityName.ToUpperInvariant());
    private const string RarityKeywordPrefix = "_RARITY_";
    private static readonly int RarityPropertyId = Shader.PropertyToID("_Rarity");
    private static readonly int RarityPropertyUpperId = Shader.PropertyToID("_RARITY");

    private Material mat;
    private bool isHighlighted = false;
    private Camera shopCamera;
    private PhysicsBasedCharacterController player;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Transform HoverVisual => wholeCardGO != null ? wholeCardGO.transform : transform;

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

        PrepareRarityTargetInstances();
        ApplyRarityKeyword(itemData.Rarity);
    }

    private void PrepareRarityTargetInstances()
    {
        PrepareImageMaterialInstances();
        PrepareTextMaterialInstances();
        PrepareRendererMaterialInstances();
    }

    private void PrepareImageMaterialInstances()
    {
        if (rarityImages == null)
            return;

        for (int i = 0; i < rarityImages.Length; i++)
        {
            Image imageTarget = rarityImages[i];
            if (imageTarget == null)
                continue;

            Material sourceMaterial = imageTarget.material != null
                ? imageTarget.material
                : imageTarget.materialForRendering;

            if (sourceMaterial == null)
                continue;

            imageTarget.material = new Material(sourceMaterial);
        }
    }

    private void PrepareTextMaterialInstances()
    {
        if (rarityTextTargets == null)
            return;

        for (int targetIndex = 0; targetIndex < rarityTextTargets.Length; targetIndex++)
        {
            TMP_Text textTarget = rarityTextTargets[targetIndex];
            if (textTarget == null)
                continue;

            Material[] sourceMaterials = textTarget.fontMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0)
                continue;

            Material[] instancedMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material sourceMaterial = sourceMaterials[materialIndex];
                if (sourceMaterial != null)
                    instancedMaterials[materialIndex] = new Material(sourceMaterial);
            }

            textTarget.fontMaterials = instancedMaterials;
        }
    }

    private void PrepareRendererMaterialInstances()
    {
        if (rarityMaterials == null || rarityMaterials.Length == 0)
            return;

        Transform root = wholeCardGO != null ? wholeCardGO.transform : transform;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        for (int rarityMaterialIndex = 0; rarityMaterialIndex < rarityMaterials.Length; rarityMaterialIndex++)
        {
            Material sourceMaterial = rarityMaterials[rarityMaterialIndex];
            if (sourceMaterial == null)
                continue;

            Material instancedMaterial = null;

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer targetRenderer = renderers[rendererIndex];
                if (targetRenderer == null)
                    continue;

                Material[] sharedMaterials = targetRenderer.sharedMaterials;
                Material[] runtimeMaterials = targetRenderer.materials;
                bool changed = false;

                int slotCount = Mathf.Min(sharedMaterials.Length, runtimeMaterials.Length);
                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    Material sharedMaterial = sharedMaterials[slotIndex];
                    Material runtimeMaterial = runtimeMaterials[slotIndex];

                    bool sharedMatches = MaterialMatchesSource(sharedMaterial, sourceMaterial);
                    bool runtimeMatches = MaterialMatchesSource(runtimeMaterial, sourceMaterial);
                    if (!sharedMatches && !runtimeMatches)
                        continue;

                    if (instancedMaterial == null)
                    {
                        Material instanceSource = runtimeMaterial != null ? runtimeMaterial : sourceMaterial;
                        instancedMaterial = new Material(instanceSource);
                    }

                    runtimeMaterials[slotIndex] = instancedMaterial;
                    changed = true;
                }

                if (changed)
                    targetRenderer.materials = runtimeMaterials;
            }

            if (instancedMaterial != null)
                rarityMaterials[rarityMaterialIndex] = instancedMaterial;
            else
            {
                Material existingRuntimeMaterial = FindFirstMatchingRendererMaterial(renderers, sourceMaterial);
                if (existingRuntimeMaterial != null)
                    rarityMaterials[rarityMaterialIndex] = existingRuntimeMaterial;
            }
        }
    }

    private static bool MaterialMatchesSource(Material candidate, Material sourceMaterial)
    {
        if (candidate == null || sourceMaterial == null)
            return false;

        if (candidate == sourceMaterial)
            return true;

        string sourceName = sourceMaterial.name;
        string candidateName = candidate.name;

        return candidateName == sourceName
            || candidateName.StartsWith(sourceName + " (", System.StringComparison.Ordinal);
    }

    private static Material FindFirstMatchingRendererMaterial(Renderer[] renderers, Material sourceMaterial)
    {
        if (renderers == null || sourceMaterial == null)
            return null;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer targetRenderer = renderers[rendererIndex];
            if (targetRenderer == null)
                continue;

            Material[] runtimeMaterials = targetRenderer.materials;
            for (int slotIndex = 0; slotIndex < runtimeMaterials.Length; slotIndex++)
            {
                Material runtimeMaterial = runtimeMaterials[slotIndex];
                if (MaterialMatchesSource(runtimeMaterial, sourceMaterial))
                    return runtimeMaterial;
            }
        }

        return null;
    }

    private void ApplyRarityKeyword(ItemRarity rarity)
    {
        ApplyKeywordToMaterialTargets(rarityMaterials, rarity);
        ApplyKeywordToImageTargets(rarityImages, rarity);
        ApplyKeywordToTextTargets(rarityTextTargets, rarity);
    }

    private static void ApplyKeywordToImageTargets(Image[] imageTargets, ItemRarity rarity)
    {
        if (imageTargets == null)
            return;

        for (int i = 0; i < imageTargets.Length; i++)
        {
            Image imageTarget = imageTargets[i];
            if (imageTarget == null)
                continue;

            Material rarityMaterial = imageTarget.material;
            if (rarityMaterial == null)
            {
                Material baseMaterial = imageTarget.materialForRendering;
                if (baseMaterial == null)
                    continue;

                rarityMaterial = new Material(baseMaterial);
                imageTarget.material = rarityMaterial;
            }

            SetExclusiveRarityKeyword(rarityMaterial, rarity);
        }
    }

    private static void ApplyKeywordToTextTargets(TMP_Text[] textTargets, ItemRarity rarity)
    {
        if (textTargets == null)
            return;

        foreach (TMP_Text textTarget in textTargets)
        {
            if (textTarget == null)
                continue;

            Material[] textMaterials = textTarget.fontMaterials;
            for (int i = 0; i < textMaterials.Length; i++)
            {
                Material rarityMaterial = textMaterials[i];
                if (rarityMaterial != null)
                    SetExclusiveRarityKeyword(rarityMaterial, rarity);
            }
        }
    }

    private static void ApplyKeywordToMaterialTargets(Material[] materials, ItemRarity rarity)
    {
        if (materials == null)
            return;

        for (int i = 0; i < materials.Length; i++)
        {
            Material rarityMaterial = materials[i];
            if (rarityMaterial != null)
                SetExclusiveRarityKeyword(rarityMaterial, rarity);
        }
    }

    private static void SetExclusiveRarityKeyword(Material rarityMaterial, ItemRarity rarity)
    {
        if (rarityMaterial == null)
            return;

        string selectedRarityKeyword = rarity.ToString().ToUpperInvariant();

        for (int i = 0; i < RarityKeywords.Length; i++)
        {
            string keyword = RarityKeywords[i];
            rarityMaterial.DisableKeyword(keyword);
            rarityMaterial.DisableKeyword(RarityKeywordPrefix + keyword);
        }

        rarityMaterial.EnableKeyword(selectedRarityKeyword);
        rarityMaterial.EnableKeyword(RarityKeywordPrefix + selectedRarityKeyword);

        if (rarityMaterial.HasProperty(RarityPropertyId))
            rarityMaterial.SetFloat(RarityPropertyId, (float)rarity);

        if (rarityMaterial.HasProperty(RarityPropertyUpperId))
            rarityMaterial.SetFloat(RarityPropertyUpperId, (float)rarity);
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
            Destroy(wholeCardGO);
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