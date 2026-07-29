using UnityEngine;

public class EnvironmentalAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private WindEventController windEventController;

    public void WaveSpawnEvent()
    {
        SpawnTriggerEvents.Raise(SpawnTrigger.WaveSpawnTrigger);
    }

    public void WaveAudioEvent()
    {
        AudioTriggerEvents.Raise(AudioTrigger.WaveAudioTrigger);
    }

    public void WindSpawnEvent()
    {
        SpawnTriggerEvents.Raise(SpawnTrigger.WindSpawnTrigger);
    }

    public void WindSpawnAndMovementEvent(float movementDuration)
    {
        if (windEventController != null)
        {
            windEventController.WindSpawnAndMovementEvent(movementDuration);
        }
    }

    public void WindEndEvent()
    {
        if (windEventController != null)
        {
            windEventController.WindEndEvent();
        }
    }

    public void WindAudioEvent()
    {
        AudioTriggerEvents.Raise(AudioTrigger.WindAudioTrigger);
    }
}
