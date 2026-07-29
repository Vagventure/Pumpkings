using System;
using System.Collections.Generic;
using UnityEngine;

public static class RecyclingPatrolTargetSelector
{
    public static bool TrySelectNearest(
        IReadOnlyList<Trash> candidates,
        Transform patrolArea,
        ISet<Trash> claimedTrash,
        Func<Trash, float?> reachablePathLength,
        out Trash target)
    {
        target = null;

        if (candidates == null || patrolArea == null || reachablePathLength == null)
        {
            return false;
        }

        float shortestPathLength = float.PositiveInfinity;

        for (int i = 0; i < candidates.Count; i++)
        {
            Trash candidate = candidates[i];

            if (!IsEligible(candidate, patrolArea, claimedTrash))
            {
                continue;
            }

            float? pathLength = reachablePathLength(candidate);

            if (!pathLength.HasValue
                || pathLength.Value < 0f
                || float.IsNaN(pathLength.Value)
                || float.IsInfinity(pathLength.Value)
                || pathLength.Value >= shortestPathLength)
            {
                continue;
            }

            shortestPathLength = pathLength.Value;
            target = candidate;
        }

        return target != null;
    }

    public static bool IsEligible(Trash trash, Transform patrolArea, ISet<Trash> claimedTrash)
    {
        return trash != null
            && trash.gameObject.activeInHierarchy
            && !trash.IsBeingCollected
            && (claimedTrash == null || !claimedTrash.Contains(trash))
            && IsInsidePatrolArea(patrolArea, trash.transform.position);
    }

    public static bool IsInsidePatrolArea(Transform patrolArea, Vector3 worldPosition)
    {
        if (patrolArea == null || !patrolArea.TryGetComponent(out BoxCollider boxCollider))
        {
            return false;
        }

        Vector3 localPosition = patrolArea.InverseTransformPoint(worldPosition) - boxCollider.center;
        Vector3 halfSize = boxCollider.size * 0.5f;

        return Mathf.Abs(localPosition.x) <= halfSize.x
            && Mathf.Abs(localPosition.z) <= halfSize.z;
    }
}
