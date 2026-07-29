#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SpawnServiceWindIntegrationTests
{
    [Test]
    public void DirectionalBurst_FirstSpawnIsImmediateAtUpwindEdge()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.DirectionalBurst,
            perTypeLimit: 3,
            globalLimit: 10,
            eventSpawnCount: 3,
            durationRange: new Vector2(0.1f, 0.1f));

        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.PositiveX, 0.2f);

        Assert.That(fixture.Spawned.Count, Is.EqualTo(1));
        Vector3 localPoint = fixture.SpawnArea.InverseTransformPoint(
            fixture.Spawned[0].transform.position);
        Assert.That(localPoint.x, Is.InRange(-0.5f, -0.4f));
    }

    [Test]
    public void DirectionalSpawnCount_IsTotalBudgetAcrossWindBursts()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.DirectionalBurst,
            perTypeLimit: 3,
            globalLimit: 10,
            eventSpawnCount: 1);

        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.PositiveX, 0.2f);
        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.PositiveZ, 0.2f);

        Assert.That(fixture.Spawned.Count, Is.EqualTo(1));
    }

    [Test]
    public void BeginSpawnEvent_ResetsDirectionalSpawnBudget()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.DirectionalBurst,
            perTypeLimit: 3,
            globalLimit: 10,
            eventSpawnCount: 1);

        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.PositiveX, 0.2f);
        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.PositiveZ, 0.2f);
        fixture.Service.BeginSpawnEvent(SpawnTrigger.WindSpawnTrigger);
        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.NegativeX, 0.2f);

        Assert.That(fixture.Spawned.Count, Is.EqualTo(2));
    }

    [Test]
    public void WindController_EachGustResetsDirectionalSpawnBudget()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.DirectionalBurst,
            perTypeLimit: 2,
            globalLimit: 2,
            eventSpawnCount: 1,
            durationRange: new Vector2(0.1f, 0.1f));
        GameObject controllerObject = new GameObject("Wind Controller");

        try
        {
            WindEventController controller = controllerObject.AddComponent<WindEventController>();
            SetField(controller, "spawnService", fixture.Service);

            controller.ActivateWind();
            controller.WindSpawnAndMovementEvent(0.2f);
            controller.WindSpawnAndMovementEvent(0.2f);

            Assert.That(fixture.Spawned.Count, Is.EqualTo(2));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void InstantEventSpawn_RespectsPerTypeLimit()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.Instant,
            perTypeLimit: 2,
            globalLimit: 10,
            instantCount: 3);

        SpawnTriggerEvents.Raise(SpawnTrigger.WaveSpawnTrigger);

        Assert.That(fixture.Spawned.Count, Is.EqualTo(2));
        Assert.That(fixture.Service.ActiveTrashCount, Is.EqualTo(2));
    }

    [Test]
    public void DirectionalBurst_DoesNotStartWhileGameplayIsPaused()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.DirectionalBurst,
            perTypeLimit: 2,
            globalLimit: 2,
            eventSpawnCount: 2,
            durationRange: new Vector2(0.1f, 0.1f));
        GameManager gameManager = GameManager.Instance;
        GameObject createdGameManager = null;

        if (gameManager == null)
        {
            createdGameManager = new GameObject("Test Game Manager");
            gameManager = createdGameManager.AddComponent<GameManager>();
        }

        try
        {
            Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.Running));
            gameManager.PauseGame();
            SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger, WindDirection.PositiveZ, 0.2f);

            Assert.That(fixture.Spawned.Count, Is.EqualTo(0));
        }
        finally
        {
            if (gameManager != null && gameManager.IsPaused)
            {
                gameManager.ResumeGame();
            }

            if (createdGameManager != null)
            {
                UnityEngine.Object.DestroyImmediate(createdGameManager);
            }
        }
    }

    [Test]
    public void InstantEventSpawn_PreservesImmediateWaveBehavior()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.Instant,
            perTypeLimit: 3,
            globalLimit: 3,
            instantCount: 2);

        SpawnTriggerEvents.Raise(SpawnTrigger.WaveSpawnTrigger);

        Assert.That(fixture.Spawned.Count, Is.EqualTo(2));
    }

    [Test]
    public void InstantEventSpawn_RespectsGlobalLimit()
    {
        using SpawnFixture fixture = SpawnFixture.Create(
            EventSpawnPattern.Instant,
            perTypeLimit: 3,
            globalLimit: 1,
            instantCount: 3);

        SpawnTriggerEvents.Raise(SpawnTrigger.WaveSpawnTrigger);

        Assert.That(fixture.Spawned.Count, Is.EqualTo(1));
        Assert.That(fixture.Service.ActiveTrashCount, Is.EqualTo(1));
    }

    private sealed class SpawnFixture : IDisposable
    {
        private readonly GameObject serviceObject;
        private readonly GameObject prefabObject;
        private readonly SpawnData spawnData;
        private readonly Texture2D texture;
        private readonly Sprite sprite;

        private SpawnFixture(
            GameObject serviceObject,
            GameObject prefabObject,
            SpawnData spawnData,
            Texture2D texture,
            Sprite sprite,
            SpawnService service,
            Transform spawnArea)
        {
            this.serviceObject = serviceObject;
            this.prefabObject = prefabObject;
            this.spawnData = spawnData;
            this.texture = texture;
            this.sprite = sprite;
            Service = service;
            SpawnArea = spawnArea;
            SpawnService.TrashAdded += HandleTrashAdded;
        }

        public SpawnService Service { get; }
        public Transform SpawnArea { get; }
        public List<Trash> Spawned { get; } = new List<Trash>();

        public static SpawnFixture Create(
            EventSpawnPattern pattern,
            int perTypeLimit,
            int globalLimit,
            int eventSpawnCount = 1,
            Vector2? durationRange = null,
            int instantCount = 1)
        {
            GameObject prefabObject = new GameObject("Test Trash Prefab");
            prefabObject.SetActive(false);
            SpriteRenderer renderer = prefabObject.AddComponent<SpriteRenderer>();
            prefabObject.AddComponent<BoxCollider>();
            Trash trash = prefabObject.AddComponent<Trash>();
            SetField(trash, "trashType", TrashType.Bag);
            SetField(trash, "isMovable", true);

            Texture2D texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            renderer.sprite = sprite;

            SpawnData data = ScriptableObject.CreateInstance<SpawnData>();
            SetField(data, "prefab", trash);
            SetField(data, "spawnLimit", perTypeLimit);
            SetField(data, "sprites", new List<Sprite> { sprite });

            GameObject serviceObject = new GameObject("Test Spawn Service");
            serviceObject.SetActive(false);
            GameObject spawnAreaObject = new GameObject("Test Spawn Area");
            spawnAreaObject.transform.SetParent(serviceObject.transform, false);
            BoxCollider spawnAreaCollider = spawnAreaObject.AddComponent<BoxCollider>();
            spawnAreaCollider.size = Vector3.one;
            SpawnService service = serviceObject.AddComponent<SpawnService>();

            Type configType = typeof(SpawnService).GetNestedType(
                "TrashTypeSpawnConfig",
                BindingFlags.NonPublic);
            object config = Activator.CreateInstance(configType);
            SetField(config, "data", data);
            SetField(config, "spawnArea", spawnAreaObject.transform);
            SetField(config, "spawnMode", SpawnMode.EventSpawn);
            SetField(config, "spawnTrigger", pattern == EventSpawnPattern.Instant
                ? SpawnTrigger.WaveSpawnTrigger
                : SpawnTrigger.WindSpawnTrigger);
            SetField(config, "eventSpawnPattern", pattern);
            SetField(config, "eventSpawnCount", pattern == EventSpawnPattern.Instant
                ? instantCount
                : eventSpawnCount);
            SetField(config, "directionalBurstDurationRange", durationRange ?? new Vector2(3f, 4f));
            SetField(config, "directionalEdgeInsetRange", new Vector2(0.05f, 0.1f));

            Array configs = Array.CreateInstance(configType, 1);
            configs.SetValue(config, 0);
            SetField(service, "trashTypes", configs);
            SetField(service, "poolParent", serviceObject.transform);
            SetField(service, "spawnLimit", globalLimit);

            SpawnFixture fixture = new SpawnFixture(
                serviceObject,
                prefabObject,
                data,
                texture,
                sprite,
                service,
                spawnAreaObject.transform);
            serviceObject.SetActive(true);
            return fixture;
        }

        public void Dispose()
        {
            SpawnService.TrashAdded -= HandleTrashAdded;
            UnityEngine.Object.DestroyImmediate(serviceObject);
            UnityEngine.Object.DestroyImmediate(prefabObject);
            UnityEngine.Object.DestroyImmediate(spawnData);
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private void HandleTrashAdded(Trash trash)
        {
            if (trash != null && trash.transform.IsChildOf(serviceObject.transform))
            {
                Spawned.Add(trash);
            }
        }
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
    }
}
#endif
