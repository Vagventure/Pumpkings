using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RecyclingPatrolService : MonoBehaviour
{
    public static RecyclingPatrolService Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SpawnService spawnService;
    [SerializeField] private RecyclingPatrolAgent patrolPrefab;

    [Header("Patrol Route")]
    [SerializeField] private Transform patrolArea;
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Navigation")]
    [SerializeField, Min(0.01f)] private float trashNavMeshSampleRadius = 2f;
    [SerializeField, Min(0.05f)] private float availabilityRefreshInterval = 0.2f;

    private readonly Dictionary<RecyclingPatrolDefinition, RecyclingPatrolCooldown> cooldowns = new();
    private readonly HashSet<Trash> claimedTrash = new();
    private readonly List<Trash> trashBuffer = new();
    private NavMeshPath availabilityPath;
    private RecyclingPatrolAgent latestPatrol;
    private bool availabilityDirty = true;
    private bool cachedHasAvailableTarget;
    private float nextAvailabilityRefreshTime;

    public Transform PatrolArea => patrolArea;
    public Transform ExitPoint => exitPoint;
    public SpawnService Spawner => spawnService;

    private void Awake()
    {
        availabilityPath = new NavMeshPath();

        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        ScoringService.ItemPurchaseConfirmed += HandlePurchaseConfirmed;
        SpawnService.TrashAdded += HandleTrashChanged;
        SpawnService.TrashRemoved += HandleTrashRemoved;
    }

    private void OnDisable()
    {
        ScoringService.ItemPurchaseConfirmed -= HandlePurchaseConfirmed;
        SpawnService.TrashAdded -= HandleTrashChanged;
        SpawnService.TrashRemoved -= HandleTrashRemoved;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        bool gameplayActive = IsGameplayActive();

        foreach (RecyclingPatrolCooldown cooldown in cooldowns.Values)
        {
            cooldown.Tick(Time.deltaTime, gameplayActive);
        }
    }

    public static bool IsPurchaseAvailable(ShopItemDefinition definition, bool forceRefresh = false)
    {
        if (definition is not RecyclingPatrolDefinition patrolDefinition)
        {
            return true;
        }

        return Instance != null && Instance.CanPurchase(patrolDefinition, forceRefresh);
    }

    public bool CanPurchase(RecyclingPatrolDefinition definition, bool forceRefresh = false)
    {
        if (definition == null
            || spawnService == null
            || patrolPrefab == null
            || patrolArea == null
            || entryPoint == null
            || exitPoint == null
            || !IsGameplayActive()
            || IsOnCooldown(definition))
        {
            return false;
        }

        if (forceRefresh || availabilityDirty || Time.time >= nextAvailabilityRefreshTime)
        {
            cachedHasAvailableTarget = TryGetEntryNavMeshPosition(out Vector3 entryNavMeshPosition)
                && TrySelectTarget(entryNavMeshPosition, out _);
            availabilityDirty = false;
            nextAvailabilityRefreshTime = Time.time + availabilityRefreshInterval;
        }

        return cachedHasAvailableTarget;
    }

    public float GetCooldownFill(RecyclingPatrolDefinition definition)
    {
        return definition != null && cooldowns.TryGetValue(definition, out RecyclingPatrolCooldown cooldown)
            ? cooldown.FillAmount
            : 0f;
    }

    public bool TryGetLatestWorkRemaining(RecyclingPatrolDefinition definition, out float remainingSeconds)
    {
        if (definition != null
            && latestPatrol != null
            && latestPatrol.Definition == definition
            && latestPatrol.ShowWorkTimer)
        {
            remainingSeconds = latestPatrol.RemainingWorkSeconds;
            return true;
        }

        remainingSeconds = 0f;
        return false;
    }

    public bool TryClaimNearestTarget(Vector3 origin, out Trash target)
    {
        if (!TrySelectTarget(origin, out target))
        {
            return false;
        }

        bool claimed = claimedTrash.Add(target);
        availabilityDirty |= claimed;
        return claimed;
    }

    public void ReleaseClaim(Trash trash)
    {
        if (trash != null)
        {
            availabilityDirty |= claimedTrash.Remove(trash);
        }
    }

    private bool TrySelectTarget(Vector3 origin, out Trash target)
    {
        target = null;

        if (spawnService == null || patrolArea == null)
        {
            return false;
        }

        spawnService.GetActiveTrash(trashBuffer);
        return RecyclingPatrolTargetSelector.TrySelectNearest(
            trashBuffer,
            patrolArea,
            claimedTrash,
            trash => GetReachablePathLength(origin, trash),
            out target);
    }

    private bool TryGetEntryNavMeshPosition(out Vector3 position)
    {
        NavMeshAgent prefabAgent = patrolPrefab == null ? null : patrolPrefab.GetComponent<NavMeshAgent>();
        int areaMask = prefabAgent == null ? NavMesh.AllAreas : prefabAgent.areaMask;

        if (entryPoint != null
            && NavMesh.SamplePosition(
                entryPoint.position,
                out NavMeshHit entryHit,
                trashNavMeshSampleRadius,
                areaMask))
        {
            position = entryHit.position;
            return true;
        }

        position = default;
        return false;
    }

    private float? GetReachablePathLength(Vector3 origin, Trash trash)
    {
        if (trash == null)
        {
            return null;
        }

        NavMeshAgent prefabAgent = patrolPrefab == null ? null : patrolPrefab.GetComponent<NavMeshAgent>();
        int areaMask = prefabAgent == null ? NavMesh.AllAreas : prefabAgent.areaMask;

        if (!RecyclingPatrolNavigation.TryCalculateCompletePath(
                origin,
                trash.transform.position,
                trashNavMeshSampleRadius,
                areaMask,
                availabilityPath,
                out float pathLength))
        {
            return null;
        }

        return pathLength;
    }

    private bool IsOnCooldown(RecyclingPatrolDefinition definition)
    {
        return cooldowns.TryGetValue(definition, out RecyclingPatrolCooldown cooldown) && cooldown.IsActive;
    }

    private void HandlePurchaseConfirmed(RewardItemView view)
    {
        if (view == null || view.ShopDefinition is not RecyclingPatrolDefinition definition)
        {
            return;
        }

        if (!cooldowns.TryGetValue(definition, out RecyclingPatrolCooldown cooldown))
        {
            cooldown = new RecyclingPatrolCooldown();
            cooldowns.Add(definition, cooldown);
        }

        cooldown.Start(definition.CooldownDuration);
        SpawnPatrol(definition);
    }

    private void SpawnPatrol(RecyclingPatrolDefinition definition)
    {
        if (!TryGetEntryNavMeshPosition(out Vector3 spawnPosition))
        {
            Debug.LogError("RecyclingPatrolService: Entry Point must be on or near the Patrol NavMesh.");
            return;
        }

        RecyclingPatrolAgent patrol = Instantiate(
            patrolPrefab,
            spawnPosition,
            entryPoint.rotation,
            transform);
        latestPatrol = patrol;
        patrol.Initialize(this, definition);
    }

    private void HandleTrashRemoved(Trash trash)
    {
        ReleaseClaim(trash);
        availabilityDirty = true;
    }

    private void HandleTrashChanged(Trash _)
    {
        availabilityDirty = true;
    }

    private void OnValidate()
    {
        trashNavMeshSampleRadius = Mathf.Max(0.01f, trashNavMeshSampleRadius);
        availabilityRefreshInterval = Mathf.Max(0.05f, availabilityRefreshInterval);
    }

    private static bool IsGameplayActive()
    {
        return GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
    }
}
