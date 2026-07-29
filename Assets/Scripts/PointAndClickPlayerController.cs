using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickPlayerController : MonoBehaviour
{
    public static event Action OnPlayerWalkStartedSFX;
    public static event Action OnPlayerWalkStoppedSFX;

    [Header("Input")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private LayerMask movementSurfaceLayerMask = ~0;
    [SerializeField] private LayerMask clickBlockerLayerMask = 0;
    [SerializeField] private float clickRayDistance = 100f;
    [SerializeField] private bool ignoreClicksOverUi = true;
    [SerializeField] private bool forceGameplayInputLayers = true;
    [SerializeField] private bool debugClickRaycasts;

    [Header("Trash Pickup")]
    [SerializeField, Min(0.01f)] private float trashPickupDistance = 0.55f;
    [SerializeField, Min(0f)] private float trashDestinationSampleRadius = 2f;
    [SerializeField, Min(0f)] private float trashClickSphereCastRadius = 0.25f;
    [SerializeField] private string trashPickupStateName = "SS_CrouchIdle";

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 3.5f;
    [SerializeField, Min(0f)] private float stoppingDistance = 0.15f;
    [SerializeField, Min(0f)] private float arrivalStopDistance = 0.08f;
    [SerializeField, Min(0f)] private float navMeshSampleRadius = 1.5f;
    [SerializeField, Min(0f)] private float destinationChangeThreshold = 0.05f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;
    [SerializeField, Range(0f, 180f)] private float maxCameraFacingAngle = 75f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private bool disableRootMotion = true;
    [SerializeField, Min(0f)] private float animationCrossFadeDuration = 0.12f;

    private NavMeshAgent agent;
    private Trash pendingTrash;
    private Trash collectingTrash;
    private Vector3 lastTrackedTrashPosition;
    private Coroutine trashPickupCoroutine;
    private bool isPickingUpTrash;
    private int destinationSetFrame = -1;
    private int idleStateHash;
    private int walkStateHash;
    private int trashPickupStateHash;
    private int currentAnimationStateHash;
    private bool wasMovingForSFX;
    private NavMeshPath trashPickupPath;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private const string MovementSurfaceLayerName = "Movement Surface Layer Mask";
    private const string TrashLayerName = "Trash";

    public bool IsMoving => CanUseAgentPath() && !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        trashPickupPath = new NavMeshPath();

        movementSurfaceLayerMask = ResolveMovementSurfaceLayerMask(movementSurfaceLayerMask, forceGameplayInputLayers);
        clickBlockerLayerMask = ResolveClickBlockerLayerMask(clickBlockerLayerMask, forceGameplayInputLayers);

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimationStateHashes();
        ConfigureAgent();
        ApplyRootMotionSetting();
    }

    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0.1f, walkSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
        arrivalStopDistance = Mathf.Max(0f, arrivalStopDistance);
        navMeshSampleRadius = Mathf.Max(0f, navMeshSampleRadius);
        destinationChangeThreshold = Mathf.Max(0f, destinationChangeThreshold);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        maxCameraFacingAngle = Mathf.Clamp(maxCameraFacingAngle, 0f, 180f);
        trashPickupDistance = Mathf.Max(0.01f, trashPickupDistance);
        trashDestinationSampleRadius = Mathf.Max(0f, trashDestinationSampleRadius);
        trashClickSphereCastRadius = Mathf.Max(0f, trashClickSphereCastRadius);
        animationCrossFadeDuration = Mathf.Max(0f, animationCrossFadeDuration);

        if (agent != null)
        {
            ConfigureAgent();
        }
    }

    private void Update()
    {
        if (!IsGameplayActive())
        {
            StopAgentForInactiveGameplay();
            UpdateAnimator(false);
            UpdatePlayerWalkSFX(false);
            return;
        }

        if (agent.enabled && agent.isOnNavMesh && agent.isStopped)
        {
            agent.isStopped = false;
        }

        HandleClickInput();
        UpdatePendingMovableTrashDestination();
        TryStartCollectingPendingTrash();

        if (isPickingUpTrash)
        {
            UpdatePlayerWalkSFX(false);
            return;
        }

        bool moving = HasMovementIntent();

        if (!moving)
        {
            StopMoving();
        }
        else
        {
            RotateTowardsMovement();
        }

        UpdateAnimator(moving);
        UpdatePlayerWalkSFX(moving);
    }

    public void StopMoving()
    {
        if (!CanUseAgentPath())
        {
            UpdateAnimator(false);
            UpdatePlayerWalkSFX(false);
            return;
        }

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        UpdateAnimator(false);
        UpdatePlayerWalkSFX(false);
    }

    private void OnDisable()
    {
        if (collectingTrash != null)
        {
            collectingTrash.SetBeingCollected(false);
            collectingTrash = null;
        }

        if (trashPickupCoroutine != null)
        {
            StopCoroutine(trashPickupCoroutine);
            trashPickupCoroutine = null;
        }

        isPickingUpTrash = false;
        UpdatePlayerWalkSFX(false);
    }

    private void ConfigureAgent()
    {
        agent.updateRotation = false;
        agent.stoppingDistance = stoppingDistance;
        agent.speed = walkSpeed;
    }

    private void CacheAnimationStateHashes()
    {
        idleStateHash = GetAnimatorHash(idleStateName);
        walkStateHash = GetAnimatorHash(walkStateName);
        trashPickupStateHash = GetAnimatorHash(trashPickupStateName);
    }

    private static int GetAnimatorHash(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return 0;
        }

        return Animator.StringToHash(parameterName);
    }

    private void HandleClickInput()
    {
        if (isPickingUpTrash)
        {
            return;
        }

        if (!TryGetPointerPressPosition(out Vector2 pointerPosition))
        {
            return;
        }

        if (IsPointerOverUi(pointerPosition))
        {
            return;
        }

        if (TryHandleTrashClick(pointerPosition))
        {
            return;
        }

        if (!InputRaycastCameraResolver.TryRaycast(
                inputCamera,
                pointerPosition,
                clickRayDistance,
                movementSurfaceLayerMask,
                out RaycastHit hit))
        {
            return;
        }

        if (!TryGetNavMeshDestination(hit.point, out Vector3 destination))
        {
            return;
        }

        SetDestination(destination);
        pendingTrash = null;
    }

    private static bool TryGetPointerPressPosition(out Vector2 screenPosition)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private bool IsPointerOverUi(Vector2 screenPosition)
    {
        if (!ignoreClicksOverUi || EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

    private bool TryHandleTrashClick(Vector2 mousePosition)
    {
        if (clickBlockerLayerMask.value == 0)
        {
            return false;
        }

        if (!InputRaycastCameraResolver.TryRaycast(
                inputCamera,
                mousePosition,
                clickRayDistance,
                clickBlockerLayerMask,
                trashClickSphereCastRadius,
                out RaycastHit hit))
        {
            DebugLogClickHits(mousePosition, "trash miss");
            return false;
        }

        Trash trash = hit.collider.GetComponentInParent<Trash>();

        if (trash == null)
        {
            DebugLogClickHit(mousePosition, hit, "trash layer hit without Trash component");
            return true;
        }

        if (trash.IsBeingCollected)
        {
            DebugLogClickHit(mousePosition, hit, $"trash already being collected: {trash.name}");
            return true;
        }

        DebugLogClickHit(mousePosition, hit, $"trash selected: {trash.name}");
        if (TryGetTrashDestination(trash, mousePosition, hit.point, out Vector3 destination))
        {
            pendingTrash = trash;
            lastTrackedTrashPosition = trash.transform.position;
            SetDestination(destination);
        }
        else
        {
            pendingTrash = null;
            DebugLogClickHit(mousePosition, hit, $"trash selected but no NavMesh destination: {trash.name}");
        }

        return true;
    }

    private bool TryGetTrashDestination(Trash trash, Vector2 mousePosition, Vector3 hitPoint, out Vector3 destination)
    {
        if (InputRaycastCameraResolver.TryRaycast(
                inputCamera,
                mousePosition,
                clickRayDistance,
                movementSurfaceLayerMask,
                out RaycastHit movementHit)
            && TryGetNavMeshDestination(movementHit.point, out destination))
        {
            if (IsReachablePickupDestination(trash, destination))
            {
                return true;
            }
        }

        if (TryGetTrashNavMeshDestination(trash, out destination))
        {
            return true;
        }

        Vector3 flattenedHitPoint = FlattenToAgentHeight(hitPoint);

        return TryGetNavMeshDestination(
                flattenedHitPoint,
                out destination,
                trashDestinationSampleRadius)
            && IsReachablePickupDestination(trash, destination);
    }

    private void TryStartCollectingPendingTrash()
    {
        if (isPickingUpTrash)
        {
            return;
        }

        if (pendingTrash == null || !pendingTrash.gameObject.activeSelf)
        {
            pendingTrash = null;
            return;
        }

        if (pendingTrash.IsBeingCollected)
        {
            pendingTrash = null;
            StopMoving();
            return;
        }

        if (!IsCloseEnoughToCollect(pendingTrash))
        {
            return;
        }

        Trash trash = pendingTrash;
        pendingTrash = null;
        trash.SetBeingCollected(true);

        StopMoving();

        if (trashPickupCoroutine != null)
        {
            StopCoroutine(trashPickupCoroutine);
        }

        trashPickupCoroutine = StartCoroutine(CollectTrashAfterPickupAnimation(trash));
    }

    private IEnumerator CollectTrashAfterPickupAnimation(Trash trash)
    {
        isPickingUpTrash = true;
        collectingTrash = trash;
        PlayAnimationState(trashPickupStateHash);

        float pickupTime = trash != null ? trash.PickupTime : 0f;

        if (pickupTime > 0f)
        {
            yield return new WaitForSeconds(pickupTime);
        }

        if (trash != null && trash.gameObject.activeSelf)
        {
            MousePickUpController.CollectTrash(trash);
        }

        if (trash != null)
        {
            trash.SetBeingCollected(false);
        }

        collectingTrash = null;

        PlayAnimationState(idleStateHash);
        isPickingUpTrash = false;
        trashPickupCoroutine = null;
    }

    private bool IsCloseEnoughToCollect(Trash trash)
    {
        return GetHorizontalDistanceToTrash(trash, transform.position) <= trashPickupDistance;
    }

    private void UpdatePendingMovableTrashDestination()
    {
        if (pendingTrash == null
            || !pendingTrash.gameObject.activeSelf
            || !pendingTrash.RequiresDynamicPickupTracking
            || isPickingUpTrash)
        {
            return;
        }

        Vector3 currentTrashPosition = pendingTrash.transform.position;

        if (Vector3.Distance(lastTrackedTrashPosition, currentTrashPosition)
            <= destinationChangeThreshold)
        {
            return;
        }

        if (TryGetTrashNavMeshDestination(pendingTrash, out Vector3 destination))
        {
            lastTrackedTrashPosition = currentTrashPosition;
            SetDestination(destination);
            return;
        }

        if (!IsCloseEnoughToCollect(pendingTrash))
        {
            pendingTrash = null;
            StopMoving();
        }
    }

    private bool TryGetTrashNavMeshDestination(Trash trash, out Vector3 destination)
    {
        if (trash == null)
        {
            destination = default;
            return false;
        }

        Vector3 queryPoint = FlattenToAgentHeight(trash.transform.position);

        return TryGetNavMeshDestination(
                queryPoint,
                out destination,
                trashDestinationSampleRadius)
            && IsReachablePickupDestination(trash, destination);
    }

    private bool IsReachablePickupDestination(Trash trash, Vector3 destination)
    {
        if (trash == null || !CanUseAgentPath())
        {
            return false;
        }

        if (!agent.CalculatePath(destination, trashPickupPath)
            || trashPickupPath.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        Vector3 reachableEndpoint = trashPickupPath.corners.Length > 0
            ? trashPickupPath.corners[trashPickupPath.corners.Length - 1]
            : destination;

        return GetHorizontalDistanceToTrash(trash, reachableEndpoint) <= trashPickupDistance;
    }

    private Vector3 FlattenToAgentHeight(Vector3 point)
    {
        point.y = agent != null ? agent.transform.position.y : transform.position.y;
        return point;
    }

    private static float GetHorizontalDistanceToTrash(Trash trash, Vector3 worldPoint)
    {
        if (trash == null)
        {
            return float.PositiveInfinity;
        }

        Collider trashCollider = trash.GetComponentInChildren<Collider>();
        Vector3 closestPoint = trashCollider != null
            ? trashCollider.ClosestPoint(worldPoint)
            : trash.transform.position;
        closestPoint.y = worldPoint.y;
        return Vector3.Distance(worldPoint, closestPoint);
    }

    private void DebugLogClickHit(Vector2 mousePosition, RaycastHit hit, string reason)
    {
        if (!debugClickRaycasts)
        {
            return;
        }

        Debug.Log($"[DEBUG-click-ray] {reason}; screen={mousePosition}; hit={hit.collider.name}; layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}; distance={hit.distance:0.###}; point={hit.point}");
    }

    private void DebugLogClickHits(Vector2 mousePosition, string reason)
    {
        if (!debugClickRaycasts)
        {
            return;
        }

        Camera camera = InputRaycastCameraResolver.ResolveFallback(inputCamera);

        if (camera == null)
        {
            Debug.Log($"[DEBUG-click-ray] {reason}; screen={mousePosition}; no camera");
            return;
        }

        Ray ray = camera.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, clickRayDistance, ~0);
        System.Array.Sort(hits, CompareRaycastHitsByDistance);

        if (hits.Length == 0)
        {
            Debug.Log($"[DEBUG-click-ray] {reason}; screen={mousePosition}; no hits");
            return;
        }

        int hitCount = Mathf.Min(5, hits.Length);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            Trash trash = hit.collider.GetComponentInParent<Trash>();
            string trashName = trash != null ? trash.name : "none";
            Debug.Log($"[DEBUG-click-ray] {reason}; rank={i}; hit={hit.collider.name}; layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}; trash={trashName}; distance={hit.distance:0.###}; point={hit.point}");
        }
    }

    private static int CompareRaycastHitsByDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }

    private static LayerMask ResolveMovementSurfaceLayerMask(LayerMask configuredMask, bool forceGameplayInputLayers)
    {
        if (!forceGameplayInputLayers && configuredMask.value != ~0)
        {
            return configuredMask;
        }

        return ResolveNamedLayerMask(configuredMask, MovementSurfaceLayerName);
    }

    private static LayerMask ResolveClickBlockerLayerMask(LayerMask configuredMask, bool forceGameplayInputLayers)
    {
        if (!forceGameplayInputLayers && configuredMask.value != 0)
        {
            return configuredMask;
        }

        return ResolveNamedLayerMask(configuredMask, TrashLayerName);
    }

    private static LayerMask ResolveNamedLayerMask(LayerMask fallbackMask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? 1 << layer : fallbackMask;
    }

    private bool TryGetNavMeshDestination(Vector3 hitPoint, out Vector3 destination)
    {
        return TryGetNavMeshDestination(hitPoint, out destination, navMeshSampleRadius);
    }

    private bool TryGetNavMeshDestination(Vector3 hitPoint, out Vector3 destination, float sampleRadius)
    {
        if (NavMesh.SamplePosition(hitPoint, out NavMeshHit navMeshHit, sampleRadius, NavMesh.AllAreas))
        {
            destination = navMeshHit.position;
            return true;
        }

        destination = hitPoint;
        return false;
    }

    private void SetDestination(Vector3 destination)
    {
        if (!CanUseAgentPath())
        {
            return;
        }

        if (agent.hasPath && Vector3.Distance(agent.destination, destination) <= destinationChangeThreshold)
        {
            return;
        }

        agent.isStopped = false;
        bool destinationSet = agent.SetDestination(destination);

        if (destinationSet)
        {
            destinationSetFrame = Time.frameCount;
        }

        if (debugClickRaycasts)
        {
            Debug.Log($"[DEBUG-click-ray] SetDestination; success={destinationSet}; destination={destination}; isOnNavMesh={agent.isOnNavMesh}; pathStatus={agent.pathStatus}");
        }
    }

    private void RotateTowardsMovement()
    {
        if (rotationSpeed <= 0f)
        {
            return;
        }

        if (!HasMovementIntent())
        {
            return;
        }

        Vector3 direction = agent.velocity.sqrMagnitude > 0.001f ? agent.velocity : agent.desiredVelocity;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Camera camera = InputRaycastCameraResolver.ResolveFallback(inputCamera);
        Quaternion targetRotation = CameraFacingRotationUtility.GetSemiBillboardRotation(
            transform.position,
            direction,
            camera != null ? camera.transform : null,
            maxCameraFacingAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateAnimator(bool moving)
    {
        if (animator == null)
        {
            return;
        }

        PlayAnimationState(moving ? walkStateHash : idleStateHash);
    }

    private void UpdatePlayerWalkSFX(bool moving)
    {
        if (wasMovingForSFX == moving)
        {
            return;
        }

        wasMovingForSFX = moving;

        if (moving)
        {
            OnPlayerWalkStartedSFX?.Invoke();
        }
        else
        {
            OnPlayerWalkStoppedSFX?.Invoke();
        }
    }

    private void PlayAnimationState(int targetStateHash)
    {
        if (animator == null || targetStateHash == 0 || currentAnimationStateHash == targetStateHash)
        {
            return;
        }

        animator.CrossFade(targetStateHash, animationCrossFadeDuration, 0);
        currentAnimationStateHash = targetStateHash;
    }

    private bool HasMovementIntent()
    {
        if (!CanUseAgentPath() || agent.isStopped)
        {
            return false;
        }

        if (Time.frameCount == destinationSetFrame)
        {
            return true;
        }

        if (agent.pathPending)
        {
            return true;
        }

        if (!agent.hasPath)
        {
            return false;
        }

        float stopDistance = agent.stoppingDistance + arrivalStopDistance;

        return agent.remainingDistance > stopDistance
               && agent.desiredVelocity.sqrMagnitude > 0.001f;
    }

    private void StopAgentForInactiveGameplay()
    {
        if (!CanUseAgentPath() || agent.isStopped)
        {
            return;
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private bool CanUseAgentPath()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void ApplyRootMotionSetting()
    {
        if (animator != null && disableRootMotion)
        {
            animator.applyRootMotion = false;
        }
    }

    private static bool IsGameplayActive()
    {
        return GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
    }
}
