using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Player Stats")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TMP_Text healthNumber;

    [Header("Abilities")]
    [SerializeField] private Image[] abilityBGIcons;
    [SerializeField] private Image[] abilityIcons;
    [SerializeField] private TMP_Text[] abilityCDTexts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        float fill = 1 - NormalizeFill(currentHealth, maxHealth);
        healthBar.fillAmount = fill;
        healthNumber.text = currentHealth.ToString("N0") + "/" + maxHealth.ToString("N0");
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

    public static float NormalizeFill(float currentFill, float fillMax)
    {
        if (fillMax <= 0f)
            return 1f;

        return Mathf.Clamp01(1f - (currentFill / fillMax));
    }
}
