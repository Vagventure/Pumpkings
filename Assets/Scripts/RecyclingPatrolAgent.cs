using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RecyclingPatrolAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(0.01f)] private float collectionRadius = 0.55f;
    [SerializeField, Min(0.01f)] private float targetSampleRadius = 2f;
    [SerializeField, Min(0f)] private float movingTargetThreshold = 0.05f;

    [Header("2D Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipSpriteWithHorizontalMovement = true;

    private RecyclingPatrolService service;
    private RecyclingPatrolDefinition definition;
    private NavMeshAgent agent;
    private NavMeshPath path;
    private Trash target;
    private Vector3 lastTargetPosition;
    private float remainingWorkSeconds;
    private float pickupDuration;
    private float pickupElapsed;
    private bool workStarted;
    private bool workExpired;
    private bool isPickingUp;
    private bool isExiting;

    public RecyclingPatrolDefinition Definition => definition;
    public bool ShowWorkTimer => definition != null && !isExiting && !workExpired;
    public float RemainingWorkSeconds => workStarted
        ? Mathf.Max(0f, remainingWorkSeconds)
        : definition == null ? 0f : definition.WorkDuration;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ConfigureAgent();
    }

    public void Initialize(RecyclingPatrolService patrolService, RecyclingPatrolDefinition patrolDefinition)
    {
        service = patrolService;
        definition = patrolDefinition;
        remainingWorkSeconds = definition == null ? 0f : definition.WorkDuration;
        TryAcquireTarget();
    }

    private void Update()
    {
        if (service == null || definition == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!IsGameplayActive())
        {
            PauseMovement();
            return;
        }

        ResumeMovement();

        if (isExiting)
        {
            UpdateExit();
            UpdateSprite();
            return;
        }

        if (isPickingUp)
        {
            TickWorkTimer();
            TickPickup();
            return;
        }

        TickWorkTimer();

        if (workExpired)
        {
            BeginExit();
            return;
        }

        if (!ValidateCurrentTarget())
        {
            ReleaseCurrentTarget(false);
            TryAcquireTarget();
            return;
        }

        UpdateTargetDestination();

        if (HasReachedTarget())
        {
            BeginPickup();
        }

        UpdateSprite();
    }

    private void TickWorkTimer()
    {
        if (!workStarted || workExpired)
        {
            return;
        }

        remainingWorkSeconds = Mathf.Max(0f, remainingWorkSeconds - Time.deltaTime);
        workExpired = remainingWorkSeconds <= 0f;
    }

    private bool ValidateCurrentTarget()
    {
        if (target == null)
        {
            return false;
        }

        if (!target.gameObject.activeInHierarchy
            || target.IsBeingCollected
            || (agent != null && !agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
            || !RecyclingPatrolTargetSelector.IsInsidePatrolArea(service.PatrolArea, target.transform.position))
        {
            return false;
        }

        return true;
    }

    private void TryAcquireTarget()
    {
        if (isExiting || workExpired || service == null)
        {
            return;
        }

        if (!service.TryClaimNearestTarget(transform.position, out target))
        {
            StopMovement();
            return;
        }

        lastTargetPosition = target.transform.position;
        target.BeginPickupProgress();

        if (!TrySetTargetDestination())
        {
            ReleaseCurrentTarget(false);
        }
    }

    private void UpdateTargetDestination()
    {
        if (target == null
            || Vector3.SqrMagnitude(target.transform.position - lastTargetPosition)
                < movingTargetThreshold * movingTargetThreshold)
        {
            return;
        }

        lastTargetPosition = target.transform.position;

        if (!TrySetTargetDestination())
        {
            ReleaseCurrentTarget(false);
        }
    }

    private bool TrySetTargetDestination()
    {
        return target != null && TrySetDestination(target.transform.position, targetSampleRadius);
    }

    private bool TrySetDestination(Vector3 worldPosition, float sampleRadius)
    {
        if (agent == null
            || !agent.enabled
            || !agent.isOnNavMesh
            || !RecyclingPatrolNavigation.TryCalculateCompletePath(
                transform.position,
                worldPosition,
                sampleRadius,
                agent.areaMask,
                path,
                out _))
        {
            return false;
        }

        agent.isStopped = false;
        return agent.SetPath(path);
    }

    private bool HasReachedTarget()
    {
        if (target == null || agent == null || agent.pathPending)
        {
            return false;
        }

        float horizontalDistance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.transform.position.x, target.transform.position.z));

        return horizontalDistance <= collectionRadius
            || (agent.hasPath && agent.remainingDistance <= collectionRadius + 0.05f);
    }

    private void BeginPickup()
    {
        if (target == null || target.IsBeingCollected)
        {
            ReleaseCurrentTarget(false);
            TryAcquireTarget();
            return;
        }

        StopMovement();
        isPickingUp = true;
        pickupElapsed = 0f;
        pickupDuration = Mathf.Max(0f, target.PickupTime * definition.PickupDurationMultiplier);
        target.SetBeingCollected(true);
        target.BeginPickupProgress();

        if (!workStarted)
        {
            workStarted = true;
            remainingWorkSeconds = definition.WorkDuration;
        }

        if (pickupDuration <= 0f)
        {
            CompletePickup();
        }
    }

    private void TickPickup()
    {
        if (target == null
            || !target.gameObject.activeInHierarchy
            || !RecyclingPatrolTargetSelector.IsInsidePatrolArea(service.PatrolArea, target.transform.position))
        {
            ReleaseCurrentTarget(true);
            isPickingUp = false;

            if (workExpired)
            {
                BeginExit();
            }
            else
            {
                TryAcquireTarget();
            }

            return;
        }

        pickupElapsed += Time.deltaTime;
        target.SetPickupProgress(pickupDuration <= 0f ? 1f : pickupElapsed / pickupDuration);

        if (pickupElapsed >= pickupDuration)
        {
            CompletePickup();
        }
    }

    private void CompletePickup()
    {
        Trash collectedTrash = target;
        target = null;
        isPickingUp = false;
        service.ReleaseClaim(collectedTrash);
        service.Spawner.DespawnTrash(collectedTrash, TrashRemovalSource.RecyclingPatrol);

        if (collectedTrash != null && collectedTrash.gameObject.activeInHierarchy)
        {
            collectedTrash.SetBeingCollected(false);
        }

        if (workExpired)
        {
            BeginExit();
        }
        else
        {
            TryAcquireTarget();
        }
    }

    private void BeginExit()
    {
        if (isExiting)
        {
            return;
        }

        ReleaseCurrentTarget(isPickingUp);
        isPickingUp = false;
        isExiting = true;

        if (service.ExitPoint == null
            || !TrySetDestination(service.ExitPoint.position, targetSampleRadius))
        {
            Destroy(gameObject);
        }
    }

    private void UpdateExit()
    {
        if (agent == null
            || !agent.enabled
            || !agent.isOnNavMesh
            || (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= collectionRadius)))
        {
            Destroy(gameObject);
        }
    }

    private void ReleaseCurrentTarget(bool releaseCollection)
    {
        Trash releasedTarget = target;
        target = null;

        if (releasedTarget == null)
        {
            return;
        }

        service?.ReleaseClaim(releasedTarget);

        if (!releasedTarget.gameObject.activeInHierarchy)
        {
            return;
        }

        if (releaseCollection)
        {
            releasedTarget.SetBeingCollected(false);
        }
        else
        {
            releasedTarget.HidePickupProgress();
        }
    }

    private void ConfigureAgent()
    {
        if (agent == null)
        {
            return;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = collectionRadius;
        agent.updateRotation = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    }

    private void PauseMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    private void ResumeMovement()
    {
        if (!isPickingUp && agent != null && agent.enabled && agent.isOnNavMesh && agent.isStopped)
        {
            agent.isStopped = false;
        }
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void UpdateSprite()
    {
        if (!flipSpriteWithHorizontalMovement || spriteRenderer == null || agent == null)
        {
            return;
        }

        Vector3 velocity = agent.velocity.sqrMagnitude > 0.001f ? agent.velocity : agent.desiredVelocity;

        if (Mathf.Abs(velocity.x) > 0.01f)
        {
            spriteRenderer.flipX = velocity.x < 0f;
        }
    }

    private void OnDestroy()
    {
        ReleaseCurrentTarget(isPickingUp);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        collectionRadius = Mathf.Max(0.01f, collectionRadius);
        targetSampleRadius = Mathf.Max(0.01f, targetSampleRadius);
        movingTargetThreshold = Mathf.Max(0f, movingTargetThreshold);

        if (agent != null)
        {
            ConfigureAgent();
        }
    }

    private static bool IsGameplayActive()
    {
        return GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
    }
}
