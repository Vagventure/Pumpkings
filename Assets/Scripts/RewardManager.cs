using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RewardManager : MonoBehaviour
{
    public static event Action<BonusDefinition> BonusActivated;
    public static event Action<BonusDefinition> AutoCollectRequested;
    public static event Action<int> PassiveAwarenessRequested;
    public static event Action OnEventSpawnSFX;
    public static event Action OnEventDurationSFX;
    public static event Action<EventPresentationResolver, EventDefinition> OnProgressEventShown;
    public static event Action<ProgressEventContext> ProgressEventCompleted;
    public static event Action OnRewardChoiceShownSFX;
    public static event Action OnRewardChoiceSelectedSFX;
    public static event Action OnShopItemUnlockedSFX;
    public static event Action OnBonusUnlockedSFX;

    public static RewardManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RewardSelectionUI rewardSelectionUI;
    [SerializeField] private MonoBehaviour eventResolver;
    [SerializeField] private ShopController shopController;

    [Header("Feedback")]
    [SerializeField] private RewardPresentationPresenter shopItemRewardPresenter;
    [SerializeField] private RewardPresentationPresenter passiveRewardPresenter;

    [Header("Reward Catalog")]
    [FormerlySerializedAs("bonusCatalog")]
    [SerializeField] private RewardCatalog rewardCatalog;

    [Header("Level")]
    [SerializeField] private LevelController levelController;

    [Header("Runtime State")]
    [SerializeField] private List<BonusDefinition> activeBonuses = new();

    private readonly Dictionary<BonusDefinition, float> bonusTimers = new();
    private readonly Queue<ProgressEventContext> pendingProgressEvents = new();
    private readonly List<RewardItem> currentRewardChoices = new();
    private readonly List<DialogueChoiceDefinition> currentDialogueChoices = new();

    private ProgressEventContext currentProgressEvent;
    private RewardItem currentEventSelectedReward;
    private bool currentEventDialogueChoiceSelected;
    private bool rewardSelectionOpen;
    private bool progressEventFlowOpen;
    private bool pausedGameplayForReward;
    private bool rewardPresentationOpen;
    private bool waitingForStageTransition;

    public RewardCatalog Catalog => rewardCatalog;
    public LevelController LevelController => GetLevelController();
    public IReadOnlyList<BonusDefinition> ActiveBonuses => activeBonuses;
    public bool IsRewardSelectionOpen => rewardSelectionOpen;
    public bool IsProgressEventFlowOpen => progressEventFlowOpen;
    public ShopController ShopController => GetShopController();
    private EventPresentationResolver AssignedEventResolver => eventResolver as EventPresentationResolver;

    internal static void RaiseRewardChoiceShownSFX()
    {
        OnRewardChoiceShownSFX?.Invoke();
    }

    private void Awake()
    {
        SetupSingleton();
    }

    public bool TryGetTimer(BonusDefinition bonus, out float timer)
    {
        return bonusTimers.TryGetValue(bonus, out timer);
    }

    private void OnEnable()
    {
        ProgressTracker.ProgressEventReached += HandleProgressEventReached;
        StageManager.TransitionStarted += HandleStageTransitionStarted;
        StageManager.TransitionCompleted += HandleStageTransitionCompleted;
    }

    private void OnDisable()
    {
        ProgressTracker.ProgressEventReached -= HandleProgressEventReached;
        StageManager.TransitionStarted -= HandleStageTransitionStarted;
        StageManager.TransitionCompleted -= HandleStageTransitionCompleted;
        currentProgressEvent = default;
        currentRewardChoices.Clear();
        currentDialogueChoices.Clear();
        currentEventSelectedReward = null;
        currentEventDialogueChoiceSelected = false;
        rewardSelectionOpen = false;
        progressEventFlowOpen = false;
        rewardPresentationOpen = false;
        waitingForStageTransition = false;
        HideProgressEventPresentationRoot();
        ResumeGameplayForReward();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    private void Update()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager != null && gameManager.IsPaused)
        {
            return;
        }

        TickTimedBonuses();
    }

    public void ActivateBonus(BonusDefinition bonus)
    {
        if (bonus == null)
        {
            return;
        }

        if (rewardCatalog != null && !rewardCatalog.Contains(bonus))
        {
            Debug.LogWarning($"Bonus is not part of the active catalog: {bonus.name}");
            return;
        }

        if (activeBonuses.Contains(bonus))
        {
            return;
        }

        activeBonuses.Add(bonus);

        if (bonus.UsesTimer())
        {
            bonusTimers[bonus] = 0f;
        }

        BonusActivated?.Invoke(bonus);
    }

    public bool IsBonusActive(BonusDefinition bonus)
    {
        return bonus != null && activeBonuses.Contains(bonus);
    }

    private void HandleProgressEventReached(ProgressEventContext progressEvent)
    {
        pendingProgressEvents.Enqueue(progressEvent);

        if (!progressEventFlowOpen && !waitingForStageTransition)
        {
            ShowNextPendingProgressEvent();
        }
    }

    private void ShowNextPendingProgressEvent()
    {
        while (pendingProgressEvents.Count > 0)
        {
            ProgressEventContext progressEvent = pendingProgressEvents.Dequeue();
            ProgressEventDefinition definition = progressEvent.Definition;

            if (definition == null)
            {
                Debug.LogWarning($"RewardManager: {progressEvent.Metric} progress event has no definition.");
                continue;
            }

            currentRewardChoices.Clear();
            currentDialogueChoices.Clear();
            currentEventSelectedReward = null;
            currentEventDialogueChoiceSelected = false;
            BuildDialogueChoices(definition, currentDialogueChoices);
            BuildRewardChoices(currentDialogueChoices, currentRewardChoices);

            bool hasEventPresentation = definition.EventDefinition != null || currentDialogueChoices.Count > 0;
            bool hasConfiguredRewards = currentRewardChoices.Count > 0 || HasLegacyRewardItems(definition);

            if (!hasEventPresentation && !hasConfiguredRewards)
            {
                Debug.LogWarning($"RewardManager: Empty {progressEvent.Metric} progress event at {definition.RequiredValue} has no event definition, dialogue choices, or rewards.");
                continue;
            }

            currentProgressEvent = progressEvent;
            progressEventFlowOpen = true;
            PauseGameplayForReward();

            if (hasEventPresentation && TryShowProgressEvent(definition))
            {
                return;
            }

            ShowRewardSelectionOrComplete(definition);
            return;
        }

        ResumeGameplayForReward();
    }

    private bool TryShowProgressEvent(ProgressEventDefinition definition)
    {
        EventPresentationResolver resolver = GetAssignedEventResolver();

        if (resolver == null)
        {
            Debug.LogError($"RewardManager: Event Resolver is required on '{name}'. Assign DiscoEventPresentationResolver or VisualNovelEventPresentationResolver.");
            return false;
        }

        OnEventSpawnSFX?.Invoke();
        resolver.StartEvent(
            definition.EventDefinition,
            currentDialogueChoices,
            definition.ChoicesSide,
            HandleEventRewardSelected,
            HandleProgressEventFinished);
        OnProgressEventShown?.Invoke(resolver, definition.EventDefinition);
        OnEventDurationSFX?.Invoke();
        return true;
    }

    private EventPresentationResolver GetAssignedEventResolver()
    {
        EventPresentationResolver assignedResolver = AssignedEventResolver;

        if (assignedResolver != null && assignedResolver.IsPanelController)
        {
            return assignedResolver;
        }

        if (eventResolver != null && assignedResolver == null)
        {
            Debug.LogWarning($"RewardManager: Assigned Event Resolver on '{name}' must inherit from EventPresentationResolver.");
        }

        if (assignedResolver != null && !assignedResolver.IsPanelController)
        {
            Debug.LogWarning($"RewardManager: Ignoring '{assignedResolver.name}' because it is not configured as an event presentation resolver.");
        }

        return null;
    }

    private void OnValidate()
    {
        if (eventResolver != null && eventResolver is not EventPresentationResolver)
        {
            Debug.LogWarning($"RewardManager: Event Resolver on '{name}' must be DiscoEventPresentationResolver or VisualNovelEventPresentationResolver.");
        }
    }

    private void HandleProgressEventFinished()
    {
        if (currentEventSelectedReward != null)
        {
            ApplySelectedEventReward(currentEventSelectedReward);
            currentEventSelectedReward = null;
            return;
        }

        if (currentEventDialogueChoiceSelected)
        {
            CompleteProgressEventFlow();
            return;
        }

        ShowRewardSelectionOrComplete(currentProgressEvent.Definition);
    }

    private void ShowRewardSelectionOrComplete(ProgressEventDefinition definition)
    {
        if (currentRewardChoices.Count == 0)
        {
            if (definition != null && HasLegacyRewardItems(definition))
            {
                Debug.LogWarning($"RewardManager: No valid rewards for {currentProgressEvent.Metric} progress event at {definition.RequiredValue}. Check the configured reward paths and catalog.");
            }

            CompleteProgressEventFlow();
            return;
        }

        ShowRewardSelection(definition);
    }

    private void ShowRewardSelection(ProgressEventDefinition definition)
    {
        rewardSelectionOpen = true;
        OnRewardChoiceShownSFX?.Invoke();

        if (definition != null)
        {
            Debug.Log($"RewardManager: Showing {currentRewardChoices.Count} reward choices.");
        }

        if (rewardSelectionUI == null)
        {
            Debug.LogWarning("RewardManager: RewardSelectionUI is missing. Activating the first valid reward.");
            SelectReward(currentRewardChoices[0]);
            return;
        }

        string rewardTitle = definition == null || definition.EventDefinition == null
            ? string.Empty
            : definition.EventDefinition.RewardTitle;

        rewardSelectionUI.Show(currentRewardChoices, rewardTitle, SelectReward);
    }

    private void BuildRewardChoices(ProgressEventDefinition definition, List<RewardItem> results)
    {
        if (definition == null || definition.RewardItems == null)
        {
            return;
        }

        List<RewardItem> availableRewards = new();

        for (int i = 0; i < definition.RewardItems.Count; i++)
        {
            RewardItem reward = definition.RewardItems[i];

            if (!CanOfferReward(reward))
            {
                continue;
            }

            availableRewards.Add(reward);
        }

        while (availableRewards.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            results.Add(availableRewards[randomIndex]);
            availableRewards.RemoveAt(randomIndex);
        }
    }

    private void BuildRewardChoices(IReadOnlyList<DialogueChoiceDefinition> availableChoices, List<RewardItem> results)
    {
        if (availableChoices == null || results == null)
        {
            return;
        }

        for (int i = 0; i < availableChoices.Count; i++)
        {
            RewardItem reward = availableChoices[i]?.Reward;
            if (reward != null && !results.Contains(reward))
            {
                results.Add(reward);
            }
        }
    }

    private void BuildDialogueChoices(ProgressEventDefinition definition, List<DialogueChoiceDefinition> results)
    {
        if (definition == null || results == null)
        {
            return;
        }

        IReadOnlyList<DialogueChoiceDefinition> configuredChoices = definition.DialogueChoices;

        if (configuredChoices != null && configuredChoices.Count > 0)
        {
            for (int i = 0; i < configuredChoices.Count; i++)
            {
                DialogueChoiceDefinition choice = configuredChoices[i];
                if (choice == null)
                {
                    continue;
                }

                if (!TryResolveDialogueChoice(choice, out DialogueChoiceDefinition resolvedChoice))
                {
                    continue;
                }

                results.Add(resolvedChoice);
            }

            if (results.Count == 0)
            {
                Debug.LogWarning($"RewardManager: No valid dialogue choices for progress event at {definition.RequiredValue}. Check Dialogue Choices text, reward paths, and Reward Catalog.");
            }

            return;
        }

        BuildRewardChoices(definition, currentRewardChoices);

        for (int i = 0; i < currentRewardChoices.Count; i++)
        {
            results.Add(DialogueChoiceDefinition.CreateRuntime(currentRewardChoices[i]));
        }
    }

    private static bool HasLegacyRewardItems(ProgressEventDefinition definition)
    {
        return definition != null && definition.RewardItems != null && definition.RewardItems.Count > 0;
    }

    private LevelController GetLevelController()
    {
        if (levelController == null)
        {
            levelController = global::LevelController.Instance;
        }

        return levelController;
    }

    private ShopController GetShopController()
    {
        if (shopController == null)
        {
            shopController = global::ShopController.Instance;
        }

        return shopController;
    }

    private bool CanOfferReward(RewardItem reward)
    {
        if (reward == null)
        {
            return false;
        }

        return reward switch
        {
            BonusDefinition bonus => CanOfferBonusReward(bonus),
            ShopItemDefinition shopItem => CanOfferShopItemReward(shopItem),
            _ => false
        };
    }

    private bool TryResolveDialogueChoice(
        DialogueChoiceDefinition choice,
        out DialogueChoiceDefinition resolvedChoice)
    {
        resolvedChoice = null;

        if (choice == null)
        {
            return false;
        }

        if (choice.RewardPath == RewardPath.None)
        {
            if (string.IsNullOrWhiteSpace(choice.GetButtonText())
                && string.IsNullOrWhiteSpace(choice.GetPlayerLine()))
            {
                return false;
            }

            resolvedChoice = choice.Resolve(null);
            return true;
        }

        if (rewardCatalog == null)
        {
            Debug.LogWarning("RewardManager: Reward Catalog is missing.");
            return false;
        }

        if (!rewardCatalog.TryGetNextReward(choice.RewardPath, IsRewardOwned, out RewardItem reward))
        {
            return false;
        }

        resolvedChoice = choice.Resolve(reward);
        return true;
    }

    private bool CanOfferBonusReward(BonusDefinition bonus)
    {
        if (bonus == null || IsBonusActive(bonus))
        {
            return false;
        }

        return true;
    }

    private bool IsRewardOwned(RewardItem reward)
    {
        return reward switch
        {
            BonusDefinition bonus => IsBonusActive(bonus),
            ShopItemDefinition shopItem => GetShopController() != null
                && GetShopController().IsShopItemUnlocked(shopItem),
            _ => false
        };
    }

    private bool CanOfferShopItemReward(ShopItemDefinition shopItem)
    {
        if (shopItem == null)
        {
            return false;
        }

        ShopController controller = GetShopController();
        return controller == null || !controller.IsShopItemUnlocked(shopItem);
    }

    private void SelectReward(RewardItem reward)
    {
        if (!rewardSelectionOpen || rewardPresentationOpen)
        {
            return;
        }

        if (!currentRewardChoices.Contains(reward))
        {
            Debug.LogWarning("RewardManager: Selected reward was not part of the current choices.");
            return;
        }

        if (rewardSelectionUI != null)
        {
            rewardSelectionUI.Hide();
        }

        rewardSelectionOpen = false;
        OnRewardChoiceSelectedSFX?.Invoke();
        PlayUnlockedFeedback(reward, () =>
        {
            ApplyReward(reward);
            CompleteProgressEventFlow();
        });
    }

    private void HandleEventRewardSelected(RewardItem reward)
    {
        if (reward == null)
        {
            currentEventDialogueChoiceSelected = true;
            return;
        }

        if (!currentRewardChoices.Contains(reward))
        {
            Debug.LogWarning("RewardManager: Dialogue event selected reward was not part of the current choices.");
            return;
        }

        currentEventDialogueChoiceSelected = true;
        currentEventSelectedReward = reward;
    }

    private void ApplySelectedEventReward(RewardItem reward)
    {
        OnRewardChoiceSelectedSFX?.Invoke();
        PlayUnlockedFeedback(reward, () =>
        {
            ApplyReward(reward);
            CompleteProgressEventFlow();
        });
    }

    private void PlayUnlockedFeedback(RewardItem reward, Action completed)
    {
        switch (reward)
        {
            case BonusDefinition:
                OnBonusUnlockedSFX?.Invoke();
                PlayRewardPresentation(reward, completed);
                break;
            case ShopItemDefinition:
                OnShopItemUnlockedSFX?.Invoke();
                PlayRewardPresentation(reward, completed);
                break;
            default:
                completed?.Invoke();
                break;
        }
    }

    private void PlayRewardPresentation(RewardItem reward, Action completed)
    {
        if (reward == null)
        {
            Debug.LogError("RewardManager: Cannot present a null reward.");
            completed?.Invoke();
            return;
        }

        if (rewardPresentationOpen)
        {
            Debug.LogWarning("RewardManager: Reward presentation is already open. Ignoring duplicate presentation request.");
            return;
        }

        rewardPresentationOpen = true;

        EventPresentationResolver resolver = AssignedEventResolver;

        if (resolver == null || !resolver.TryPresentReward(reward, () => PlayRewardTypePresentation(reward, completed)))
        {
            Debug.LogError("RewardManager: Event Resolver cannot present rewards. Check the reward container and prefab bindings.");
            PlayRewardTypePresentation(reward, completed);
        }
    }

    private void PlayRewardTypePresentation(RewardItem reward, Action completed)
    {
        RewardPresentationPresenter presenter = GetRewardPresenter(reward);

        if (presenter == null)
        {
            Debug.LogWarning($"RewardManager: Missing reward presenter for '{reward.name}'. Applying reward without presentation VFX.");
            CompleteRewardPresentation(completed);
            return;
        }

        presenter.Present(reward, () => CompleteRewardPresentation(completed));
    }

    private RewardPresentationPresenter GetRewardPresenter(RewardItem reward)
    {
        return reward switch
        {
            ShopItemDefinition => shopItemRewardPresenter,
            BonusDefinition => passiveRewardPresenter,
            _ => null
        };
    }

    private void CompleteRewardPresentation(Action completed)
    {
        if (!rewardPresentationOpen)
        {
            return;
        }

        rewardPresentationOpen = false;
        completed?.Invoke();
    }

    public bool ApplyReward(RewardItem reward)
    {
        if (reward == null)
        {
            Debug.LogWarning("RewardManager: Cannot apply an invalid reward definition.");
            return false;
        }

        switch (reward)
        {
            case BonusDefinition bonus:
                ActivateBonus(bonus);
                return true;
            case ShopItemDefinition shopItem:
                return UnlockShopItemReward(shopItem);
            default:
                Debug.LogWarning($"RewardManager: Unsupported reward item '{reward.name}'.");
                return false;
        }
    }

    private bool UnlockShopItemReward(ShopItemDefinition shopItem)
    {
        ShopController controller = GetShopController();

        if (controller == null)
        {
            Debug.LogWarning("RewardManager: Cannot unlock a shop item because ShopController is missing.");
            return false;
        }

        return controller.UnlockShopItem(shopItem);
    }

    private void CompleteProgressEventFlow()
    {
        ProgressEventContext completedProgressEvent = currentProgressEvent;

        rewardSelectionOpen = false;
        progressEventFlowOpen = false;
        rewardPresentationOpen = false;
        currentProgressEvent = default;
        currentRewardChoices.Clear();
        currentDialogueChoices.Clear();
        currentEventSelectedReward = null;
        currentEventDialogueChoiceSelected = false;

        if (rewardSelectionUI != null)
        {
            rewardSelectionUI.Hide();
        }

        HideProgressEventPresentationRoot();

        ProgressEventCompleted?.Invoke(completedProgressEvent);

        if (waitingForStageTransition)
        {
            return;
        }

        ContinueAfterProgressEventFlow();
    }

    private void HandleStageTransitionStarted()
    {
        waitingForStageTransition = true;
    }

    private void HandleStageTransitionCompleted()
    {
        if (!waitingForStageTransition)
        {
            return;
        }

        waitingForStageTransition = false;

        if (progressEventFlowOpen)
        {
            return;
        }

        ContinueAfterProgressEventFlow();
    }

    private void ContinueAfterProgressEventFlow()
    {
        if (pendingProgressEvents.Count > 0)
        {
            ShowNextPendingProgressEvent();
            return;
        }

        ResumeGameplayForReward();
    }

    private void HideProgressEventPresentationRoot()
    {
        AssignedEventResolver?.HidePresentationRoot();
    }

    private void PauseGameplayForReward()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || pausedGameplayForReward)
        {
            return;
        }

        gameManager.PauseGame();
        pausedGameplayForReward = gameManager.IsPaused;
    }

    private void ResumeGameplayForReward()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || !pausedGameplayForReward)
        {
            return;
        }

        gameManager.ResumeGame();
        pausedGameplayForReward = false;
    }

    public int GetFinalCost(RewardItemView item)
    {
        if (item == null)
        {
            return 0;
        }

        int finalCost = item.BaseCost;
        int flatDiscount = 0;
        float percentDiscount = 0f;

        foreach (BonusDefinition bonus in activeBonuses)
        {
            if (bonus == null
                || bonus.EffectType != BonusEffectType.ShopCheaper
                || !bonus.MatchesShopItem(item.ShopDefinition))
            {
                continue;
            }

            flatDiscount += bonus.FlatValue;
            percentDiscount += bonus.PercentValue;
        }

        finalCost -= flatDiscount;
        finalCost = Mathf.RoundToInt(finalCost * Mathf.Max(0f, 1f - percentDiscount / 100f));

        return Mathf.Max(0, finalCost);
    }

    public int GetFinalAwarenessValue(RewardItemView item)
    {
        if (item == null)
        {
            return 0;
        }

        int finalAwarenessValue = item.BaseAwarenessValue;

        if (finalAwarenessValue <= 0)
        {
            return 0;
        }

        int flatBonus = 0;
        float percentBonus = 0f;

        foreach (BonusDefinition bonus in activeBonuses)
        {
            if (bonus == null
                || bonus.EffectType != BonusEffectType.ShopMoreAwareness)
            {
                continue;
            }

            flatBonus += bonus.FlatValue;
            percentBonus += bonus.PercentValue;
        }

        finalAwarenessValue += flatBonus;
        finalAwarenessValue = Mathf.RoundToInt(finalAwarenessValue * (1f + percentBonus / 100f));

        return Mathf.Max(0, finalAwarenessValue);
    }

    public int GetFinalTrashPollution(Trash trash)
    {
        if (trash == null)
        {
            return 0;
        }

        float reductionPercent = 0f;

        foreach (BonusDefinition bonus in activeBonuses)
        {
            if (bonus == null
                || bonus.EffectType != BonusEffectType.TrashLessPollution
                || !bonus.MatchesTrash(trash))
            {
                continue;
            }

            reductionPercent += bonus.PercentValue;
        }

        float multiplier = Mathf.Max(0f, 1f - reductionPercent / 100f);
        return Mathf.Max(0, Mathf.RoundToInt(trash.Score * multiplier));
    }

    public int GetFinalTrashIncome(Trash trash)
    {
        if (trash == null)
        {
            return 0;
        }

        float bonusPercent = 0f;

        foreach (BonusDefinition bonus in activeBonuses)
        {
            if (bonus == null
                || bonus.EffectType != BonusEffectType.TrashMoreGold
                || !bonus.MatchesTrash(trash))
            {
                continue;
            }

            bonusPercent += bonus.PercentValue;
        }

        return Mathf.Max(0, Mathf.RoundToInt(trash.Income * (1f + bonusPercent / 100f)));
    }

    private void TickTimedBonuses()
    {
        if (activeBonuses.Count == 0)
        {
            return;
        }

        for (int i = 0; i < activeBonuses.Count; i++)
        {
            BonusDefinition bonus = activeBonuses[i];

            if (bonus == null || !bonus.UsesTimer())
            {
                continue;
            }

            float interval = Mathf.Max(0.01f, bonus.IntervalSeconds);
            bonusTimers.TryGetValue(bonus, out float timer);
            timer += Time.deltaTime;

            if (timer < interval)
            {
                bonusTimers[bonus] = timer;
                continue;
            }

            bonusTimers[bonus] = 0f;
            TriggerTimedBonus(bonus);
        }
    }

    private void TriggerTimedBonus(BonusDefinition bonus)
    {
        if (bonus.EffectType == BonusEffectType.TrashAutoCollect)
        {
            AutoCollectRequested?.Invoke(bonus);
            return;
        }

        if (bonus.EffectType == BonusEffectType.ShopPassiveAwareness)
        {
            PassiveAwarenessRequested?.Invoke(Mathf.Max(0, bonus.FlatValue));
        }
    }
}
