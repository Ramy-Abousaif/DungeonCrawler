using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Player Portrait")]
    [SerializeField] private Image playerPortrait;

    [Header("Player Stats")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TMP_Text healthNumber;
    [SerializeField] private TMP_Text goldAmountText;

    [Header("Shop UI")]
    [SerializeField] private Image blackScreen;
    [SerializeField] private GameObject shopUI;
    [SerializeField] TMP_Text shopDialogueText;

    [Header("Abilities")]
    [SerializeField] private Image[] abilityBGIcons;
    [SerializeField] private Image[] abilityIcons;
    [SerializeField] private TMP_Text[] abilityCDTexts;

    private PlayerInventory goldInventory;
    private Coroutine waitForGoldInventoryRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TryBindGoldCounter();

        if (goldInventory == null && waitForGoldInventoryRoutine == null)
            waitForGoldInventoryRoutine = StartCoroutine(WaitForGoldInventory());
    }

    private void OnDisable()
    {
        if (waitForGoldInventoryRoutine != null)
        {
            StopCoroutine(waitForGoldInventoryRoutine);
            waitForGoldInventoryRoutine = null;
        }

        UnbindGoldCounter();
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        float fill = 1 - NormalizeFill(currentHealth, maxHealth);
        healthBar.fillAmount = fill;
        healthNumber.text = currentHealth.ToString("N0") + "/" + maxHealth.ToString("N0");
    }

    public void UpdatePlayerPortrait(Sprite newPortrait)
    {
        playerPortrait.sprite = newPortrait;
    }

    public void SetupAbilityIcons(int i, float timer, Sprite icon)
    {
        float cdTimer = timer;
        abilityBGIcons[i].sprite = icon;
        abilityIcons[i].sprite = icon;
        if(cdTimer > 0)
        {
            abilityCDTexts[i].text = cdTimer.ToString("N1");   
        }
        else
        {
            abilityCDTexts[i].transform.gameObject.SetActive(false);
        }
    }

    public void UpdateCooldown(int index, float timer, float duration)
    {
        float fill = NormalizeFill(timer, duration);
        abilityIcons[index].fillAmount = fill;

        abilityCDTexts[index].gameObject.SetActive(timer > 0f);
        abilityCDTexts[index].text = timer.ToString("N1");
    }

    public void ToggleShopUI(bool isActive)
    {
        shopUI.SetActive(isActive);
    }

    public void SetShopDialogue(string dialogue)
    {
        if (shopDialogueText == null)
            return;

        if (shopDialogueText.TryGetComponent(out TypewriterEffect typewriter))
        {
            typewriter.PlayText(dialogue);
            return;
        }

        shopDialogueText.text = dialogue ?? string.Empty;
    }

    public void UpdateGoldAmount(int amount)
    {
        if (goldAmountText == null)
            return;

        goldAmountText.text = amount.ToString("N0");
    }

    private IEnumerator WaitForGoldInventory()
    {
        while (goldInventory == null)
        {
            TryBindGoldCounter();
            if (goldInventory != null)
                break;

            yield return null;
        }

        waitForGoldInventoryRoutine = null;
    }

    private void TryBindGoldCounter()
    {
        if (goldInventory != null)
            return;

        if (PlayerInventory.Instance == null)
            return;

        goldInventory = PlayerInventory.Instance;
        goldInventory.OnGoldChanged += HandleGoldChanged;
        HandleGoldChanged(goldInventory.GetGold());
    }

    private void UnbindGoldCounter()
    {
        if (goldInventory == null)
            return;

        goldInventory.OnGoldChanged -= HandleGoldChanged;
        goldInventory = null;
    }

    private void HandleGoldChanged(int newAmount)
    {
        UpdateGoldAmount(newAmount);
    }

    public static float NormalizeFill(float currentFill, float fillMax)
    {
        if (fillMax <= 0f)
            return 1f;

        return Mathf.Clamp01(1f - (currentFill / fillMax));
    }

    public Image BlackScreen => blackScreen;
}
