#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TrashPathFollowerTests
{
    [Test]
    public void Advance_MovesThroughEveryWaypointAndStopsAtTheEnd()
    {
        GameObject pathObject = new GameObject("River Path");
        GameObject trashObject = new GameObject("River Trash");
        TrashPathDefinition definition = ScriptableObject.CreateInstance<TrashPathDefinition>();

        try
        {
            Transform first = CreateWaypoint(pathObject.transform, "0", Vector3.zero);
            Transform second = CreateWaypoint(pathObject.transform, "1", new Vector3(1f, 0f, 0f));
            Transform third = CreateWaypoint(pathObject.transform, "2", new Vector3(1f, 0f, 1f));

            SetField(definition, "movementSpeed", 1f);

            TrashPath path = pathObject.AddComponent<TrashPath>();
            SetField(path, "definition", definition);
            SetField(path, "waypoints", new[] { first, second, third });

            trashObject.AddComponent<Trash>();
            TrashPathFollower follower = trashObject.AddComponent<TrashPathFollower>();

            follower.AssignPath(path);
            Assert.That(trashObject.GetComponent<Trash>().RequiresDynamicPickupTracking, Is.True);

            follower.Advance(2f);

            Assert.That(trashObject.transform.position, Is.EqualTo(third.position));
            Assert.That(follower.IsMoving, Is.False);
            Assert.That(follower.CurrentWaypointIndex, Is.EqualTo(2));
            Assert.That(trashObject.GetComponent<Trash>().RequiresDynamicPickupTracking, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(trashObject);
            Object.DestroyImmediate(pathObject);
        }
    }

    [Test]
    public void Advance_WhileTrashIsBeingCollectedDoesNotMove()
    {
        GameObject pathObject = new GameObject("River Path");
        GameObject trashObject = new GameObject("River Trash");
        TrashPathDefinition definition = ScriptableObject.CreateInstance<TrashPathDefinition>();

        try
        {
            Transform first = CreateWaypoint(pathObject.transform, "0", Vector3.zero);
            Transform second = CreateWaypoint(pathObject.transform, "1", new Vector3(5f, 0f, 0f));

            TrashPath path = pathObject.AddComponent<TrashPath>();
            SetField(path, "definition", definition);
            SetField(path, "waypoints", new[] { first, second });

            Trash trash = trashObject.AddComponent<Trash>();
            TrashPathFollower follower = trashObject.AddComponent<TrashPathFollower>();
            follower.AssignPath(path);
            trash.SetBeingCollected(true);

            follower.Advance(1f);

            Assert.That(trashObject.transform.position, Is.EqualTo(first.position));
            Assert.That(follower.IsMoving, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(trashObject);
            Object.DestroyImmediate(pathObject);
        }
    }

    private static Transform CreateWaypoint(Transform parent, string name, Vector3 position)
    {
        GameObject waypoint = new GameObject(name);
        waypoint.transform.SetParent(parent);
        waypoint.transform.position = position;
        return waypoint.transform;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
    }
}
#endif
