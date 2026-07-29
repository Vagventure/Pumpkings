using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RewardItemView : MonoBehaviour
{
    public static event Action<RewardItemView> Clicked;

    [Header("References")]
    [FormerlySerializedAs("purchaseButton")]
    [SerializeField] private Button button;

    [Header("Feel")]
    [SerializeField] private RewardItemFeelFeedback feelFeedback;

    [Header("UI")]
    [FormerlySerializedAs("displayNameText")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconDisplay;
    [SerializeField] private Image iconDimOverlay;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image effectIconDisplay;
    [SerializeField] private TMP_Text effectValueText;
    [SerializeField] private Image affordabilityBorder;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text durationText;

    [Header("Affordability")]
    [SerializeField, Range(0f, 1f)] private float unaffordableAlpha = 0.45f;

    private RewardItem rewardItem;
    private Action accepted;
    private bool suppressClickEvent;
    private CanvasGroup canvasGroup;

    public RewardItem RewardItem => rewardItem;
    public ShopItemDefinition ShopDefinition => rewardItem as ShopItemDefinition;
    public BonusDefinition BonusDefinition => rewardItem as BonusDefinition;
    public string Title => rewardItem == null ? string.Empty : rewardItem.Title;
    public string Subtitle => rewardItem == null ? string.Empty : rewardItem.Subtitle;
    public string Description => rewardItem == null ? string.Empty : rewardItem.Description;
    public Sprite Icon => rewardItem == null ? null : rewardItem.Icon;
    public int BaseCost => ShopDefinition == null ? 0 : ShopDefinition.Cost;
    public int BaseAwarenessValue => ShopDefinition == null ? 0 : ShopDefinition.AwarenessValue;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        RewardManager.BonusActivated += HandleBonusActivated;
        ScoringService.OnBudgetChanged += HandleBudgetChanged;
        RefreshPresentation();

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }
    }

    private void OnDisable()
    {
        RewardManager.BonusActivated -= HandleBonusActivated;
        ScoringService.OnBudgetChanged -= HandleBudgetChanged;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Configure(RewardItem item)
    {
        rewardItem = item;
        accepted = null;
        suppressClickEvent = false;
        RefreshPresentation();
    }

    public void ConfigureDisplay(RewardItem item, Action onAccepted)
    {
        rewardItem = item;
        accepted = onAccepted;
        suppressClickEvent = true;
        RefreshPresentation();
        EnsureReferences();

        if (button != null)
        {
            button.interactable = accepted != null;
        }
    }

    private void HandleClicked()
    {
        if (accepted != null)
        {
            Action callback = accepted;
            accepted = null;

            if (button != null)
            {
                button.interactable = false;
            }

            PlayAcceptedFeedback(callback);
            return;
        }

        if (suppressClickEvent)
        {
            return;
        }

        Clicked?.Invoke(this);
    }

    public void PlayAcceptedFeedback(Action completed = null)
    {
        EnsureReferences();

        if (feelFeedback == null)
        {
            completed?.Invoke();
            return;
        }

        feelFeedback.PlayAccepted(completed);
    }

    public void PlayRejectedFeedback()
    {
        EnsureReferences();
        feelFeedback?.PlayRejected();
    }

    private void HandleBonusActivated(BonusDefinition bonus)
    {
        if (ShopDefinition != null)
        {
            RefreshPresentation();
        }
    }

    private void Update()
    {
        if (ShopDefinition is RecyclingPatrolDefinition && !suppressClickEvent)
        {
            RefreshPatrolPresentation();
            RefreshAffordability();
        }
    }

    private void HandleBudgetChanged(int budget)
    {
        RefreshAffordability(budget);
    }

    private void RefreshPresentation()
    {
        EnsureReferences();

        if (titleText != null)
        {
            titleText.text = rewardItem == null ? string.Empty : rewardItem.Title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = rewardItem == null ? string.Empty : rewardItem.Subtitle;
        }

        if (descriptionText != null)
        {
            descriptionText.text = rewardItem == null ? string.Empty : rewardItem.Description;
        }

        if (iconDisplay != null)
        {
            iconDisplay.sprite = rewardItem == null ? null : rewardItem.Icon;
            iconDisplay.enabled = iconDisplay.sprite != null;
            iconDisplay.preserveAspect = true;
        }

        RefreshShopCost();
        RefreshEffect();
        RefreshAffordability();
        RefreshPatrolPresentation();
    }

    private void RefreshAffordability()
    {
        int budget = ScoringService.Instance == null ? 0 : ScoringService.Instance.Budget;
        RefreshAffordability(budget);
    }

    private void RefreshAffordability(int budget)
    {
        bool showShopState = ShopDefinition != null && !suppressClickEvent;
        bool canAfford = !showShopState || budget >= GetFinalCost();
        bool isAvailable = !showShopState || RecyclingPatrolService.IsPurchaseAvailable(ShopDefinition);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = canAfford ? 1f : unaffordableAlpha;
        }

        if (affordabilityBorder != null)
        {
            affordabilityBorder.enabled = showShopState && canAfford && isAvailable;
        }

        if (button != null && showShopState)
        {
            button.interactable = canAfford && isAvailable;
        }
    }

    private void RefreshShopCost()
    {
        ShopItemDefinition shopItem = ShopDefinition;
        bool showCost = shopItem != null;

        if (iconDimOverlay != null)
        {
            iconDimOverlay.gameObject.SetActive(showCost);
        }

        if (costText == null)
        {
            return;
        }

        costText.gameObject.SetActive(showCost);
        costText.text = showCost ? FormatShopCost(shopItem) : string.Empty;
    }

    private void RefreshEffect()
    {
        bool showEffect = rewardItem != null;

        if (effectIconDisplay != null)
        {
            Sprite effectIcon = rewardItem == null ? null : rewardItem.EffectIcon;
            effectIconDisplay.sprite = effectIcon;
            effectIconDisplay.enabled = effectIcon != null;
            effectIconDisplay.gameObject.SetActive(showEffect && effectIcon != null);
            effectIconDisplay.preserveAspect = true;
        }

        if (effectValueText == null)
        {
            return;
        }

        string effectValue = FormatEffectValue();
        effectValueText.text = effectValue;
        effectValueText.gameObject.SetActive(showEffect && !string.IsNullOrWhiteSpace(effectValue));
    }

    private string FormatShopCost(ShopItemDefinition shopItem)
    {
        if (shopItem == null)
        {
            return string.Empty;
        }

        return GetFinalCost().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void RefreshPatrolPresentation()
    {
        RecyclingPatrolDefinition patrolDefinition = ShopDefinition as RecyclingPatrolDefinition;
        RecyclingPatrolService patrolService = RecyclingPatrolService.Instance;
        bool showPatrolState = patrolDefinition != null && !suppressClickEvent;
        float cooldownFill = showPatrolState && patrolService != null
            ? patrolService.GetCooldownFill(patrolDefinition)
            : 0f;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(showPatrolState && cooldownFill > 0f);
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Horizontal;
            cooldownOverlay.fillOrigin = (int)Image.OriginHorizontal.Right;
            cooldownOverlay.fillAmount = cooldownFill;
        }

        if (durationText == null)
        {
            return;
        }

        float remainingSeconds = 0f;
        bool showDuration = showPatrolState
            && patrolService != null
            && patrolService.TryGetLatestWorkRemaining(patrolDefinition, out remainingSeconds);
        durationText.gameObject.SetActive(showDuration);
        durationText.text = showDuration
            ? $"{remainingSeconds:0.0} s"
            : string.Empty;
    }

    private int GetFinalCost()
    {
        RewardManager rewardManager = RewardManager.Instance;
        return rewardManager == null ? BaseCost : rewardManager.GetFinalCost(this);
    }

    private string FormatEffectValue()
    {
        if (ShopDefinition != null)
        {
            if (ShopDefinition is RecyclingPatrolDefinition patrolDefinition)
            {
                return $"{patrolDefinition.WorkDuration:0.#}s";
            }

            RewardManager rewardManager = RewardManager.Instance;
            int value = rewardManager == null ? ShopDefinition.AwarenessValue : rewardManager.GetFinalAwarenessValue(this);
            return RewardEffectFormatter.FormatShopAwarenessValue(value);
        }

        return RewardEffectFormatter.FormatEffectValue(rewardItem);
    }

    private void EnsureReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (feelFeedback == null)
        {
            feelFeedback = GetComponent<RewardItemFeelFeedback>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

    }
}
