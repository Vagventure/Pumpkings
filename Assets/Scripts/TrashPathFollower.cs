using UnityEngine;

[RequireComponent(typeof(Trash))]
public class TrashPathFollower : MonoBehaviour, ITrashMover
{
    private Trash trash;
    private TrashPath path;
    private int currentWaypointIndex;

    public bool IsMoving { get; private set; }
    public int CurrentWaypointIndex => currentWaypointIndex;
    public TrashPath Path => path;

    private void Awake()
    {
        trash = GetComponent<Trash>();
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void AssignPath(TrashPath assignedPath)
    {
        path = assignedPath;
        currentWaypointIndex = 0;
        IsMoving = false;

        if (path == null || !path.IsValid())
        {
            return;
        }

        transform.position = path.GetPointPosition(0);

        if (path.PointCount > 1)
        {
            currentWaypointIndex = 1;
            IsMoving = true;
        }
    }

    public void ClearPath()
    {
        path = null;
        currentWaypointIndex = 0;
        IsMoving = false;
    }

    public void Advance(float deltaTime)
    {
        if (!IsMoving
            || path == null
            || deltaTime <= 0f
            || (GameManager.Instance != null && !GameManager.Instance.IsGameplayActive)
            || (trash != null && trash.IsBeingCollected))
        {
            return;
        }

        float remainingDistance = path.MovementSpeed * deltaTime;

        while (IsMoving && remainingDistance > 0f)
        {
            Vector3 destination = path.GetPointPosition(currentWaypointIndex);
            float distanceToDestination = Vector3.Distance(transform.position, destination);

            if (distanceToDestination > remainingDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, remainingDistance);
                return;
            }

            transform.position = destination;
            remainingDistance -= distanceToDestination;

            if (currentWaypointIndex >= path.PointCount - 1)
            {
                IsMoving = false;
                return;
            }

            currentWaypointIndex++;
        }
    }
}