using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Serializable]
    public sealed class StageEntry
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CinemachineCamera camera;

        public GameObject Root => root;
        public CinemachineCamera Camera => camera;
    }

    public static event Action TransitionStarted;
    public static event Action TransitionCompleted;
    public static event Action SequenceCompleted;

    public static StageManager Instance { get; private set; }

    [Header("Cinemachine")]
    [SerializeField] private int activeCameraPriority = 10;
    [SerializeField] private int inactiveCameraPriority;

    [Header("Transition Zoom")]
    [SerializeField, Min(0f)] private float zoomOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float zoomInDuration = 0.35f;
    [SerializeField, Min(0f)] private float zoomFieldOfViewOffset = 20f;
    [SerializeField, Min(1f)] private float zoomOrthographicSizeMultiplier = 1.3f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Stages")]
    [SerializeField] private List<StageEntry> stages = new();
    [SerializeField, Min(0)] private int startingStageIndex;

    private int pendingStageIndex = -1;
    private bool pausedGameplayForTransition;
    private bool sequenceCompletedRaised;

    public int CurrentStageIndex { get; private set; }
    public int StageCount => stages == null ? 0 : stages.Count;
    public bool IsTransitioning { get; private set; }
    public bool HasNextStage => CurrentStageIndex >= 0 && CurrentStageIndex + 1 < StageCount;

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        InitializeStages();
    }

    private void OnEnable()
    {
        RewardManager.ProgressEventCompleted += HandleProgressEventCompleted;
    }

    private void OnDisable()
    {
        RewardManager.ProgressEventCompleted -= HandleProgressEventCompleted;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool GoToNextStage()
    {
        if (IsTransitioning || !HasNextStage)
        {
            return false;
        }

        int nextStageIndex = CurrentStageIndex + 1;

        if (!TryGetValidStage(CurrentStageIndex, out StageEntry currentStage)
            || !TryGetValidStage(nextStageIndex, out StageEntry nextStage))
        {
            return false;
        }

        PauseGameplayForTransition();
        IsTransitioning = true;
        pendingStageIndex = nextStageIndex;
        sequenceCompletedRaised = false;
        TransitionStarted?.Invoke();

        nextStage.Root.SetActive(true);

        StartCoroutine(RunZoomTransition(currentStage, nextStage));
        return true;
    }

    internal void InitializeStages()
    {
        int count = StageCount;

        if (count == 0)
        {
            CurrentStageIndex = -1;
            Debug.LogError($"StageManager: No stages are configured on '{name}'.");
            return;
        }

        CurrentStageIndex = Mathf.Clamp(startingStageIndex, 0, count - 1);
        pendingStageIndex = -1;
        IsTransitioning = false;
        sequenceCompletedRaised = false;

        for (int i = 0; i < count; i++)
        {
            StageEntry stage = stages[i];

            if (stage == null)
            {
                Debug.LogError($"StageManager: Stage {i} is missing on '{name}'.");
                continue;
            }

            bool isCurrent = i == CurrentStageIndex;

            if (stage.Root != null)
            {
                stage.Root.SetActive(isCurrent);
            }
            else
            {
                Debug.LogError($"StageManager: Stage {i} has no Root on '{name}'.");
            }

            if (stage.Camera != null)
            {
                stage.Camera.Priority = isCurrent
                    ? activeCameraPriority
                    : inactiveCameraPriority;
                stage.Camera.gameObject.SetActive(isCurrent);
            }
            else
            {
                Debug.LogError($"StageManager: Stage {i} has no Cinemachine Camera on '{name}'.");
            }
        }
    }

    internal void HandleProgressEventCompleted(ProgressEventContext context)
    {
        ProgressEventDefinition definition = context.Definition;

        if (definition != null
            && definition.CompletionEffect == ProgressEventCompletionEffect.GoToNextStage)
        {
            GoToNextStage();
        }
    }

    private bool SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"StageManager: More than one active manager exists. Disabling '{name}'.");
            enabled = false;
            return false;
        }

        Instance = this;
        return true;
    }

    private IEnumerator RunZoomTransition(StageEntry currentStage, StageEntry nextStage)
    {
        CameraZoomState currentCameraState = CaptureCameraState(currentStage.Camera);
        CameraZoomState nextCameraState = CaptureCameraState(nextStage.Camera);

        yield return AnimateZoom(currentCameraState, 0f, 1f, zoomOutDuration);

        currentStage.Camera.Priority = inactiveCameraPriority;
        nextStage.Camera.Priority = activeCameraPriority;
        ApplyZoom(currentCameraState, 0f);
        ApplyZoom(nextCameraState, 1f);
        currentStage.Root.SetActive(false);
        //currentStage.Camera.gameObject.SetActive(false);
        nextStage.Camera.gameObject.SetActive(true);

        yield return AnimateZoom(nextCameraState, 1f, 0f, zoomInDuration);

        ApplyZoom(currentCameraState, 0f);
        ApplyZoom(nextCameraState, 0f);
        CompleteTransition();
    }

    private IEnumerator AnimateZoom(
        CameraZoomState cameraState,
        float startAmount,
        float endAmount,
        float duration)
    {
        if (duration <= 0f)
        {
            ApplyZoom(cameraState, endAmount);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curvedTime = zoomCurve == null
                ? normalizedTime
                : zoomCurve.Evaluate(normalizedTime);
            ApplyZoom(cameraState, Mathf.Lerp(startAmount, endAmount, curvedTime));
            yield return null;
        }

        ApplyZoom(cameraState, endAmount);
    }

    private CameraZoomState CaptureCameraState(CinemachineCamera cinemachineCamera)
    {
        Camera unityCamera = cinemachineCamera.GetComponent<Camera>();
        var lens = cinemachineCamera.Lens;

        return new CameraZoomState(
            cinemachineCamera,
            unityCamera,
            lens.FieldOfView,
            lens.OrthographicSize,
            unityCamera != null ? unityCamera.fieldOfView : 0f,
            unityCamera != null ? unityCamera.orthographicSize : 0f);
    }

    private void ApplyZoom(CameraZoomState cameraState, float amount)
    {
        amount = Mathf.Clamp01(amount);

        var lens = cameraState.CinemachineCamera.Lens;
        lens.FieldOfView = Mathf.Clamp(
            cameraState.CinemachineFieldOfView + zoomFieldOfViewOffset * amount,
            1f,
            179f);
        lens.OrthographicSize = cameraState.CinemachineOrthographicSize
            * Mathf.Lerp(1f, zoomOrthographicSizeMultiplier, amount);
        cameraState.CinemachineCamera.Lens = lens;

        if (cameraState.UnityCamera == null)
        {
            return;
        }

        cameraState.UnityCamera.fieldOfView = Mathf.Clamp(
            cameraState.UnityFieldOfView + zoomFieldOfViewOffset * amount,
            1f,
            179f);
        cameraState.UnityCamera.orthographicSize = cameraState.UnityOrthographicSize
            * Mathf.Lerp(1f, zoomOrthographicSizeMultiplier, amount);
    }

    private void CompleteTransition()
    {
        if (!IsTransitioning)
        {
            return;
        }

        CurrentStageIndex = pendingStageIndex;
        pendingStageIndex = -1;
        IsTransitioning = false;

        ResumeGameplayOwnedByTransition();
        TransitionCompleted?.Invoke();

        if (!HasNextStage && !sequenceCompletedRaised)
        {
            sequenceCompletedRaised = true;
            SequenceCompleted?.Invoke();
        }
    }

    private bool TryGetValidStage(int index, out StageEntry stage)
    {
        stage = index >= 0 && index < StageCount ? stages[index] : null;

        if (stage == null || stage.Root == null || stage.Camera == null)
        {
            Debug.LogError($"StageManager: Stage {index} requires both a Root and Cinemachine Camera on '{name}'.");
            return false;
        }

        return true;
    }

    private void PauseGameplayForTransition()
    {
        GameManager gameManager = GameManager.Instance;
        pausedGameplayForTransition = gameManager != null && gameManager.IsGameplayActive;

        if (pausedGameplayForTransition)
        {
            gameManager.PauseGame();
        }
    }

    private void ResumeGameplayOwnedByTransition()
    {
        if (!pausedGameplayForTransition)
        {
            return;
        }

        pausedGameplayForTransition = false;
        GameManager.Instance?.ResumeGame();
    }

    private void OnValidate()
    {
        startingStageIndex = Mathf.Max(0, startingStageIndex);
        zoomOutDuration = Mathf.Max(0f, zoomOutDuration);
        zoomInDuration = Mathf.Max(0f, zoomInDuration);
        zoomFieldOfViewOffset = Mathf.Max(0f, zoomFieldOfViewOffset);
        zoomOrthographicSizeMultiplier = Mathf.Max(1f, zoomOrthographicSizeMultiplier);

        if (activeCameraPriority <= inactiveCameraPriority)
        {
            activeCameraPriority = inactiveCameraPriority + 1;
        }
    }

    private readonly struct CameraZoomState
    {
        public CameraZoomState(
            CinemachineCamera cinemachineCamera,
            Camera unityCamera,
            float cinemachineFieldOfView,
            float cinemachineOrthographicSize,
            float unityFieldOfView,
            float unityOrthographicSize)
        {
            CinemachineCamera = cinemachineCamera;
            UnityCamera = unityCamera;
            CinemachineFieldOfView = cinemachineFieldOfView;
            CinemachineOrthographicSize = cinemachineOrthographicSize;
            UnityFieldOfView = unityFieldOfView;
            UnityOrthographicSize = unityOrthographicSize;
        }

        public CinemachineCamera CinemachineCamera { get; }
        public Camera UnityCamera { get; }
        public float CinemachineFieldOfView { get; }
        public float CinemachineOrthographicSize { get; }
        public float UnityFieldOfView { get; }
        public float UnityOrthographicSize { get; }
    }
}
