#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LevelControllerProgressEventsTests
{
    [Test]
    public void OnValidate_PreservesProgressEventOrder()
    {
        GameObject gameObject = new GameObject("Level Controller");

        try
        {
            LevelController controller = gameObject.AddComponent<LevelController>();
            ProgressEventDefinition first = CreateProgressEvent(100);
            ProgressEventDefinition second = CreateProgressEvent(25);
            List<ProgressEventDefinition> events = new() { first, second };

            SetAwarenessEvents(controller, events);
            InvokePrivateInstanceMethod(controller, "OnValidate");

            Assert.That(controller.AwarenessEvents[0], Is.SameAs(first));
            Assert.That(controller.AwarenessEvents[1], Is.SameAs(second));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ValidateAndSortProgressEvents_SortsProgressEventsByRequiredValue()
    {
        GameObject gameObject = new GameObject("Level Controller");

        try
        {
            LevelController controller = gameObject.AddComponent<LevelController>();
            ProgressEventDefinition first = CreateProgressEvent(100);
            ProgressEventDefinition second = CreateProgressEvent(25);
            List<ProgressEventDefinition> events = new() { first, second };

            SetAwarenessEvents(controller, events);
            InvokePrivateInstanceMethod(controller, "ValidateAndSortProgressEvents");

            Assert.That(controller.AwarenessEvents[0], Is.SameAs(second));
            Assert.That(controller.AwarenessEvents[1], Is.SameAs(first));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static ProgressEventDefinition CreateProgressEvent(int requiredValue)
    {
        ProgressEventDefinition progressEvent = new ProgressEventDefinition();
        typeof(ProgressEventDefinition)
            .GetField("requiredValue", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(progressEvent, requiredValue);

        return progressEvent;
    }

    private static void SetAwarenessEvents(LevelController controller, List<ProgressEventDefinition> events)
    {
        typeof(LevelController)
            .GetField("awarenessEvents", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, events);
    }

    private static void InvokePrivateInstanceMethod(LevelController controller, string methodName)
    {
        typeof(LevelController)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }
}
#endif
