using UnityEngine;

public static class WindGustMath
{
    private const int DirectionCount = 4;

    public static WindDirection SelectDifferentDirection(WindDirection previous, int alternativeIndex)
    {
        int previousIndex = (int)previous;
        int offset = Mathf.Clamp(alternativeIndex, 0, DirectionCount - 2) + 1;

        return (WindDirection)((previousIndex + offset) % DirectionCount);
    }

    public static Vector3 GetLocalDirection(WindDirection direction)
    {
        return direction switch
        {
            WindDirection.PositiveX => Vector3.right,
            WindDirection.NegativeX => Vector3.left,
            WindDirection.PositiveZ => Vector3.forward,
            WindDirection.NegativeZ => Vector3.back,
            _ => Vector3.right
        };
    }

    public static float GetRemainingMovementFraction(float joinedProgress)
    {
        float clampedProgress = Mathf.Clamp01(joinedProgress);
        float easedProgress = clampedProgress * clampedProgress * (3f - (2f * clampedProgress));

        return 1f - easedProgress;
    }

    public static Vector3 GetLocalDisplacement(
        WindDirection direction,
        float axisSpan,
        float distanceFraction,
        float distanceMultiplier,
        float deviationDegrees)
    {
        Vector3 baseDirection = GetLocalDirection(direction);
        Vector3 deviatedDirection = Quaternion.AngleAxis(deviationDegrees, Vector3.up) * baseDirection;
        float distance = Mathf.Max(0f, axisSpan)
            * Mathf.Max(0f, distanceFraction)
            * Mathf.Max(0f, distanceMultiplier);

        return deviatedDirection.normalized * distance;
    }
}
