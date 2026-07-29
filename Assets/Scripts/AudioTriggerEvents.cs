using System;

public static class AudioTriggerEvents
{
    public static event Action<AudioTrigger> Triggered;

    public static void Raise(AudioTrigger trigger)
    {
        Triggered?.Invoke(trigger);
    }
}
