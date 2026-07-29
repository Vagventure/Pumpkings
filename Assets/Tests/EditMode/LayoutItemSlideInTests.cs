#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LayoutItemSlideInTests
{
    [Test]
    public void Play_StartsAnimatedElementAtConfiguredOffsetAndBlocksInteraction()
    {
        GameObject root = new GameObject("Layout Root", typeof(RectTransform), typeof(CanvasGroup));
        GameObject animated = new GameObject("Animated Element", typeof(RectTransform));
        animated.transform.SetParent(root.transform, false);

        try
        {
            LayoutItemSlideIn slide = root.AddComponent<LayoutItemSlideIn>();
            RectTransform animatedRect = animated.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            animatedRect.anchoredPosition = new Vector2(12f, 8f);

            SetField(slide, "animatedElement", animatedRect);
            SetField(slide, "interactionCanvasGroup", canvasGroup);
            SetField(slide, "direction", LayoutItemSlideIn.SlideDirection.LeftToRight);
            SetField(slide, "distance", 80f);
            SetField(slide, "duration", 100f);

            slide.Play();

            Assert.That(animatedRect.anchoredPosition.x, Is.EqualTo(-68f).Within(0.1f));
            Assert.That(animatedRect.anchoredPosition.y, Is.EqualTo(8f).Within(0.1f));
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Play_TopToBottomStartsAnimatedElementAboveItsLayoutPosition()
    {
        GameObject root = new GameObject("Layout Root", typeof(RectTransform));
        GameObject animated = new GameObject("Animated Element", typeof(RectTransform));
        animated.transform.SetParent(root.transform, false);

        try
        {
            LayoutItemSlideIn slide = root.AddComponent<LayoutItemSlideIn>();
            RectTransform animatedRect = animated.GetComponent<RectTransform>();

            SetField(slide, "animatedElement", animatedRect);
            SetField(slide, "direction", LayoutItemSlideIn.SlideDirection.TopToBottom);
            SetField(slide, "distance", 60f);
            SetField(slide, "duration", 100f);

            slide.Play();

            Assert.That(animatedRect.anchoredPosition.x, Is.EqualTo(0f).Within(0.1f));
            Assert.That(animatedRect.anchoredPosition.y, Is.EqualTo(60f).Within(0.1f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetField<T>(LayoutItemSlideIn slide, string fieldName, T value)
    {
        typeof(LayoutItemSlideIn)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(slide, value);
    }
}
#endif
