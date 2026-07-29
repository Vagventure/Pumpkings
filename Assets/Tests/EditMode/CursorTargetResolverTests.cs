#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CursorTargetResolverTests
{
    [Test]
    public void IsCollectableTrash_ReturnsFalseOnceCollectionBegins()
    {
        GameObject gameObject = new GameObject("Trash");

        try
        {
            Trash trash = gameObject.AddComponent<Trash>();

            Assert.That(CursorTargetResolver.IsCollectableTrash(trash), Is.True);

            trash.SetBeingCollected(true);

            Assert.That(CursorTargetResolver.IsCollectableTrash(trash), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void IsClickableUi_UsesInteractableSelectableState()
    {
        GameObject gameObject = new GameObject("Button");

        try
        {
            Button button = gameObject.AddComponent<Button>();

            Assert.That(CursorTargetResolver.IsClickableUi(gameObject), Is.True);

            button.interactable = false;

            Assert.That(CursorTargetResolver.IsClickableUi(gameObject), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
#endif
