using System;

public static class EventPresentationEvents
{
    public static event Action OnEventButtonClickSFX;
    public static event Action OnEventDurationStopSFX;
    public static event Action<EventPresentationResolver> OnEventEnded;
    public static event Action<UIRevealController> OnEventTextRevealCompleteRequested;

    public static void RaiseEventButtonClickSFX()
    {
        OnEventButtonClickSFX?.Invoke();
    }

    public static void RaiseEventDurationStopSFX()
    {
        OnEventDurationStopSFX?.Invoke();
    }

    public static void RaiseEventEnded(EventPresentationResolver resolver)
    {
        OnEventEnded?.Invoke(resolver);
    }

    public static void RaiseEventTextRevealCompleteRequested(UIRevealController revealController)
    {
        OnEventTextRevealCompleteRequested?.Invoke(revealController);
    }
}
