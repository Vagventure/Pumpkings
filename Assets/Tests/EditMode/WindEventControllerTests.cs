#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WindEventControllerTests
{
    [Test]
    public void CompletedStartWindProgressEvent_SchedulesFirstGust()
    {
        GameObject gameObject = new GameObject("Wind Controller");

        try
        {
            WindEventController controller = gameObject.AddComponent<WindEventController>();
            ProgressEventDefinition definition = new ProgressEventDefinition();
            SetField(definition, "completionEffect", ProgressEventCompletionEffect.StartWind);

            controller.HandleProgressEventCompleted(
                new ProgressEventContext(ProgressMetric.ThreatProduced, 100, definition));

            Assert.That(controller.IsGustScheduled, Is.True);
            Assert.That(controller.LastScheduledDelay, Is.EqualTo(5f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ActivateWind_SchedulesFirstGustAfterConfiguredDelay()
    {
        GameObject gameObject = new GameObject("Wind Controller");

        try
        {
            WindEventController controller = gameObject.AddComponent<WindEventController>();
            SetField(controller, "firstGustDelay", 5f);

            controller.ActivateWind();

            Assert.That(controller.IsGustScheduled, Is.True);
            Assert.That(controller.LastScheduledDelay, Is.EqualTo(5f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void WindEndEvent_SchedulesRepeatInsideConfiguredRange()
    {
        GameObject gameObject = new GameObject("Wind Controller");

        try
        {
            WindEventController controller = gameObject.AddComponent<WindEventController>();
            SetField(controller, "repeatGustDelayRange", new Vector2(15f, 30f));
            Random.InitState(2468);

            controller.WindEndEvent();

            Assert.That(controller.IsGustScheduled, Is.True);
            Assert.That(controller.LastScheduledDelay, Is.InRange(15f, 30f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
    }
}
#endif
