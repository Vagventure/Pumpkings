using UnityEngine;

[CreateAssetMenu(fileName = "RecyclingPatrol", menuName = "Pumpkins/Shop/Recycling Patrol Definition")]
public class RecyclingPatrolDefinition : ShopItemDefinition
{
    [Header("Recycling Patrol")]
    [SerializeField, Min(0.01f)] private float workDuration = 5f;
    [SerializeField, Min(0f)] private float cooldownDuration = 20f;
    [SerializeField, Min(0f)] private float pickupDurationMultiplier = 0.5f;

    public float WorkDuration => workDuration;
    public float CooldownDuration => cooldownDuration;
    public float PickupDurationMultiplier => pickupDurationMultiplier;

    protected override void OnValidate()
    {
        base.OnValidate();
        workDuration = Mathf.Max(0.01f, workDuration);
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
        pickupDurationMultiplier = Mathf.Max(0f, pickupDurationMultiplier);
    }
}
