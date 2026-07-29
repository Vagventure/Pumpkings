using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [Header("Goal")]
    [SerializeField] private TMP_Text goalBoxText;

    [Header("Music")]
    [SerializeField] private MusicStateDefinition startingMusicState;

    [Header("Progress Events")]
    [FormerlySerializedAs("tierDefinitions")]
    [SerializeField] private List<ProgressEventDefinition> awarenessEvents = new();
    [SerializeField] private List<ProgressEventDefinition> goldGatheredEvents = new();
    [SerializeField] private List<ProgressEventDefinition> threatProducedEvents = new();

    public IReadOnlyList<ProgressEventDefinition> AwarenessEvents => awarenessEvents;
    public IReadOnlyList<ProgressEventDefinition> GoldGatheredEvents => goldGatheredEvents;
    public IReadOnlyList<ProgressEventDefinition> ThreatProducedEvents => threatProducedEvents;
    public MusicStateDefinition StartingMusicState => startingMusicState;
    public string CurrentGoalText => goalBoxText == null ? string.Empty : goalBoxText.text;

    private void Awake()
    {
        SetupSingleton();
        ValidateAndSortProgressEvents();
    }

    private void OnEnable()
    {
        ProgressTracker.ProgressEventReached += HandleProgressEventReached;
    }

    private void OnDisable()
    {
        ProgressTracker.ProgressEventReached -= HandleProgressEventReached;
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

    private void ValidateAndSortProgressEvents()
    {
        ValidateProgressEvents(ref awarenessEvents);
        ValidateProgressEvents(ref goldGatheredEvents);
        ValidateProgressEvents(ref threatProducedEvents);

        SortProgressEvents(awarenessEvents);
        SortProgressEvents(goldGatheredEvents);
        SortProgressEvents(threatProducedEvents);
    }

    private void ValidateProgressEvents()
    {
        ValidateProgressEvents(ref awarenessEvents);
        ValidateProgressEvents(ref goldGatheredEvents);
        ValidateProgressEvents(ref threatProducedEvents);
    }

    private static void ValidateProgressEvents(ref List<ProgressEventDefinition> progressEvents)
    {
        if (progressEvents == null)
        {
            progressEvents = new List<ProgressEventDefinition>();
            return;
        }

        for (int i = 0; i < progressEvents.Count; i++)
        {
            progressEvents[i]?.Validate();
        }
    }

    private static void SortProgressEvents(List<ProgressEventDefinition> progressEvents)
    {
        progressEvents.Sort(CompareProgressEvents);
    }

    private static int CompareProgressEvents(ProgressEventDefinition left, ProgressEventDefinition right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.RequiredValue.CompareTo(right.RequiredValue);
    }

    private void HandleProgressEventReached(ProgressEventContext progressEvent)
    {
        ProgressEventDefinition definition = progressEvent.Definition;

        if (definition == null || goalBoxText == null)
        {
            return;
        }

        goalBoxText.text = definition.Goal;
    }

    private void OnValidate()
    {
        ValidateProgressEvents();
    }
}
