using UnityEngine;

public class Trash : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private TrashType trashType;
    [SerializeField] private string trashName;

    [Header("Gameplay")]
    [SerializeField] private int score;
    [SerializeField] private int income = 1;
    [SerializeField, Min(0f)] private float pickupTime = 0.2f;
    [SerializeField] private bool isMovable;

    [Header("Pickup View")]
    [SerializeField] private TrashPickupProgressView pickupProgressView;

    [Header("Sounds")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip despawnSound;

    public TrashType TrashType => trashType;
    public string Name => trashName;
    public int Score => score;
    public int Income => income;
    public float PickupTime => pickupTime;
    public bool IsMovable => isMovable;
    public bool IsBeingCollected { get; private set; }
    public bool RequiresDynamicPickupTracking
    {
        get
        {
            ITrashMover mover = GetComponent<ITrashMover>();
            return isMovable || (mover != null && mover.IsMoving);
        }
    }

    public AudioClip SpawnSound => spawnSound;
    public AudioClip DespawnSound => despawnSound;

    public void SetBeingCollected(bool beingCollected)
    {
        IsBeingCollected = beingCollected;

        if (!beingCollected)
        {
            pickupProgressView?.Hide();
        }
    }

    public void BeginPickupProgress()
    {
        pickupProgressView?.Show();
    }

    public void SetPickupProgress(float progress01)
    {
        pickupProgressView?.SetProgress(progress01);
    }

    public void HidePickupProgress()
    {
        pickupProgressView?.Hide();
    }

    public void PrepareForSpawn()
    {
        IsBeingCollected = false;
        pickupProgressView?.Hide();

        if (!isMovable)
        {
            return;
        }

        Vector3 localEulerAngles = transform.localEulerAngles;
        localEulerAngles.z = Random.Range(0f, 360f);
        transform.localEulerAngles = localEulerAngles;
    }

    private void OnDisable()
    {
        IsBeingCollected = false;
        pickupProgressView?.Hide();
    }

    private void OnValidate()
    {
        pickupTime = Mathf.Max(0f, pickupTime);
    }
}
