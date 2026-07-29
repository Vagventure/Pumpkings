#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TrashPickupProgressViewTests
{
    [Test]
    public void Show_DetachesViewFromNonUniformlyScaledTrash()
    {
        GameObject trashObject = new GameObject("Trash");
        GameObject viewObject = new GameObject("Pickup Progress");

        try
        {
            trashObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
            viewObject.transform.SetParent(trashObject.transform, false);
            TrashPickupProgressView view = viewObject.AddComponent<TrashPickupProgressView>();

            view.Show();

            Assert.That(viewObject.transform.parent, Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(viewObject);
            Object.DestroyImmediate(trashObject);
        }
    }
}
#endif
