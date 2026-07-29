using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(Trash))]
public class TrashFlowFollower : MonoBehaviour, ITrashMover
{
    [SerializeField] private SplineContainer[] rivers;
    [SerializeField] private float speed = 0.1f; // progress per second (0 to 1 range)

    private Trash trash;
    private SplineContainer activeRiver;
    private float t;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        trash = GetComponent<Trash>();
    }

    private void OnEnable()
    {
        PickRandomRiver();
    }

    private void PickRandomRiver()
    {
        t = 0f;

        if (rivers == null || rivers.Length == 0)
        {
            activeRiver = null;
            IsMoving = false;
            Debug.LogWarning($"TrashFlowFollower on '{name}': no SplineContainers assigned.");
            return;
        }

        activeRiver = rivers[Random.Range(0, rivers.Length)];
        IsMoving = activeRiver != null;

        if (activeRiver != null)
        {
            transform.position = activeRiver.EvaluatePosition(0f);
        }
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (!IsMoving
            || activeRiver == null
            || deltaTime <= 0f
            || (GameManager.Instance != null && !GameManager.Instance.IsGameplayActive)
            || (trash != null && trash.IsBeingCollected))
        {
            return;
        }

        t += speed * deltaTime;

        if (t >= 1f)
        {
            // reached the end point — stop here, stay visible and collectible
            t = 1f;
            transform.position = activeRiver.EvaluatePosition(t);
            IsMoving = false;
            return;
        }

        transform.position = activeRiver.EvaluatePosition(t);
    }
}