using UnityEngine;

public static class SpawnAreaSampler
{
    public static Vector3 GetRandomPoint(Transform spawnArea)
    {
        return GetPoint(spawnArea, new Vector3(Random.value, Random.value, Random.value));
    }

    public static Vector3 GetPoint(Transform spawnArea, Vector3 normalizedPosition)
    {
        if (spawnArea == null)
        {
            return Vector3.zero;
        }

        normalizedPosition = new Vector3(
            Mathf.Clamp01(normalizedPosition.x),
            Mathf.Clamp01(normalizedPosition.y),
            Mathf.Clamp01(normalizedPosition.z));

        BoxCollider boxCollider = spawnArea.GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            Vector3 localPoint = boxCollider.center + Vector3.Scale(
                boxCollider.size,
                normalizedPosition - (Vector3.one * 0.5f));

            return spawnArea.TransformPoint(localPoint);
        }

        if (spawnArea is RectTransform rectTransform)
        {
            Rect rect = rectTransform.rect;
            Vector3 localPoint = new Vector3(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPosition.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPosition.y),
                Mathf.Lerp(-0.5f, 0.5f, normalizedPosition.z));

            return rectTransform.TransformPoint(localPoint);
        }

        return spawnArea.TransformPoint(normalizedPosition - (Vector3.one * 0.5f));
    }

    public static Vector3 GetDirectionalEdgePoint(
        Transform spawnArea,
        WindDirection direction,
        Vector2 edgeInsetRange)
    {
        float minimumInset = Mathf.Clamp01(Mathf.Min(edgeInsetRange.x, edgeInsetRange.y));
        float maximumInset = Mathf.Clamp01(Mathf.Max(edgeInsetRange.x, edgeInsetRange.y));
        float inset = Random.Range(minimumInset, maximumInset);
        Vector3 normalizedPosition = new Vector3(Random.value, Random.value, Random.value);

        switch (direction)
        {
            case WindDirection.PositiveX:
                normalizedPosition.x = inset;
                break;
            case WindDirection.NegativeX:
                normalizedPosition.x = 1f - inset;
                break;
            case WindDirection.PositiveZ:
                normalizedPosition.z = inset;
                break;
            case WindDirection.NegativeZ:
                normalizedPosition.z = 1f - inset;
                break;
        }

        return GetPoint(spawnArea, normalizedPosition);
    }

    public static Vector3 ClampPointXZ(Transform spawnArea, Vector3 worldPoint)
    {
        if (spawnArea == null)
        {
            return worldPoint;
        }

        BoxCollider boxCollider = spawnArea.GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            return worldPoint;
        }

        Vector3 localPoint = spawnArea.InverseTransformPoint(worldPoint);
        Vector3 halfSize = boxCollider.size * 0.5f;
        localPoint.x = Mathf.Clamp(
            localPoint.x,
            boxCollider.center.x - halfSize.x,
            boxCollider.center.x + halfSize.x);
        localPoint.z = Mathf.Clamp(
            localPoint.z,
            boxCollider.center.z - halfSize.z,
            boxCollider.center.z + halfSize.z);

        return spawnArea.TransformPoint(localPoint);
    }
}
