using UnityEngine;

public sealed class RecyclingPatrolCooldown
{
    private float durationSeconds;

    public float RemainingSeconds { get; private set; }
    public bool IsActive => RemainingSeconds > 0f;
    public float FillAmount => durationSeconds <= 0f
        ? 0f
        : Mathf.Clamp01(RemainingSeconds / durationSeconds);

    public void Start(float duration)
    {
        durationSeconds = Mathf.Max(0f, duration);
        RemainingSeconds = durationSeconds;
    }

    public void Tick(float deltaTime, bool gameplayActive)
    {
        if (!gameplayActive || !IsActive)
        {
            return;
        }

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
    }
}
