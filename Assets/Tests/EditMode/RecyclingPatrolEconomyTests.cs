#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RecyclingPatrolEconomyTests
{
    [Test]
    public void RemovingTrash_AwardsIncomeOnlyForPlayerCollection()
    {
        GameObject scoringObject = new GameObject("Scoring Service");
        GameObject playerTrashObject = new GameObject("Player Trash");
        GameObject patrolTrashObject = new GameObject("Patrol Trash");

        try
        {
            ScoringService scoring = scoringObject.AddComponent<ScoringService>();
            Trash playerTrash = playerTrashObject.AddComponent<Trash>();
            Trash patrolTrash = patrolTrashObject.AddComponent<Trash>();
            SetField(playerTrash, "score", 6);
            SetField(playerTrash, "income", 3);
            SetField(patrolTrash, "score", 15);
            SetField(patrolTrash, "income", 10);

            scoring.RegisterSpawnedTrash(playerTrash);
            scoring.RegisterSpawnedTrash(patrolTrash);
            scoring.ApplyTrashRemoval(playerTrash, TrashRemovalSource.Player);
            scoring.ApplyTrashRemoval(patrolTrash, TrashRemovalSource.RecyclingPatrol);

            Assert.That(scoring.CurrentPollution, Is.Zero);
            Assert.That(scoring.Budget, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(scoringObject);
            Object.DestroyImmediate(playerTrashObject);
            Object.DestroyImmediate(patrolTrashObject);
        }
    }

    [Test]
    public void ShopCheaper_AppliesOnlyToConfiguredRewardPath()
    {
        GameObject managerObject = new GameObject("Reward Manager");
        GameObject postersViewObject = new GameObject("Posters View");
        GameObject patrolViewObject = new GameObject("Patrol View");
        BonusDefinition discount = ScriptableObject.CreateInstance<BonusDefinition>();
        ShopItemDefinition posters = ScriptableObject.CreateInstance<ShopItemDefinition>();
        ShopItemDefinition patrol = ScriptableObject.CreateInstance<ShopItemDefinition>();

        try
        {
            RewardManager manager = managerObject.AddComponent<RewardManager>();
            RewardItemView postersView = postersViewObject.AddComponent<RewardItemView>();
            RewardItemView patrolView = patrolViewObject.AddComponent<RewardItemView>();

            SetField(discount, "category", BonusCategory.Shop);
            SetField(discount, "effectType", BonusEffectType.ShopCheaper);
            SetField(discount, "shopTargetPath", RewardPath.Posters);
            SetField(discount, "flatValue", 2);
            SetField(posters, "path", RewardPath.Posters);
            SetField(posters, "cost", 10);
            SetField(patrol, "path", RewardPath.RecyclingPatrol);
            SetField(patrol, "cost", 25);
            SetField(manager, "activeBonuses", new List<BonusDefinition> { discount });

            postersView.Configure(posters);
            patrolView.Configure(patrol);

            Assert.That(manager.GetFinalCost(postersView), Is.EqualTo(8));
            Assert.That(manager.GetFinalCost(patrolView), Is.EqualTo(25));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(postersViewObject);
            Object.DestroyImmediate(patrolViewObject);
            Object.DestroyImmediate(discount);
            Object.DestroyImmediate(posters);
            Object.DestroyImmediate(patrol);
        }
    }

    [Test]
    public void ShopMoreAwareness_DoesNotCreateAwarenessForZeroBaseItem()
    {
        GameObject managerObject = new GameObject("Reward Manager");
        GameObject patrolViewObject = new GameObject("Patrol View");
        BonusDefinition awarenessBonus = ScriptableObject.CreateInstance<BonusDefinition>();
        RecyclingPatrolDefinition patrol = ScriptableObject.CreateInstance<RecyclingPatrolDefinition>();

        try
        {
            RewardManager manager = managerObject.AddComponent<RewardManager>();
            RewardItemView patrolView = patrolViewObject.AddComponent<RewardItemView>();
            SetField(awarenessBonus, "category", BonusCategory.Shop);
            SetField(awarenessBonus, "effectType", BonusEffectType.ShopMoreAwareness);
            SetField(awarenessBonus, "flatValue", 3);
            SetField(patrol, "awarenessValue", 0);
            SetField(manager, "activeBonuses", new List<BonusDefinition> { awarenessBonus });
            patrolView.Configure(patrol);

            Assert.That(manager.GetFinalAwarenessValue(patrolView), Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(patrolViewObject);
            Object.DestroyImmediate(awarenessBonus);
            Object.DestroyImmediate(patrol);
        }
    }

    private static void SetField(object target, string fieldName, object value)
    {
        for (System.Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                continue;
            }

            field.SetValue(target, value);
            return;
        }

        Assert.Fail($"Field '{fieldName}' was not found on {target.GetType().Name}.");
    }
}
#endif
