using System;
using TMPro;
using UnityEngine;

public class AwarenessManager : MonoBehaviour
{
    public static event Action<int> OnAwarenessChanged;
    public static event Action<int> AwarenessGained;

    public static AwarenessManager Instance { get; private set; }

    [Header("Progress Bar GameObject")]
    [SerializeField] private GameObject awarenessBar;

    [Header("Awareness Score")]
    [SerializeField] private int currentAwarenessScore;
    [SerializeField] private TMP_Text currentAwarenessRepresentation;
    [SerializeField] private TMP_Text nextAwarenessTierRepresentation;

    private ProgressBarController awarenessProgressBar;

    public int CurrentAwarenessScore => currentAwarenessScore;
    public int CurrentAwarenessTargetPoints => GetCurrentAwarenessTargetPoints();
    public float AwarenessProgress01 => CurrentAwarenessTargetPoints <= 0 ? 0f : Mathf.Clamp01((float)currentAwarenessScore / CurrentAwarenessTargetPoints);

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        CacheProgressBar();
        currentAwarenessScore = Mathf.Max(0, currentAwarenessScore);

        UpdateAwarenessBar();
        UpdateAwarenessRepresentations();
    }

    private void OnEnable()
    {
        ScoringService.ItemPurchaseConfirmed += HandleItemPurchaseConfirmed;
        RewardManager.PassiveAwarenessRequested += HandlePassiveAwarenessRequested;
    }

    private void OnDisable()
    {
        ScoringService.ItemPurchaseConfirmed -= HandleItemPurchaseConfirmed;
        RewardManager.PassiveAwarenessRequested -= HandlePassiveAwarenessRequested;
    }

    private void Start()
    {
        OnAwarenessChanged?.Invoke(currentAwarenessScore);
        UpdateAwarenessRepresentations();
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

    private void CacheProgressBar()
    {
        if (awarenessBar != null)
        {
            awarenessProgressBar = awarenessBar.GetComponent<ProgressBarController>();
        }
    }

    private void HandleItemPurchaseConfirmed(RewardItemView item)
    {
        if (item == null)
        {
            return;
        }

        int awarenessValue = item.BaseAwarenessValue;

        RewardManager rewardManager = RewardManager.Instance;

        if (rewardManager != null)
        {
            awarenessValue = rewardManager.GetFinalAwarenessValue(item);
        }

        AddAwareness(awarenessValue);
    }

    private void AddAwareness(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentAwarenessScore += amount;

        OnAwarenessChanged?.Invoke(currentAwarenessScore);
        AwarenessGained?.Invoke(amount);

        UpdateAwarenessBar();
        UpdateAwarenessRepresentations();
    }

    private void HandlePassiveAwarenessRequested(int amount)
    {
        AddAwareness(amount);
    }

    private int GetCurrentAwarenessTargetPoints()
    {
        ProgressTracker progressTracker = ProgressTracker.Instance;

        if (progressTracker == null)
        {
            return 1;
        }

        return progressTracker.NextAwarenessThreshold;
    }

    private void UpdateAwarenessBar()
    {
        if (awarenessProgressBar == null)
        {
            return;
        }

        awarenessProgressBar.SetProgress(currentAwarenessScore, CurrentAwarenessTargetPoints);
    }

    private void UpdateAwarenessRepresentations()
    {
        if (currentAwarenessRepresentation != null)
        {
            currentAwarenessRepresentation.text = currentAwarenessScore.ToString();
        }

        if (nextAwarenessTierRepresentation != null)
        {
            nextAwarenessTierRepresentation.text = CurrentAwarenessTargetPoints.ToString();
        }
    }

    private void OnValidate()
    {
        currentAwarenessScore = Mathf.Max(0, currentAwarenessScore);
    }
}
