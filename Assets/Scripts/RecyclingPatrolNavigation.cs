using UnityEngine;
using UnityEngine.AI;

public static class RecyclingPatrolNavigation
{
    public static bool TryCalculateCompletePath(
        Vector3 origin,
        Vector3 destination,
        float sampleRadius,
        int areaMask,
        NavMeshPath path,
        out float pathLength)
    {
        pathLength = 0f;

        if (path == null
            || !NavMesh.SamplePosition(origin, out NavMeshHit originHit, sampleRadius, areaMask)
            || !NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, sampleRadius, areaMask)
            || !NavMesh.CalculatePath(originHit.position, destinationHit.position, areaMask, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        for (int i = 1; i < path.corners.Length; i++)
        {
            pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return true;
    }
}
