#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarControllerTests
{
    [Test]
    public void Advance_SmoothlyRetargetsFromTheCurrentVisualValue()
    {
        GameObject gameObject = new GameObject("Progress Bar");

        try
        {
            Image image = gameObject.AddComponent<Image>();
            ProgressBarController controller = gameObject.AddComponent<ProgressBarController>();
            SetField(controller, "fillImage", image);
            SetField(controller, "transitionDuration", 0.5f);

            controller.SetProgress(0, 100);
            controller.SetProgress(100, 100);
            controller.Advance(0.25f);

            Assert.That(image.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));

            controller.SetProgress(0, 100);
            controller.Advance(0.25f);

            Assert.That(image.fillAmount, Is.EqualTo(0.25f).Within(0.0001f));
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
