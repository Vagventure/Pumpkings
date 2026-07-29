using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoringService : MonoBehaviour
{
    public static event Action<int> OnScoreChanged;
    public static event Action<int, int> OnPollutionChanged;
    public static event Action<int> OnBudgetChanged;
    public static event Action<RewardItemView> ItemPurchaseConfirmed;
    public static event Action OnShopItemPurchasedSFX;
    public static event Action OnShopItemPurchaseFailedSFX;
    public static event Action<int> GoldGathered;
    public static event Action<Trash, int> TrashIncomeAwarded;
    public static event Action<int> ThreatProduced;

    public static ScoringService Instance { get; private set; }

    [Header("Progress Bar GameObjects")]
    [SerializeField] private GameObject pollutionBar;

    [Header("UI")]
    [SerializeField] private TMP_Text budgetRepresentation;
    [SerializeField] private TMP_Text currentThreatRepresentation;
    [SerializeField] private TMP_Text maxThreatRepresentation;

    [Header("Budget")]
    [SerializeField] private int budget;

    [Header("Pollution Score")]
    [SerializeField] private int maxScore = 100;
    [SerializeField] private int currentScore;

    private ProgressBarController pollutionProgressBar;
    private readonly Dictionary<Trash, int> pollutionByTrash = new();

    public int CurrentScore => currentScore;
    public int CurrentPollution => currentScore;
    public int MaxScore => maxScore;
    public int MaxPollution => maxScore;
    public int Budget => budget;

    public float PollutionProgress01 => maxScore <= 0 ? 0f : (float)currentScore / maxScore;

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        CacheProgressBars();

        maxScore = Mathf.Max(1, maxScore);
        currentScore = Mathf.Clamp(currentScore, 0, maxScore);

        PublishPollutionChanged();
        UpdateBudgetRepresentation();
    }

    private void OnEnable()
    {
        SpawnService.TrashAdded += RegisterSpawnedTrash;
        SpawnService.TrashRemovedWithSource += ApplyTrashRemoval;

        RewardItemView.Clicked += HandleRewardItemClicked;
    }

    private void OnDisable()
    {
        SpawnService.TrashAdded -= RegisterSpawnedTrash;
        SpawnService.TrashRemovedWithSource -= ApplyTrashRemoval;

        RewardItemView.Clicked -= HandleRewardItemClicked;
    }

    private void Start()
    {
        PublishPollutionChanged();
        OnBudgetChanged?.Invoke(budget);
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

    public void SetMaxPollution(int maxPollution)
    {
        maxScore = Mathf.Max(1, maxPollution);
        currentScore = Mathf.Clamp(currentScore, 0, maxScore);

        PublishPollutionChanged();
    }

    private void CacheProgressBars()
    {
        if (pollutionBar != null)
        {
            pollutionProgressBar = pollutionBar.GetComponent<ProgressBarController>();
        }
    }

    public void RegisterSpawnedTrash(Trash trash)
    {
        if (trash == null)
        {
            return;
        }

        int pollutionValue = trash.Score;

        RewardManager rewardManager = RewardManager.Instance;

        if (rewardManager != null)
        {
            pollutionValue = rewardManager.GetFinalTrashPollution(trash);
        }

        pollutionByTrash[trash] = pollutionValue;

        currentScore += pollutionValue;
        currentScore = Mathf.Clamp(currentScore, 0, maxScore);

        ThreatProduced?.Invoke(pollutionValue);
        PublishPollutionChanged();
    }

    public void ApplyTrashRemoval(Trash trash, TrashRemovalSource removalSource)
    {
        if (trash == null)
        {
            return;
        }

        int pollutionValue = trash.Score;

        if (pollutionByTrash.TryGetValue(trash, out int registeredPollutionValue))
        {
            pollutionValue = registeredPollutionValue;
            pollutionByTrash.Remove(trash);
        }

        currentScore -= pollutionValue;
        currentScore = Mathf.Clamp(currentScore, 0, maxScore);

        PublishPollutionChanged();

        if (removalSource == TrashRemovalSource.RecyclingPatrol)
        {
            return;
        }

        int incomeValue = trash.Income;

        RewardManager rewardManager = RewardManager.Instance;

        if (rewardManager != null)
        {
            incomeValue = rewardManager.GetFinalTrashIncome(trash);
        }

        budget += incomeValue;

        OnBudgetChanged?.Invoke(budget);
        GoldGathered?.Invoke(incomeValue);
        TrashIncomeAwarded?.Invoke(trash, incomeValue);
        UpdateBudgetRepresentation();
    }

    private void HandleRewardItemClicked(RewardItemView item)
    {
        if (item == null || item.ShopDefinition == null)
        {
            item?.PlayRejectedFeedback();
            OnShopItemPurchaseFailedSFX?.Invoke();
            return;
        }

        GameManager gameManager = GameManager.Instance;

        if (gameManager != null && !gameManager.IsGameplayActive)
        {
            return;
        }

        if (!RecyclingPatrolService.IsPurchaseAvailable(item.ShopDefinition, true))
        {
            item.PlayRejectedFeedback();
            OnShopItemPurchaseFailedSFX?.Invoke();
            return;
        }

        int finalCost = item.BaseCost;

        RewardManager rewardManager = RewardManager.Instance;

        if (rewardManager != null)
        {
            finalCost = rewardManager.GetFinalCost(item);
        }

        if (budget < finalCost)
        {
            Debug.Log("Not enough budget.");
            item.PlayRejectedFeedback();
            OnShopItemPurchaseFailedSFX?.Invoke();
            return;
        }

        budget -= finalCost;

        OnBudgetChanged?.Invoke(budget);

        UpdateBudgetRepresentation();

        item.PlayAcceptedFeedback();
        ItemPurchaseConfirmed?.Invoke(item);
        OnShopItemPurchasedSFX?.Invoke();
    }

    private void PublishPollutionChanged()
    {
        OnScoreChanged?.Invoke(currentScore);
        OnPollutionChanged?.Invoke(currentScore, maxScore);

        UpdatePollutionBar();
        UpdateThreatRepresentations();
    }

    private void UpdatePollutionBar()
    {
        if (pollutionProgressBar == null)
        {
            return;
        }

        pollutionProgressBar.SetProgress(currentScore, maxScore);
    }

    private void UpdateBudgetRepresentation()
    {
        if (budgetRepresentation == null)
        {
            return;
        }

        budgetRepresentation.text = budget.ToString();
        RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, budgetRepresentation);
    }

    private void UpdateThreatRepresentations()
    {
        if (currentThreatRepresentation != null)
        {
            currentThreatRepresentation.text = currentScore.ToString();
            RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, currentThreatRepresentation);
        }

        if (maxThreatRepresentation != null)
        {
            maxThreatRepresentation.text = maxScore.ToString();
            RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, maxThreatRepresentation);
        }
    }
}
