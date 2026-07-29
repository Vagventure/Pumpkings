using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardCatalog", menuName = "Pumpkins/Rewards/Reward Catalog")]
public class RewardCatalog : ScriptableObject
{
    [SerializeField] private List<RewardItem> rewards = new();

    public IReadOnlyList<RewardItem> Rewards => rewards;

    public bool Contains(RewardItem reward)
    {
        return reward != null && rewards != null && rewards.Contains(reward);
    }

    public bool TryGetNextReward(
        RewardPath path,
        Func<RewardItem, bool> isOwned,
        out RewardItem reward)
    {
        reward = null;

        if (path == RewardPath.None || rewards == null)
        {
            return false;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            RewardItem candidate = rewards[i];
            if (candidate == null || candidate.Path != path || (isOwned != null && isOwned(candidate)))
            {
                continue;
            }

            if (reward == null || candidate.Level < reward.Level)
            {
                reward = candidate;
            }
        }

        return reward != null;
    }

    private void OnValidate()
    {
        if (rewards == null)
        {
            rewards = new List<RewardItem>();
            return;
        }

        Dictionary<RewardPath, HashSet<int>> levelsByPath = new();

        foreach (RewardItem reward in rewards)
        {
            if (reward == null || reward.Path == RewardPath.None)
            {
                continue;
            }

            if (!levelsByPath.TryGetValue(reward.Path, out HashSet<int> levels))
            {
                levels = new HashSet<int>();
                levelsByPath.Add(reward.Path, levels);
            }

            if (!levels.Add(reward.Level))
            {
                Debug.LogWarning($"RewardCatalog: Duplicate level {reward.Level} in {reward.Path}.", this);
            }
        }

        foreach (KeyValuePair<RewardPath, HashSet<int>> entry in levelsByPath)
        {
            for (int level = 1; level <= entry.Value.Count; level++)
            {
                if (!entry.Value.Contains(level))
                {
                    Debug.LogWarning($"RewardCatalog: {entry.Key} is missing level {level}.", this);
                    break;
                }
            }
        }
    }
}
