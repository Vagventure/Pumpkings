#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TrashWindTests
{
    [Test]
    public void PrepareForSpawn_MovableTrashClearsCollectionAndRandomizesZRotation()
    {
        GameObject gameObject = new GameObject("Movable Trash");

        try
        {
            Trash trash = gameObject.AddComponent<Trash>();
            SetField(trash, "isMovable", true);
            trash.SetBeingCollected(true);
            gameObject.transform.localRotation = Quaternion.Euler(10f, 20f, 0f);
            Random.InitState(4321);

            trash.PrepareForSpawn();

            Assert.That(trash.IsBeingCollected, Is.False);
            Assert.That(trash.IsMovable, Is.True);
            Assert.That(gameObject.transform.localEulerAngles.z, Is.InRange(0f, 360f));
            Assert.That(gameObject.transform.localEulerAngles.z, Is.Not.EqualTo(0f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void PrepareForSpawn_StaticTrashPreservesRotation()
    {
        GameObject gameObject = new GameObject("Static Trash");

        try
        {
            Trash trash = gameObject.AddComponent<Trash>();
            gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, 47f);

            trash.PrepareForSpawn();

            Assert.That(gameObject.transform.localEulerAngles.z, Is.EqualTo(47f).Within(0.0001f));
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
