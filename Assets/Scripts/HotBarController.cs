using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class HotBarController : MonoBehaviour
{
    [Header("Hotbar Container")]
    [FormerlySerializedAs("activeBonusesParent")]
    [SerializeField] private Transform slotsParent;

    [Header("Active Bonus Slots")]
    [FormerlySerializedAs("activeBonusViewPrefab")]
    [FormerlySerializedAs("bonusItemPrefab")]
    [SerializeField] private ActiveBonusSlotView activeBonusSlotPrefab;

    private readonly Dictionary<BonusDefinition, ActiveBonusSlotView> activeBonusSlots = new();
    private readonly HashSet<BonusDefinition> presentedBonuses = new();

    public Transform SlotsParent => slotsParent == null ? transform : slotsParent;

    private void OnEnable()
    {
        RewardManager.BonusActivated += HandleBonusActivated;
        PopulateExistingBonuses();
    }

    private void OnDisable()
    {
        RewardManager.BonusActivated -= HandleBonusActivated;
        ClearViews();
    }

    private void PopulateExistingBonuses()
    {
        RewardManager rewardManager = RewardManager.Instance;

        if (rewardManager == null)
        {
            return;
        }

        foreach (BonusDefinition bonus in rewardManager.ActiveBonuses)
        {
            HandleBonusActivated(bonus);
        }
    }

    private void HandleBonusActivated(BonusDefinition bonus)
    {
        if (bonus == null || activeBonusSlots.ContainsKey(bonus))
        {
            return;
        }

        if (activeBonusSlotPrefab == null)
        {
            Debug.LogWarning("HotBarController: Active Bonus Slot Prefab is missing.");
            return;
        }

        ActiveBonusSlotView slot = Instantiate(activeBonusSlotPrefab, SlotsParent);
        slot.Configure(bonus);
        activeBonusSlots.Add(bonus, slot);

        if (presentedBonuses.Add(bonus)
            && slot.TryGetComponent(out LayoutItemSlideIn slideIn))
        {
            slideIn.Play();
        }
    }

    private void ClearViews()
    {
        foreach (ActiveBonusSlotView slot in activeBonusSlots.Values)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        activeBonusSlots.Clear();
    }
}
