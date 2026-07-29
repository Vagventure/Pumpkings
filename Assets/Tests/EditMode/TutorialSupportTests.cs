#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TutorialSupportTests
{
    [Test]
    public void SetSpawnBlocked_OnlyBlocksSelectedTrashType()
    {
        GameObject gameObject = new GameObject("Spawn Service");

        try
        {
            SpawnService spawnService = gameObject.AddComponent<SpawnService>();

            spawnService.SetSpawnBlocked(TrashType.Bottle, true);

            Assert.That(spawnService.IsSpawnBlocked(TrashType.Bottle), Is.True);
            Assert.That(spawnService.IsSpawnBlocked(TrashType.Bag), Is.False);

            spawnService.SetSpawnBlocked(TrashType.Bottle, false);

            Assert.That(spawnService.IsSpawnBlocked(TrashType.Bottle), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void UnlockShopItem_RaisesCreatedConfiguredView()
    {
        GameObject controllerObject = new GameObject("Shop Controller");
        GameObject prefabObject = new GameObject("Shop Item Prefab", typeof(RectTransform));
        ShopItemDefinition definition = ScriptableObject.CreateInstance<ShopItemDefinition>();
        controllerObject.SetActive(false);

        try
        {
            ShopController shopController = controllerObject.AddComponent<ShopController>();
            RewardItemView prefab = prefabObject.AddComponent<RewardItemView>();
            SetField(shopController, "shopItemPrefab", prefab);

            RewardItemView unlockedView = null;
            shopController.ItemUnlocked += view => unlockedView = view;

            bool unlocked = shopController.UnlockShopItem(definition);

            Assert.That(unlocked, Is.True);
            Assert.That(unlockedView, Is.Not.Null);
            Assert.That(unlockedView.ShopDefinition, Is.SameAs(definition));
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void GetPoint_MapsNormalizedPositionInsideBoxCollider()
    {
        GameObject spawnArea = new GameObject("Spawn Area");

        try
        {
            BoxCollider collider = spawnArea.AddComponent<BoxCollider>();
            collider.center = new Vector3(1f, 2f, 3f);
            collider.size = new Vector3(4f, 6f, 8f);

            Vector3 point = SpawnAreaSampler.GetPoint(
                spawnArea.transform,
                new Vector3(0.25f, 0.5f, 0.75f));

            Assert.That(point, Is.EqualTo(new Vector3(0f, 2f, 5f)));
        }
        finally
        {
            Object.DestroyImmediate(spawnArea);
        }
    }

    [Test]
    public void Play_ReportsSlideAsPlayingUntilEntranceCompletes()
    {
        GameObject root = new GameObject("Shop Item", typeof(RectTransform));
        GameObject animated = new GameObject("Animated Element", typeof(RectTransform));
        animated.transform.SetParent(root.transform, false);

        try
        {
            LayoutItemSlideIn slide = root.AddComponent<LayoutItemSlideIn>();
            SetField(slide, "animatedElement", animated.GetComponent<RectTransform>());
            SetField(slide, "duration", 1f);

            slide.Play();

            Assert.That(slide.IsPlaying, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetField<T>(object target, string fieldName, T value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);
    }
}
#endif
