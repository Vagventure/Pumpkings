using System;
using System.Collections.Generic;
using UnityEngine;

public class ProgressTracker : MonoBehaviour
{
    public static event Action<ProgressEventContext> ProgressEventReached;

    public static ProgressTracker Instance { get; private set; }

    [Header("Runtime Totals")]
    [SerializeField] private int totalAwareness;
    [SerializeField] private int totalGoldGathered;
    [SerializeField] private int totalThreatProduced;

    private int nextAwarenessEventIndex;
    private int nextGoldGatheredEventIndex;
    private int nextThreatProducedEventIndex;

    public int TotalAwareness => totalAwareness;
    public int TotalGoldGathered => totalGoldGathered;
    public int TotalThreatProduced => totalThreatProduced;
    public int NextAwarenessThreshold => GetNextThreshold(ProgressMetric.Awareness);

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        ResetRuntimeState();
    }

    private void OnEnable()
    {
        AwarenessManager.AwarenessGained += AddAwareness;
        ScoringService.GoldGathered += AddGoldGathered;
        ScoringService.ThreatProduced += AddThreatProduced;
    }

    private void OnDisable()
    {
        AwarenessManager.AwarenessGained -= AddAwareness;
        ScoringService.GoldGathered -= AddGoldGathered;
        ScoringService.ThreatProduced -= AddThreatProduced;
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

    public void ResetRuntimeState()
    {
        totalAwareness = 0;
        totalGoldGathered = 0;
        totalThreatProduced = 0;
        nextAwarenessEventIndex = 0;
        nextGoldGatheredEventIndex = 0;
        nextThreatProducedEventIndex = 0;
    }

    public void AddAwareness(int amount)
    {
        AddProgress(ProgressMetric.Awareness, amount);
    }

    public void AddGoldGathered(int amount)
    {
        AddProgress(ProgressMetric.GoldGathered, amount);
    }

    public void AddThreatProduced(int amount)
    {
        AddProgress(ProgressMetric.ThreatProduced, amount);
    }

    public int GetNextThreshold(ProgressMetric metric)
    {
        IReadOnlyList<ProgressEventDefinition> events = GetProgressEvents(metric);
        int index = GetNextEventIndex(metric);

        while (index < events.Count)
        {
            ProgressEventDefinition progressEvent = events[index];

            if (progressEvent != null)
            {
                return progressEvent.RequiredValue;
            }

            index++;
        }

        if (events.Count == 0)
        {
            return 1;
        }

        ProgressEventDefinition lastEvent = events[events.Count - 1];
        return lastEvent == null ? 1 : Mathf.Max(1, lastEvent.RequiredValue);
    }

    private void AddProgress(ProgressMetric metric, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int total = GetTotal(metric) + amount;
        SetTotal(metric, total);
        PublishReachedEvents(metric, total);
    }

    private void PublishReachedEvents(ProgressMetric metric, int total)
    {
        IReadOnlyList<ProgressEventDefinition> events = GetProgressEvents(metric);
        int index = GetNextEventIndex(metric);

        while (index < events.Count)
        {
            ProgressEventDefinition progressEvent = events[index];
            index++;
            SetNextEventIndex(metric, index);

            if (progressEvent == null)
            {
                continue;
            }

            if (total < progressEvent.RequiredValue)
            {
                SetNextEventIndex(metric, index - 1);
                break;
            }

            Debug.Log($"ProgressTracker: {metric} progress event reached at {total}/{progressEvent.RequiredValue}.");
            ProgressEventReached?.Invoke(new ProgressEventContext(metric, total, progressEvent));
        }
    }

    private IReadOnlyList<ProgressEventDefinition> GetProgressEvents(ProgressMetric metric)
    {
        LevelController levelController = LevelController.Instance;

        if (levelController == null)
        {
            return Array.Empty<ProgressEventDefinition>();
        }

        return metric switch
        {
            ProgressMetric.Awareness => levelController.AwarenessEvents,
            ProgressMetric.GoldGathered => levelController.GoldGatheredEvents,
            ProgressMetric.ThreatProduced => levelController.ThreatProducedEvents,
            _ => Array.Empty<ProgressEventDefinition>()
        };
    }

    private int GetTotal(ProgressMetric metric)
    {
        return metric switch
        {
            ProgressMetric.Awareness => totalAwareness,
            ProgressMetric.GoldGathered => totalGoldGathered,
            ProgressMetric.ThreatProduced => totalThreatProduced,
            _ => 0
        };
    }

    private void SetTotal(ProgressMetric metric, int value)
    {
        value = Mathf.Max(0, value);

        switch (metric)
        {
            case ProgressMetric.Awareness:
                totalAwareness = value;
                break;
            case ProgressMetric.GoldGathered:
                totalGoldGathered = value;
                break;
            case ProgressMetric.ThreatProduced:
                totalThreatProduced = value;
                break;
        }
    }

    private int GetNextEventIndex(ProgressMetric metric)
    {
        return metric switch
        {
            ProgressMetric.Awareness => nextAwarenessEventIndex,
            ProgressMetric.GoldGathered => nextGoldGatheredEventIndex,
            ProgressMetric.ThreatProduced => nextThreatProducedEventIndex,
            _ => 0
        };
    }

    private void SetNextEventIndex(ProgressMetric metric, int value)
    {
        value = Mathf.Max(0, value);

        switch (metric)
        {
            case ProgressMetric.Awareness:
                nextAwarenessEventIndex = value;
                break;
            case ProgressMetric.GoldGathered:
                nextGoldGatheredEventIndex = value;
                break;
            case ProgressMetric.ThreatProduced:
                nextThreatProducedEventIndex = value;
                break;
        }
    }
}

public readonly struct ProgressEventContext
{
    public ProgressEventContext(ProgressMetric metric, int totalValue, ProgressEventDefinition definition)
    {
        Metric = metric;
        TotalValue = totalValue;
        Definition = definition;
    }

    public ProgressMetric Metric { get; }
    public int TotalValue { get; }
    public ProgressEventDefinition Definition { get; }
}
