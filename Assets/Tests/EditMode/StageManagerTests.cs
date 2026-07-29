#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

public class StageManagerTests
{
    [Test]
    public void InitializeStages_ActivatesOnlyConfiguredStartAndSetsCameraPriorities()
    {
        GameObject managerObject = new GameObject("Stage Manager");
        GameObject beach = new GameObject("Beach");
        GameObject river = new GameObject("River");

        try
        {
            managerObject.SetActive(false);
            StageManager manager = managerObject.AddComponent<StageManager>();
            CinemachineCamera beachCamera = beach.AddComponent<CinemachineCamera>();
            CinemachineCamera riverCamera = river.AddComponent<CinemachineCamera>();
            List<StageManager.StageEntry> stages = new()
            {
                CreateStage(beach, beachCamera),
                CreateStage(river, riverCamera)
            };

            SetField(manager, "stages", stages);
            SetField(manager, "startingStageIndex", 1);
            SetField(manager, "activeCameraPriority", 20);
            SetField(manager, "inactiveCameraPriority", -10);

            manager.InitializeStages();

            Assert.That(manager.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(manager.StageCount, Is.EqualTo(2));
            Assert.That(manager.HasNextStage, Is.False);
            Assert.That(beach.activeSelf, Is.False);
            Assert.That(river.activeSelf, Is.True);
            Assert.That((int)beachCamera.Priority, Is.EqualTo(-10));
            Assert.That((int)riverCamera.Priority, Is.EqualTo(20));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(beach);
            Object.DestroyImmediate(river);
        }
    }

    [Test]
    public void InitializeStages_ClampsStartIndexToLastStage()
    {
        GameObject managerObject = new GameObject("Stage Manager");
        GameObject beach = new GameObject("Beach");
        GameObject river = new GameObject("River");

        try
        {
            managerObject.SetActive(false);
            StageManager manager = managerObject.AddComponent<StageManager>();
            List<StageManager.StageEntry> stages = new()
            {
                CreateStage(beach, beach.AddComponent<CinemachineCamera>()),
                CreateStage(river, river.AddComponent<CinemachineCamera>())
            };

            SetField(manager, "stages", stages);
            SetField(manager, "startingStageIndex", 99);

            manager.InitializeStages();

            Assert.That(manager.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(beach.activeSelf, Is.False);
            Assert.That(river.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(beach);
            Object.DestroyImmediate(river);
        }
    }

    [Test]
    public void GoToNextStage_StartsZoomOutBeforeSwitchingCamera()
    {
        GameObject gameManagerObject = null;
        GameObject managerObject = new GameObject("Stage Manager");
        GameObject beachRoot = new GameObject("Beach Root");
        GameObject riverRoot = new GameObject("River Root");
        GameObject beachCameraObject = new GameObject("Beach Camera");
        GameObject riverCameraObject = new GameObject("River Camera");
        GameManager gameManager = GameManager.Instance;

        try
        {
            if (gameManager == null)
            {
                gameManagerObject = new GameObject("Game Manager");
                gameManager = gameManagerObject.AddComponent<GameManager>();
            }

            managerObject.SetActive(false);
            StageManager manager = managerObject.AddComponent<StageManager>();
            beachCameraObject.AddComponent<Camera>();
            riverCameraObject.AddComponent<Camera>();
            CinemachineCamera beachCamera = beachCameraObject.AddComponent<CinemachineCamera>();
            CinemachineCamera riverCamera = riverCameraObject.AddComponent<CinemachineCamera>();
            List<StageManager.StageEntry> stages = new()
            {
                CreateStage(beachRoot, beachCamera),
                CreateStage(riverRoot, riverCamera)
            };

            SetField(manager, "stages", stages);
            SetField(manager, "zoomOutDuration", 0.1f);
            SetField(manager, "zoomInDuration", 0.1f);
            SetField(manager, "zoomFieldOfViewOffset", 20f);
            managerObject.SetActive(true);

            Assert.That(manager.GoToNextStage(), Is.True);
            Assert.That(manager.IsTransitioning, Is.True);
            Assert.That(manager.CurrentStageIndex, Is.EqualTo(0));
            Assert.That(beachRoot.activeSelf, Is.True);
            Assert.That(riverRoot.activeSelf, Is.True);
            Assert.That(beachCameraObject.activeSelf, Is.True);
            Assert.That(riverCameraObject.activeSelf, Is.False);
            Assert.That((int)beachCamera.Priority, Is.EqualTo(10));
            Assert.That((int)riverCamera.Priority, Is.EqualTo(0));
            Assert.That(gameManager.IsPaused, Is.True);
            Assert.That(manager.GoToNextStage(), Is.False);

        }
        finally
        {
            if (gameManager != null && gameManager.IsPaused)
            {
                gameManager.ResumeGame();
            }

            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(beachRoot);
            Object.DestroyImmediate(riverRoot);
            Object.DestroyImmediate(beachCameraObject);
            Object.DestroyImmediate(riverCameraObject);

            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }
        }
    }

    private static StageManager.StageEntry CreateStage(
        GameObject root,
        CinemachineCamera camera)
    {
        StageManager.StageEntry stage = new StageManager.StageEntry();
        SetField(stage, "root", root);
        SetField(stage, "camera", camera);
        return stage;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
    }
}
#endif
