using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnService : MonoBehaviour
{
    public static event Action<Trash> TrashAdded;
    public static event Action<Trash> TrashRemoved;
    public static event Action<Trash, TrashRemovalSource> TrashRemovedWithSource;

    [Serializable]
    private class TrashTypeSpawnConfig
    {
        public SpawnData data;
        public Transform spawnArea;
        public TrashPath[] paths;
        public SpawnMode spawnMode = SpawnMode.TimedSpawn;
        public SpawnTrigger spawnTrigger = SpawnTrigger.WaveSpawnTrigger;
        public EventSpawnPattern eventSpawnPattern = EventSpawnPattern.Instant;
        public int eventSpawnCount = 1;
        public Vector2 directionalBurstDurationRange = new Vector2(3f, 4f);
        public Vector2 directionalEdgeInsetRange = new Vector2(0.05f, 0.1f);
    }

    private class SpawnRuntimeState
    {
        public SpawnData data;
        public Transform spawnArea;
        public TrashPath[] paths;
        public readonly List<Trash> pool = new List<Trash>();
        public int activeCount;
        public float nextSpawnTime;
        public int inspectorOrder;
        public SpawnMode spawnMode;
        public SpawnTrigger spawnTrigger;
        public EventSpawnPattern eventSpawnPattern;
        public int eventSpawnCount;
        public int remainingEventSpawnCount;
        public Vector2 directionalBurstDurationRange;
        public Vector2 directionalEdgeInsetRange;
        public bool missingSpawnAreaLogged;
        public bool missingPathFollowerLogged;
        public bool missingDirectionLogged;
    }

    [Header("Pool")]
    [SerializeField] private TrashTypeSpawnConfig[] trashTypes;
    [SerializeField] private Transform poolParent;
    [SerializeField] private int spawnLimit = 20;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnTickInterval = 0.5f;
    [SerializeField] private float spawnTickRandomVariation = 0.1f;

    private readonly HashSet<Trash> activeTrash = new HashSet<Trash>();
    private readonly HashSet<TrashType> blockedTrashTypes = new HashSet<TrashType>();
    private readonly Dictionary<TrashType, Vector3> nextSpawnNormalizedPositions = new Dictionary<TrashType, Vector3>();
    private readonly Dictionary<Trash, SpawnRuntimeState> activeTrashStates = new Dictionary<Trash, SpawnRuntimeState>();
    private readonly List<SpawnRuntimeState> spawnStates = new List<SpawnRuntimeState>();
    private Coroutine spawnCoroutine;

    public int SpawnLimit => spawnLimit;
    public int ActiveTrashCount => activeTrash.Count;

    public void SetSpawnBlocked(TrashType trashType, bool blocked)
    {
        if (blocked)
        {
            blockedTrashTypes.Add(trashType);
            return;
        }

        blockedTrashTypes.Remove(trashType);
    }

    public bool IsSpawnBlocked(TrashType trashType)
    {
        return blockedTrashTypes.Contains(trashType);
    }

    public void SetNextSpawnNormalizedPosition(TrashType trashType, Vector3 normalizedPosition)
    {
        nextSpawnNormalizedPositions[trashType] = new Vector3(
            Mathf.Clamp01(normalizedPosition.x),
            Mathf.Clamp01(normalizedPosition.y),
            Mathf.Clamp01(normalizedPosition.z));
    }

    public void ClearNextSpawnNormalizedPosition(TrashType trashType)
    {
        nextSpawnNormalizedPositions.Remove(trashType);
    }

    public int TrashPrefabScore
    {
        get
        {
            if (trashTypes == null || trashTypes.Length == 0)
            {
                return 0;
            }

            if (trashTypes[0] == null || trashTypes[0].data == null || trashTypes[0].data.Prefab == null)
            {
                return 0;
            }

            return trashTypes[0].data.Prefab.Score;
        }
    }

    public int MaxPollutionScore
    {
        get
        {
            int total = 0;

            if (trashTypes == null)
            {
                return total;
            }

            for (int i = 0; i < trashTypes.Length; i++)
            {
                TrashTypeSpawnConfig config = trashTypes[i];
                SpawnData data = config != null ? config.data : null;

                if (data != null && data.Prefab != null)
                {
                    total += data.SpawnLimit * data.Prefab.Score;
                }
            }

            return total;
        }
    }

    private void Awake()
    {
        if (poolParent == null)
        {
            poolParent = transform;
        }
        CreatePool();
    }

    private void OnEnable()
    {
        MousePickUpController.OnTrashClicked += DespawnTrash;
        RewardManager.AutoCollectRequested += HandleAutoCollectRequested;
        SpawnTriggerEvents.Triggered += HandleSpawnTriggered;

        StartSpawnLoop();
    }

    private void OnDisable()
    {
        MousePickUpController.OnTrashClicked -= DespawnTrash;
        RewardManager.AutoCollectRequested -= HandleAutoCollectRequested;
        SpawnTriggerEvents.Triggered -= HandleSpawnTriggered;

        StopSpawnLoop();
    }

    private void StartSpawnLoop()
    {
        if (spawnCoroutine != null)
        {
            return;
        }

        for (int i = 0; i < spawnStates.Count; i++)
        {
            SpawnRuntimeState state = spawnStates[i];

            if (state == null)
            {
                continue;
            }

            state.nextSpawnTime = Time.time + GetSpawnInterval(state);
        }

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void StopSpawnLoop()
    {
        if (spawnCoroutine == null)
        {
            return;
        }

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    private void CreatePool()
    {
        if (trashTypes == null)
        {
            return;
        }

        spawnStates.Clear();

        for (int t = 0; t < trashTypes.Length; t++)
        {
            TrashTypeSpawnConfig config = trashTypes[t];

            if (config == null || config.data == null)
            {
                continue;
            }

            SpawnData data = config.data;

            if (data.Prefab == null)
            {
                Debug.LogError($"SpawnService: Trash Prefab is missing for trashTypes[{t}].");
                continue;
            }

            SpawnRuntimeState state = new SpawnRuntimeState
            {
                data = data,
                spawnArea = config.spawnArea,
                paths = config.paths,
                spawnMode = config.spawnMode,
                spawnTrigger = config.spawnTrigger,
                eventSpawnPattern = config.eventSpawnPattern,
                eventSpawnCount = Mathf.Max(1, config.eventSpawnCount),
                remainingEventSpawnCount = Mathf.Max(1, config.eventSpawnCount),
                directionalBurstDurationRange = NormalizePositiveRange(config.directionalBurstDurationRange),
                directionalEdgeInsetRange = NormalizeInsetRange(config.directionalEdgeInsetRange),
                inspectorOrder = t
            };

            if (data.Sprites == null || data.Sprites.Count == 0)
            {
                Debug.LogError($"No sprites assigned for {data.Prefab.name}");
                continue;
            }


            for (int i = 0; i < data.SpawnLimit; i++)
            {
                Trash trash = Instantiate(data.Prefab, poolParent);
                trash.GetComponent<SpriteRenderer>().sprite = data.Sprites[UnityEngine.Random.Range(0, data.Sprites.Count)];
                trash.gameObject.SetActive(false);
                state.pool.Add(trash);
            }

            spawnStates.Add(state);
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetSpawnTickDelay());

            SpawnRuntimeState state = GetNextReadySpawnState();

            if (state == null)
            {
                continue;
            }

            TrySpawnTrash(state);
            state.nextSpawnTime = Time.time + GetSpawnInterval(state);
        }
    }

    private float GetSpawnTickDelay()
    {
        float minDelay = Mathf.Max(0.01f, spawnTickInterval - spawnTickRandomVariation);
        float maxDelay = Mathf.Max(minDelay, spawnTickInterval + spawnTickRandomVariation);

        return UnityEngine.Random.Range(minDelay, maxDelay);
    }

    private SpawnRuntimeState GetNextReadySpawnState()
    {
        float now = Time.time;
        spawnStates.Sort(CompareSpawnStates);

        for (int i = 0; i < spawnStates.Count; i++)
        {
            SpawnRuntimeState state = spawnStates[i];

            if (state == null)
            {
                continue;
            }

            if (state.spawnMode != SpawnMode.TimedSpawn)
            {
                continue;
            }

            if (state.nextSpawnTime <= now)
            {
                return state;
            }
        }

        return null;
    }

    private static int CompareSpawnStates(SpawnRuntimeState left, SpawnRuntimeState right)
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

        int timeComparison = left.nextSpawnTime.CompareTo(right.nextSpawnTime);

        if (timeComparison != 0)
        {
            return timeComparison;
        }

        return left.inspectorOrder.CompareTo(right.inspectorOrder);
    }

    private float GetSpawnInterval(SpawnRuntimeState state)
    {
        if (state == null || state.data == null)
        {
            return 1f;
        }

        float baseInterval = Mathf.Max(0.01f, state.data.SpawnInterval);

        return baseInterval;
    }

    private void HandleSpawnTriggered(SpawnTriggerContext context)
    {
        for (int i = 0; i < spawnStates.Count; i++)
        {
            SpawnRuntimeState state = spawnStates[i];

            if (state == null
                || state.spawnMode != SpawnMode.EventSpawn
                || state.spawnTrigger != context.Trigger)
            {
                continue;
            }

            if (state.eventSpawnPattern == EventSpawnPattern.DirectionalBurst)
            {
                if (!context.HasDirection)
                {
                    if (!state.missingDirectionLogged)
                    {
                        Debug.LogError($"SpawnService: Directional burst for {state.data.name} requires a wind direction.");
                        state.missingDirectionLogged = true;
                    }

                    continue;
                }

                if (state.remainingEventSpawnCount <= 0)
                {
                    continue;
                }

                int directionalBurstCount = UnityEngine.Random.Range(
                    1,
                    state.remainingEventSpawnCount + 1);

                StartCoroutine(SpawnDirectionalBurst(
                    state,
                    context.Direction,
                    context.HasDuration ? context.Duration : float.PositiveInfinity,
                    directionalBurstCount));
                continue;
            }

            int spawnCount = Mathf.Max(1, state.eventSpawnCount);

            for (int s = 0; s < spawnCount; s++)
            {
                if (!TrySpawnTrash(state))
                {
                    break;
                }
            }
        }
    }

    private IEnumerator SpawnDirectionalBurst(
        SpawnRuntimeState state,
        WindDirection direction,
        float availableDuration,
        int spawnCount)
    {
        spawnCount = Mathf.Clamp(spawnCount, 1, state.remainingEventSpawnCount);
        Vector2 durationRange = NormalizePositiveRange(state.directionalBurstDurationRange);
        float duration = Mathf.Min(
            UnityEngine.Random.Range(durationRange.x, durationRange.y),
            availableDuration);
        float elapsed = 0f;

        while (!IsGameplayActive())
        {
            yield return null;
        }

        TrySpawnDirectionalTrash(state, direction);

        for (int spawnIndex = 1; spawnIndex < spawnCount; spawnIndex++)
        {
            float targetTime = DirectionalBurstMath.GetSpawnTime(
                spawnIndex,
                spawnCount,
                duration,
                UnityEngine.Random.Range(-1f, 1f));
            yield return WaitForGameplaySeconds(targetTime - elapsed);
            elapsed = targetTime;
            TrySpawnDirectionalTrash(state, direction);
        }
    }

    public void BeginSpawnEvent(SpawnTrigger trigger)
    {
        for (int i = 0; i < spawnStates.Count; i++)
        {
            SpawnRuntimeState state = spawnStates[i];

            if (state != null
                && state.spawnMode == SpawnMode.EventSpawn
                && state.spawnTrigger == trigger
                && state.eventSpawnPattern == EventSpawnPattern.DirectionalBurst)
            {
                state.remainingEventSpawnCount = Mathf.Max(1, state.eventSpawnCount);
            }
        }
    }

    private bool TrySpawnDirectionalTrash(SpawnRuntimeState state, WindDirection direction)
    {
        if (state == null || state.remainingEventSpawnCount <= 0)
        {
            return false;
        }

        if (!TrySpawnTrash(state, direction))
        {
            return false;
        }

        state.remainingEventSpawnCount--;
        return true;
    }

    private static IEnumerator WaitForGameplaySeconds(float seconds)
    {
        float remaining = Mathf.Max(0f, seconds);

        while (remaining > 0f || !IsGameplayActive())
        {
            yield return null;

            if (IsGameplayActive())
            {
                remaining -= Time.deltaTime;
            }
        }
    }

    private bool TrySpawnTrash(SpawnRuntimeState state, WindDirection? direction = null)
    {
        if (state == null || state.data == null)
        {
            return false;
        }

        Trash prefab = state.data.Prefab;

        if (prefab == null || IsSpawnBlocked(prefab.TrashType))
        {
            return false;
        }

        if (activeTrash.Count >= spawnLimit)
        {
            return false;
        }

        if (state.activeCount >= state.data.SpawnLimit)
        {
            return false;
        }

        TrashPath selectedPath = GetRandomValidPath(state);

        if (state.spawnArea == null && selectedPath == null)
        {
            if (HasConfiguredPaths(state))
            {
                return false;
            }

            if (!state.missingSpawnAreaLogged)
            {
                Debug.LogError($"SpawnService: Spawn Area is missing for {state.data.name}.");
                state.missingSpawnAreaLogged = true;
            }

            return false;
        }

        Trash trash = GetInactiveTrash(state);

        if (trash == null)
        {
            return false;
        }

        TrashPathFollower pathFollower = trash.GetComponent<TrashPathFollower>();

        if (selectedPath != null && pathFollower == null)
        {
            if (!state.missingPathFollowerLogged)
            {
                Debug.LogError($"SpawnService: '{trash.name}' needs TrashPathFollower to use a Trash Path.");
                state.missingPathFollowerLogged = true;
            }

            return false;
        }

        trash.transform.position = GetSpawnPosition(state, direction, selectedPath);

        if (selectedPath != null)
        {
            pathFollower.AssignPath(selectedPath);
        }
        else if (pathFollower != null)
        {
            pathFollower.ClearPath();
        }

        trash.PrepareForSpawn();
        trash.gameObject.SetActive(true);

        activeTrash.Add(trash);
        activeTrashStates[trash] = state;
        state.activeCount++;

        TrashAdded?.Invoke(trash);

        return true;
    }

    private Vector3 GetSpawnPosition(SpawnRuntimeState state, WindDirection? direction, TrashPath path)
    {
        if (path != null)
        {
            return path.GetPointPosition(0);
        }

        Trash prefab = state.data.Prefab;
        TrashType trashType = prefab.TrashType;

        if (!nextSpawnNormalizedPositions.TryGetValue(trashType, out Vector3 normalizedPosition))
        {
            if (direction.HasValue)
            {
                return SpawnAreaSampler.GetDirectionalEdgePoint(
                    state.spawnArea,
                    direction.Value,
                    state.directionalEdgeInsetRange);
            }

            return SpawnAreaSampler.GetRandomPoint(state.spawnArea);
        }

        nextSpawnNormalizedPositions.Remove(trashType);
        return SpawnAreaSampler.GetPoint(state.spawnArea, normalizedPosition);
    }

    private static TrashPath GetRandomValidPath(SpawnRuntimeState state)
    {
        if (state == null || state.paths == null || state.paths.Length == 0)
        {
            return null;
        }

        int validPathCount = 0;

        for (int i = 0; i < state.paths.Length; i++)
        {
            if (state.paths[i] != null
                && state.paths[i].isActiveAndEnabled
                && state.paths[i].IsValid())
            {
                validPathCount++;
            }
        }

        if (validPathCount == 0)
        {
            return null;
        }

        int selectedIndex = UnityEngine.Random.Range(0, validPathCount);

        for (int i = 0; i < state.paths.Length; i++)
        {
            TrashPath candidate = state.paths[i];

            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.IsValid())
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return candidate;
            }

            selectedIndex--;
        }

        return null;
    }

    private static bool HasConfiguredPaths(SpawnRuntimeState state)
    {
        return state != null && state.paths != null && state.paths.Length > 0;
    }

    public bool TryGetSpawnArea(Trash trash, out Transform spawnArea)
    {
        if (trash != null
            && activeTrashStates.TryGetValue(trash, out SpawnRuntimeState state)
            && state != null
            && state.spawnArea != null)
        {
            spawnArea = state.spawnArea;
            return true;
        }

        spawnArea = null;
        return false;
    }

    public void GetActiveTrash(List<Trash> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();
        results.AddRange(activeTrash);
    }

    public bool TryGetSpawnArea(SpawnTrigger trigger, out Transform spawnArea)
    {
        for (int i = 0; i < spawnStates.Count; i++)
        {
            SpawnRuntimeState state = spawnStates[i];

            if (state != null
                && state.spawnMode == SpawnMode.EventSpawn
                && state.spawnTrigger == trigger
                && state.spawnArea != null)
            {
                spawnArea = state.spawnArea;
                return true;
            }
        }

        spawnArea = null;
        return false;
    }

    private Trash GetInactiveTrash(SpawnRuntimeState state)
    {
        if (state == null || state.pool == null)
        {
            return null;
        }

        List<Trash> pool = state.pool;

        for (int i = 0; i < pool.Count; i++)
        {
            Trash trash = pool[i];

            if (trash != null && !trash.gameObject.activeSelf)
            {
                return trash;
            }
        }

        return null;
    }

    public void DespawnTrash(Trash trash)
    {
        DespawnTrash(trash, TrashRemovalSource.Player);
    }

    public void DespawnTrash(Trash trash, TrashRemovalSource removalSource)
    {
        if (trash == null)
        {
            return;
        }

        if (!activeTrash.Contains(trash))
        {
            return;
        }

        activeTrash.Remove(trash);

        if (activeTrashStates.TryGetValue(trash, out SpawnRuntimeState state))
        {
            state.activeCount = Mathf.Max(0, state.activeCount - 1);
            activeTrashStates.Remove(trash);
        }

        // The pickup view may be detached from a scaled trash object while visible.
        // Reattach it before the parent enters Unity's deactivation pass.
        trash.HidePickupProgress();
        trash.gameObject.SetActive(false);

        TrashRemovedWithSource?.Invoke(trash, removalSource);
        TrashRemoved?.Invoke(trash);
    }

    private void HandleAutoCollectRequested(BonusDefinition bonus)
    {
        Trash trashToCollect = GetAnyActiveTrashMatchingBonus(bonus);

        if (trashToCollect == null)
        {
            return;
        }

        DespawnTrash(trashToCollect);
    }

    private Trash GetAnyActiveTrashMatchingBonus(BonusDefinition bonus)
    {
        if (bonus == null)
        {
            return null;
        }

        foreach (Trash trash in activeTrash)
        {
            if (trash == null)
            {
                continue;
            }

            if (!trash.gameObject.activeSelf)
            {
                continue;
            }

            if (!bonus.MatchesTrash(trash))
            {
                continue;
            }

            return trash;
        }

        return null;
    }

    private void OnValidate()
    {
        spawnLimit = Mathf.Max(1, spawnLimit);
        spawnTickInterval = Mathf.Max(0.01f, spawnTickInterval);
        spawnTickRandomVariation = Mathf.Max(0f, spawnTickRandomVariation);

        if (trashTypes == null)
        {
            return;
        }

        for (int i = 0; i < trashTypes.Length; i++)
        {
            TrashTypeSpawnConfig config = trashTypes[i];

            if (config == null)
            {
                continue;
            }

            config.eventSpawnCount = Mathf.Max(1, config.eventSpawnCount);
            config.directionalBurstDurationRange = NormalizePositiveRange(config.directionalBurstDurationRange);
            config.directionalEdgeInsetRange = NormalizeInsetRange(config.directionalEdgeInsetRange);
        }
    }

    private static Vector2 NormalizePositiveRange(Vector2 range)
    {
        float minimum = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
        return new Vector2(minimum, maximum);
    }

    private static Vector2 NormalizeInsetRange(Vector2 range)
    {
        float minimum = Mathf.Clamp01(Mathf.Min(range.x, range.y));
        float maximum = Mathf.Clamp(Mathf.Max(range.x, range.y), minimum, 0.5f);
        return new Vector2(minimum, maximum);
    }

    private static bool IsGameplayActive()
    {
        return GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
    }
}
