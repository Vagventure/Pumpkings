#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class MoneyFlyVfxControllerTests
{
    [Test]
    public void PrepareOverlayIcon_ResetsTiltScaleAndDepth()
    {
        GameObject iconObject = new GameObject("Money Icon", typeof(RectTransform));

        try
        {
            RectTransform icon = iconObject.GetComponent<RectTransform>();
            icon.localRotation = Quaternion.Euler(20f, 30f, 40f);
            icon.localScale = new Vector3(2f, 0.5f, 3f);
            icon.anchoredPosition3D = new Vector3(1f, 2f, 9f);

            MoneyFlyVfxController.PrepareOverlayIcon(icon, new Vector2(10f, 20f));

            Assert.That(Quaternion.Angle(icon.localRotation, Quaternion.identity), Is.LessThan(0.001f));
            Assert.That(icon.localScale, Is.EqualTo(Vector3.one));
            Assert.That(icon.anchoredPosition3D, Is.EqualTo(new Vector3(10f, 20f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(iconObject);
        }
    }
}
#endif
