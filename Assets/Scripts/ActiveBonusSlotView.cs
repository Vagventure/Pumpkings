using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveBonusSlotView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image effectIconDisplay;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Image cooldownFillImage;

    private BonusDefinition bonus;

    public BonusDefinition Bonus => bonus;

    private void OnEnable()
    {
        Refresh();
    }

    public void Configure(BonusDefinition bonusDefinition)
    {
        bonus = bonusDefinition;
        Refresh();
    }

    private void Refresh()
    {
        if (effectIconDisplay != null)
        {
            Sprite effectIcon = bonus == null ? null : bonus.EffectIcon;
            effectIconDisplay.sprite = effectIcon;
            effectIconDisplay.enabled = effectIcon != null;
            effectIconDisplay.preserveAspect = true;
        }

        if (valueText != null)
        {
            bool showValue = bonus != null;
            valueText.gameObject.SetActive(showValue);
            valueText.text = showValue ? RewardEffectFormatter.FormatBonusValue(bonus) : string.Empty;
        }

        if (cooldownFillImage != null)
        {
            cooldownFillImage.gameObject.SetActive(false);
        }
    }
}
