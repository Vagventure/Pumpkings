#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RecyclingPatrolTargetSelectorTests
{
    [Test]
    public void TrySelectNearest_UsesShortestReachablePathAmongEligibleTrashInPatrolArea()
    {
        GameObject areaObject = new GameObject("Patrol Area");
        GameObject shortestObject = new GameObject("Shortest Path");
        GameObject longerObject = new GameObject("Longer Path");
        GameObject outsideObject = new GameObject("Outside Area");
        GameObject playerTargetObject = new GameObject("Player Target");
        GameObject claimedObject = new GameObject("Claimed By Another Patrol");

        try
        {
            BoxCollider area = areaObject.AddComponent<BoxCollider>();
            area.size = new Vector3(10f, 2f, 10f);
            Trash shortest = shortestObject.AddComponent<Trash>();
            Trash longer = longerObject.AddComponent<Trash>();
            Trash outside = outsideObject.AddComponent<Trash>();
            Trash playerTarget = playerTargetObject.AddComponent<Trash>();
            Trash claimedTarget = claimedObject.AddComponent<Trash>();
            shortestObject.transform.position = new Vector3(4f, 0f, 0f);
            longerObject.transform.position = new Vector3(1f, 0f, 0f);
            outsideObject.transform.position = new Vector3(20f, 0f, 0f);
            playerTargetObject.transform.position = new Vector3(2f, 0f, 0f);
            claimedObject.transform.position = new Vector3(3f, 0f, 0f);
            playerTarget.SetBeingCollected(true);

            List<Trash> candidates = new List<Trash> { longer, outside, playerTarget, claimedTarget, shortest };
            HashSet<Trash> claimed = new HashSet<Trash> { claimedTarget };
            Dictionary<Trash, float?> pathLengths = new Dictionary<Trash, float?>
            {
                [shortest] = 3f,
                [longer] = 8f,
                [outside] = 1f,
                [playerTarget] = 2f,
                [claimedTarget] = 1.5f
            };

            bool selected = RecyclingPatrolTargetSelector.TrySelectNearest(
                candidates,
                areaObject.transform,
                claimed,
                trash => pathLengths[trash],
                out Trash target);

            Assert.That(selected, Is.True);
            Assert.That(target, Is.SameAs(shortest));
        }
        finally
        {
            Object.DestroyImmediate(areaObject);
            Object.DestroyImmediate(shortestObject);
            Object.DestroyImmediate(longerObject);
            Object.DestroyImmediate(outsideObject);
            Object.DestroyImmediate(playerTargetObject);
            Object.DestroyImmediate(claimedObject);
        }
    }
}
#endif
