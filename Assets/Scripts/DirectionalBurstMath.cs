using UnityEngine;

public static class DirectionalBurstMath
{
    private const float SpacingJitterFraction = 0.15f;

    public static float GetSpawnTime(
        int spawnIndex,
        int spawnCount,
        float duration,
        float jitterNormalized)
    {
        if (spawnIndex <= 0 || spawnCount <= 1)
        {
            return 0f;
        }

        float safeDuration = Mathf.Max(0f, duration);

        if (spawnIndex >= spawnCount - 1)
        {
            return safeDuration;
        }

        float spacing = safeDuration / (spawnCount - 1);
        float jitter = Mathf.Clamp(jitterNormalized, -1f, 1f)
            * spacing
            * SpacingJitterFraction;

        return Mathf.Clamp((spawnIndex * spacing) + jitter, 0f, safeDuration);
    }
}
