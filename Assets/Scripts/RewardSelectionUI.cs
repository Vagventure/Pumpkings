using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RewardSelectionUI : MonoBehaviour
{
    [Header("Fixed Card Slots")]
    [SerializeField] private TMP_Text rewardTitleText;
    [SerializeField] private RewardSelectionRepresentation[] rewardChoiceSlots;

    private Action<RewardItem> rewardSelected;
    private bool opening;

    private void Awake()
    {
        if (!opening)
        {
            ClearSlots();
        }
    }

    public void Show(IReadOnlyList<RewardItem> rewards, Action<RewardItem> onRewardSelected)
    {
        Show(rewards, string.Empty, onRewardSelected);
    }

    public void Show(IReadOnlyList<RewardItem> rewards, string rewardTitle, Action<RewardItem> onRewardSelected)
    {
        ClearSlots();
        SetRewardTitle(rewardTitle);

        rewardSelected = onRewardSelected;
        opening = true;
        gameObject.SetActive(true);
        opening = false;

        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning("RewardSelectionUI: No rewards were provided.");
            return;
        }

        if (rewardChoiceSlots == null || rewardChoiceSlots.Length == 0)
        {
            Debug.LogWarning("RewardSelectionUI: Reward choice slots are missing.");
            return;
        }

        ShowInFixedSlots(rewards);
    }

    public void Hide()
    {
        ClearSlots();
        SetRewardTitle(string.Empty);
        rewardSelected = null;
        gameObject.SetActive(false);
    }

    private void HandleRewardSelected(RewardItem reward)
    {
        rewardSelected?.Invoke(reward);
        Hide();
    }

    private void ShowInFixedSlots(IReadOnlyList<RewardItem> rewards)
    {
        int rewardCount = rewards == null ? 0 : rewards.Count;

        for (int i = 0; i < rewardChoiceSlots.Length; i++)
        {
            RewardSelectionRepresentation slot = rewardChoiceSlots[i];

            if (slot == null)
            {
                continue;
            }

            bool hasReward = i < rewardCount && rewards[i] != null;
            slot.gameObject.SetActive(hasReward);

            if (hasReward)
            {
                slot.Configure(rewards[i], HandleRewardSelected);
            }
        }
    }

    private void SetRewardTitle(string rewardTitle)
    {
        if (rewardTitleText != null)
        {
            rewardTitleText.text = rewardTitle;
        }
    }

    private void ClearSlots()
    {
        if (rewardChoiceSlots == null)
        {
            return;
        }

        for (int i = 0; i < rewardChoiceSlots.Length; i++)
        {
            RewardSelectionRepresentation slot = rewardChoiceSlots[i];

            if (slot == null)
            {
                continue;
            }

            slot.Clear();
            slot.gameObject.SetActive(false);
        }
    }
}
