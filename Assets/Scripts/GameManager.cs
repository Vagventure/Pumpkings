using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Running,
    Lost
}

public class GameManager : MonoBehaviour
{
    public static event Action<GameState> GameStateChanged;
    public static event Action GameLost;

    public static GameManager Instance { get; private set; }

    [Header("Pollution Limit")]
    [SerializeField] private int maxPollution = 100;

    [Header("Gameplay Control")]
    [SerializeField] private Behaviour[] disableOnLose;
    [SerializeField] private Behaviour[] pauseOnGamePause;

    [Header("State Objects")]
    [SerializeField] private GameObject[] hideOnLose;
    [SerializeField] private GameObject[] showOnLose;
    [SerializeField] private bool applyInitialStateVisibilityOnAwake = true;

    private GameState currentState = GameState.Running;
    private bool isPaused;
    private readonly Dictionary<Behaviour, bool> pauseBehaviourStates = new();

    public GameState CurrentState => currentState;
    public int MaxPollution => maxPollution;
    public bool IsPaused => isPaused;
    public bool IsGameplayActive => currentState == GameState.Running && !isPaused;

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        maxPollution = Mathf.Max(1, maxPollution);

        if (applyInitialStateVisibilityOnAwake)
        {
            ApplyRunningStateVisibility();
        }

        PushPollutionLimitToScoring();
    }

    private void OnEnable()
    {
        ScoringService.OnPollutionChanged += HandlePollutionChanged;

        PushPollutionLimitToScoring();
    }

    private void OnDisable()
    {
        ScoringService.OnPollutionChanged -= HandlePollutionChanged;
    }

    private void Start()
    {
        PushPollutionLimitToScoring();

        ScoringService scoringService = ScoringService.Instance;

        if (scoringService != null)
        {
            HandlePollutionChanged(scoringService.CurrentPollution, scoringService.MaxPollution);
        }
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

    private void HandlePollutionChanged(int currentPollution, int _)
    {
        if (currentState != GameState.Running)
        {
            return;
        }

        if (currentPollution < maxPollution)
        {
            return;
        }

        EnterLostState();
    }

    private void EnterLostState()
    {
        currentState = GameState.Lost;
        isPaused = false;
        pauseBehaviourStates.Clear();

        DisableConfiguredGameplay();
        ApplyLostStateVisibility();

        GameStateChanged?.Invoke(currentState);
        GameLost?.Invoke();
    }

    public void PauseGame()
    {
        if (currentState != GameState.Running || isPaused)
        {
            return;
        }

        isPaused = true;
        SetPausedBehaviours(false);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Running || !isPaused)
        {
            return;
        }

        isPaused = false;
        RestorePausedBehaviours();
    }

    private void PushPollutionLimitToScoring()
    {
        ScoringService scoringService = ScoringService.Instance;

        if (scoringService == null)
        {
            return;
        }

        scoringService.SetMaxPollution(maxPollution);
    }

    private void DisableConfiguredGameplay()
    {
        SetBehavioursEnabled(disableOnLose, false);
        SetBehavioursEnabled(pauseOnGamePause, false);
    }

    private void ApplyRunningStateVisibility()
    {
        SetObjectsActive(hideOnLose, true);
        SetObjectsActive(showOnLose, false);
    }

    private void ApplyLostStateVisibility()
    {
        SetObjectsActive(hideOnLose, false);
        SetObjectsActive(showOnLose, true);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject target = objects[i];

            if (target == null)
            {
                continue;
            }

            target.SetActive(active);
            RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, target);
        }
    }

    private void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
        {
            return;
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = enabled;
        }
    }

    private void SetPausedBehaviours(bool enabled)
    {
        pauseBehaviourStates.Clear();

        if (pauseOnGamePause == null)
        {
            return;
        }

        for (int i = 0; i < pauseOnGamePause.Length; i++)
        {
            Behaviour behaviour = pauseOnGamePause[i];

            if (behaviour == null)
            {
                continue;
            }

            pauseBehaviourStates[behaviour] = behaviour.enabled;
            behaviour.enabled = enabled;
        }
    }

    private void RestorePausedBehaviours()
    {
        foreach (KeyValuePair<Behaviour, bool> pausedBehaviourState in pauseBehaviourStates)
        {
            Behaviour behaviour = pausedBehaviourState.Key;

            if (behaviour == null)
            {
                continue;
            }

            behaviour.enabled = pausedBehaviourState.Value;
        }

        pauseBehaviourStates.Clear();
    }
}
