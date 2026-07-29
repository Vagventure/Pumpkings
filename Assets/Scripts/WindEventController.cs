using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindEventController : MonoBehaviour
{
    private sealed class TrashMotion
    {
        public Trash trash;
        public Vector3 startPosition;
        public Vector3 targetPosition;
        public Quaternion startLocalRotation;
        public float rotationDegrees;
        public float joinedProgress;
    }

    [Header("Scene References")]
    [SerializeField] private SpawnService spawnService;
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private string gustTriggerName = "PlayWind";
    [SerializeField] private SpawnTrigger spawnTrigger = SpawnTrigger.WindSpawnTrigger;

    [Header("Schedule")]
    [SerializeField, Min(0f)] private float firstGustDelay = 5f;
    [SerializeField] private Vector2 repeatGustDelayRange = new Vector2(15f, 30f);

    [Header("Trash Movement")]
    [SerializeField, Range(0f, 1f)] private float movementDistanceFraction = 0.15f;
    [SerializeField] private Vector2 movementDistanceMultiplierRange = new Vector2(0.8f, 1.2f);
    [SerializeField, Range(0f, 45f)] private float movementDeviationDegrees = 12f;
    [SerializeField] private Vector2 rotationDegreesRange = new Vector2(30f, 120f);

    private readonly HashSet<Trash> activeMovableTrash = new HashSet<Trash>();
    private readonly Dictionary<Trash, TrashMotion> activeMotions = new Dictionary<Trash, TrashMotion>();
    private readonly List<Trash> trashBuffer = new List<Trash>();
    private Coroutine scheduledGust;
    private WindDirection currentDirection;
    private bool hasPreviousDirection;
    private bool gustActive;
    private float gustElapsed;
    private float gustDuration = 1f;
    private int gustTriggerHash;
    private bool animatorPaused;
    private float animatorSpeedBeforePause = 1f;

    public bool IsGustActive => gustActive;
    public WindDirection CurrentDirection => currentDirection;
    public bool IsGustScheduled => scheduledGust != null;
    public float LastScheduledDelay { get; private set; }

    private void Awake()
    {
        gustTriggerHash = string.IsNullOrWhiteSpace(gustTriggerName)
            ? 0
            : Animator.StringToHash(gustTriggerName);

    }

    private void OnEnable()
    {
        RewardManager.ProgressEventCompleted += HandleProgressEventCompleted;
        SpawnService.TrashAdded += HandleTrashAdded;
        SpawnService.TrashRemoved += HandleTrashRemoved;
        GameManager.GameStateChanged += HandleGameStateChanged;

        if (spawnService != null)
        {
            spawnService.GetActiveTrash(trashBuffer);

            for (int i = 0; i < trashBuffer.Count; i++)
            {
                HandleTrashAdded(trashBuffer[i]);
            }
        }
    }

    private void OnDisable()
    {
        RewardManager.ProgressEventCompleted -= HandleProgressEventCompleted;
        SpawnService.TrashAdded -= HandleTrashAdded;
        SpawnService.TrashRemoved -= HandleTrashRemoved;
        GameManager.GameStateChanged -= HandleGameStateChanged;

        StopAllCoroutines();
        scheduledGust = null;
        gustActive = false;
        activeMotions.Clear();
        activeMovableTrash.Clear();
        RestoreAnimatorSpeed();
    }

    private void Update()
    {
        bool gameplayActive = IsGameplayActive();
        UpdateAnimatorPause(gameplayActive);

        if (!gameplayActive || !gustActive)
        {
            return;
        }

        gustElapsed = Mathf.Min(gustDuration, gustElapsed + Time.deltaTime);
        ApplyMotion(gustElapsed / gustDuration);
    }

    public void ActivateWind()
    {
        ScheduleNextGust(firstGustDelay);
    }

    public void WindSpawnAndMovementEvent(float movementDuration)
    {
        if (spawnService == null)
        {
            Debug.LogError($"WindEventController: Spawn Service is missing on '{name}'.");
            return;
        }

        spawnService.BeginSpawnEvent(spawnTrigger);
        gustActive = true;
        gustElapsed = 0f;
        gustDuration = Mathf.Max(0.01f, movementDuration);
        activeMotions.Clear();

        trashBuffer.Clear();
        trashBuffer.AddRange(activeMovableTrash);

        for (int i = 0; i < trashBuffer.Count; i++)
        {
            AddMotion(trashBuffer[i], 0f);
        }

        SpawnTriggerEvents.Raise(spawnTrigger, currentDirection, gustDuration);
    }

    public void WindEndEvent()
    {
        if (gustActive)
        {
            ApplyMotion(1f);
        }

        gustActive = false;
        activeMotions.Clear();
        Vector2 delayRange = NormalizePositiveRange(repeatGustDelayRange, 0f);
        ScheduleNextGust(Random.Range(delayRange.x, delayRange.y));
    }

    internal void HandleProgressEventCompleted(ProgressEventContext context)
    {
        ProgressEventDefinition definition = context.Definition;

        if (definition != null
            && definition.CompletionEffect == ProgressEventCompletionEffect.StartWind)
        {
            ActivateWind();
        }
    }

    private void HandleTrashAdded(Trash trash)
    {
        if (trash == null || !trash.IsMovable)
        {
            return;
        }

        activeMovableTrash.Add(trash);

        if (gustActive)
        {
            AddMotion(trash, Mathf.Clamp01(gustElapsed / gustDuration));
        }
    }

    private void HandleTrashRemoved(Trash trash)
    {
        if (trash == null)
        {
            return;
        }

        activeMovableTrash.Remove(trash);
        activeMotions.Remove(trash);
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Lost)
        {
            return;
        }

        StopAllCoroutines();
        scheduledGust = null;
        gustActive = false;
        activeMotions.Clear();
    }

    private void ScheduleNextGust(float delay)
    {
        LastScheduledDelay = Mathf.Max(0f, delay);
        scheduledGust = StartCoroutine(PlayGustAfterDelay(LastScheduledDelay));
    }

    private IEnumerator PlayGustAfterDelay(float delay)
    {
        float remaining = delay;

        while (remaining > 0f)
        {
            yield return null;

            if (IsGameplayActive())
            {
                remaining -= Time.deltaTime;
            }
        }

        scheduledGust = null;
        PlayGust();
    }

    private void PlayGust()
    {
        if (animator == null || gustTriggerHash == 0)
        {
            Debug.LogError($"WindEventController: Animator or Gust Trigger Name is missing on '{name}'.");
            return;
        }

        currentDirection = hasPreviousDirection
            ? WindGustMath.SelectDifferentDirection(currentDirection, Random.Range(0, 3))
            : (WindDirection)Random.Range(0, 4);
        hasPreviousDirection = true;
        animator.SetTrigger(gustTriggerHash);
    }

    private void AddMotion(Trash trash, float joinedProgress)
    {
        if (trash == null
            || !trash.gameObject.activeSelf
            || !trash.IsMovable
            || trash.IsBeingCollected
            || spawnService == null
            || !spawnService.TryGetSpawnArea(trash, out Transform spawnArea))
        {
            return;
        }

        BoxCollider boxCollider = spawnArea.GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            Debug.LogError($"WindEventController: Spawn Area '{spawnArea.name}' requires a BoxCollider.");
            return;
        }

        float axisSpan = currentDirection == WindDirection.PositiveX
            || currentDirection == WindDirection.NegativeX
            ? boxCollider.size.x
            : boxCollider.size.z;
        Vector2 multiplierRange = NormalizePositiveRange(movementDistanceMultiplierRange, 0f);
        float multiplier = Random.Range(multiplierRange.x, multiplierRange.y);
        float deviation = Random.Range(-movementDeviationDegrees, movementDeviationDegrees);
        float remainingFraction = WindGustMath.GetRemainingMovementFraction(joinedProgress);
        Vector3 localDisplacement = WindGustMath.GetLocalDisplacement(
            currentDirection,
            axisSpan,
            movementDistanceFraction,
            multiplier,
            deviation) * remainingFraction;
        Vector3 targetPosition = spawnArea.TransformPoint(
            spawnArea.InverseTransformPoint(trash.transform.position) + localDisplacement);
        Vector2 rotationRange = NormalizePositiveRange(rotationDegreesRange, 0f);
        float rotationSign = Random.value < 0.5f ? -1f : 1f;

        activeMotions[trash] = new TrashMotion
        {
            trash = trash,
            startPosition = trash.transform.position,
            targetPosition = SpawnAreaSampler.ClampPointXZ(spawnArea, targetPosition),
            startLocalRotation = trash.transform.localRotation,
            rotationDegrees = Random.Range(rotationRange.x, rotationRange.y)
                * rotationSign
                * remainingFraction,
            joinedProgress = joinedProgress
        };
    }

    private void ApplyMotion(float gustProgress)
    {
        trashBuffer.Clear();
        trashBuffer.AddRange(activeMotions.Keys);

        for (int i = 0; i < trashBuffer.Count; i++)
        {
            Trash trash = trashBuffer[i];

            if (trash == null || !trash.gameObject.activeSelf)
            {
                activeMotions.Remove(trash);
                continue;
            }

            if (trash.IsBeingCollected)
            {
                continue;
            }

            TrashMotion motion = activeMotions[trash];
            float localProgress = Mathf.InverseLerp(motion.joinedProgress, 1f, gustProgress);
            float easedProgress = Mathf.SmoothStep(0f, 1f, localProgress);
            trash.transform.position = Vector3.LerpUnclamped(
                motion.startPosition,
                motion.targetPosition,
                easedProgress);
            trash.transform.localRotation = motion.startLocalRotation
                * Quaternion.Euler(0f, 0f, motion.rotationDegrees * easedProgress);
        }
    }

    private void UpdateAnimatorPause(bool gameplayActive)
    {
        if (animator == null)
        {
            return;
        }

        if (!gameplayActive && !animatorPaused)
        {
            animatorSpeedBeforePause = animator.speed;
            animator.speed = 0f;
            animatorPaused = true;
            return;
        }

        if (gameplayActive && animatorPaused)
        {
            RestoreAnimatorSpeed();
        }
    }

    private void RestoreAnimatorSpeed()
    {
        if (animator != null && animatorPaused)
        {
            animator.speed = animatorSpeedBeforePause;
        }

        animatorPaused = false;
    }

    private void OnValidate()
    {
        firstGustDelay = Mathf.Max(0f, firstGustDelay);
        repeatGustDelayRange = NormalizePositiveRange(repeatGustDelayRange, 0f);
        movementDistanceFraction = Mathf.Clamp01(movementDistanceFraction);
        movementDistanceMultiplierRange = NormalizePositiveRange(movementDistanceMultiplierRange, 0f);
        movementDeviationDegrees = Mathf.Clamp(movementDeviationDegrees, 0f, 45f);
        rotationDegreesRange = NormalizePositiveRange(rotationDegreesRange, 0f);
    }

    private static Vector2 NormalizePositiveRange(Vector2 range, float minimumAllowed)
    {
        float minimum = Mathf.Max(minimumAllowed, Mathf.Min(range.x, range.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
        return new Vector2(minimum, maximum);
    }

    private static bool IsGameplayActive()
    {
        return GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
    }
}
