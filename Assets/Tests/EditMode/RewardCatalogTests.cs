#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RewardCatalogTests
{
    [Test]
    public void DefaultCatalog_ContainsBothSchoolLectureLevels()
    {
        RewardCatalog catalog = AssetDatabase.LoadAssetAtPath<RewardCatalog>(
            "Assets/ScriptableObjects/Catalogs/DefaultRewardCatalog.asset");

        Assert.That(catalog, Is.Not.Null);
        Assert.That(
            catalog.Rewards,
            Has.Exactly(1).Matches<RewardItem>(reward =>
                reward != null && reward.Path == RewardPath.SchoolLectures && reward.Level == 1));
        Assert.That(
            catalog.Rewards,
            Has.Exactly(1).Matches<RewardItem>(reward =>
                reward != null && reward.Path == RewardPath.SchoolLectures && reward.Level == 2));

        RewardItem levelOne = null;
        RewardItem levelTwo = null;
        foreach (RewardItem reward in catalog.Rewards)
        {
            if (reward == null || reward.Path != RewardPath.SchoolLectures)
            {
                continue;
            }

            if (reward.Level == 1)
            {
                levelOne = reward;
            }
            else if (reward.Level == 2)
            {
                levelTwo = reward;
            }
        }

        bool found = catalog.TryGetNextReward(
            RewardPath.SchoolLectures,
            reward => reward == levelOne,
            out RewardItem nextReward);

        Assert.That(found, Is.True);
        Assert.That(nextReward, Is.SameAs(levelTwo));
    }

    [Test]
    public void TryGetNextReward_ReturnsLowestUnownedLevelInPath()
    {
        RewardCatalog catalog = ScriptableObject.CreateInstance<RewardCatalog>();
        TestReward levelOne = CreateReward(RewardPath.Posters, 1);
        TestReward levelTwo = CreateReward(RewardPath.Posters, 2);

        try
        {
            SetRewards(catalog, new List<RewardItem> { levelTwo, levelOne });

            bool found = catalog.TryGetNextReward(
                RewardPath.Posters,
                reward => reward == levelOne,
                out RewardItem result);

            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(levelTwo));
        }
        finally
        {
            Object.DestroyImmediate(levelOne);
            Object.DestroyImmediate(levelTwo);
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void TryGetNextReward_ReturnsFalseWhenPathIsExhausted()
    {
        RewardCatalog catalog = ScriptableObject.CreateInstance<RewardCatalog>();
        TestReward reward = CreateReward(RewardPath.CleanUp, 1);

        try
        {
            SetRewards(catalog, new List<RewardItem> { reward });

            bool found = catalog.TryGetNextReward(
                RewardPath.CleanUp,
                _ => true,
                out RewardItem result);

            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(reward);
            Object.DestroyImmediate(catalog);
        }
    }

    private static TestReward CreateReward(RewardPath path, int level)
    {
        TestReward reward = ScriptableObject.CreateInstance<TestReward>();
        typeof(RewardItem).GetField("path", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(reward, path);
        typeof(RewardItem).GetField("level", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(reward, level);
        return reward;
    }

    private static void SetRewards(RewardCatalog catalog, List<RewardItem> rewards)
    {
        typeof(RewardCatalog).GetField("rewards", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog, rewards);
    }

    private sealed class TestReward : RewardItem
    {
    }
}
#endif
