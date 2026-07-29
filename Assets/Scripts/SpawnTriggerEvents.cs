using System;

public readonly struct SpawnTriggerContext
{
    public SpawnTriggerContext(SpawnTrigger trigger)
    {
        Trigger = trigger;
        Direction = default;
        HasDirection = false;
        Duration = 0f;
        HasDuration = false;
    }

    public SpawnTriggerContext(SpawnTrigger trigger, WindDirection direction, float duration)
    {
        Trigger = trigger;
        Direction = direction;
        HasDirection = true;
        Duration = duration;
        HasDuration = duration > 0f;
    }

    public SpawnTrigger Trigger { get; }
    public WindDirection Direction { get; }
    public bool HasDirection { get; }
    public float Duration { get; }
    public bool HasDuration { get; }
}

public static class SpawnTriggerEvents
{
    public static event Action<SpawnTriggerContext> Triggered;

    public static void Raise(SpawnTrigger trigger)
    {
        Triggered?.Invoke(new SpawnTriggerContext(trigger));
    }

    public static void Raise(SpawnTrigger trigger, WindDirection direction, float duration)
    {
        Triggered?.Invoke(new SpawnTriggerContext(trigger, direction, duration));
    }
}
