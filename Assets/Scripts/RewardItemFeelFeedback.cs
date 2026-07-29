using System;
using MoreMountains.Feedbacks;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardItemFeelFeedback : MonoBehaviour
{
    [Header("Feel Players")]
    [SerializeField] private MMF_Player acceptedFeedback;
    [SerializeField] private MMF_Player rejectedFeedback;

    private Action pendingAcceptedCompletion;
    private bool waitingForAcceptedFeedback;
    private bool previousTriggerMMFeedbacksEvents;

    public void PlayAccepted(Action completed = null)
    {
        CancelPendingAccepted();

        if (acceptedFeedback == null || !isActiveAndEnabled)
        {
            completed?.Invoke();
            return;
        }

        pendingAcceptedCompletion = completed;
        EnsureAcceptedFeedbackEvents();
        previousTriggerMMFeedbacksEvents = acceptedFeedback.Events.TriggerMMFeedbacksEvents;
        acceptedFeedback.Events.TriggerMMFeedbacksEvents = true;
        waitingForAcceptedFeedback = true;
        MMFeedbacksEvent.Register(HandleFeedbackEvent);
        acceptedFeedback.PlayFeedbacks();

        if (!acceptedFeedback.IsPlaying)
        {
            CompleteAccepted();
        }
    }

    public void PlayRejected()
    {
        if (rejectedFeedback != null && isActiveAndEnabled)
        {
            rejectedFeedback.PlayFeedbacks();
        }
    }

    private void OnDisable()
    {
        CancelPendingAccepted();
    }

    private void HandleFeedbackEvent(MMFeedbacks source, MMFeedbacksEvent.EventTypes eventType)
    {
        if (source == acceptedFeedback && eventType == MMFeedbacksEvent.EventTypes.Complete)
        {
            CompleteAccepted();
        }
    }

    private void CompleteAccepted()
    {
        StopWaitingForAcceptedFeedback();
        InvokePendingAcceptedCompletion();
    }

    private void CancelPendingAccepted()
    {
        StopWaitingForAcceptedFeedback();
        pendingAcceptedCompletion = null;
    }

    private void StopWaitingForAcceptedFeedback()
    {
        if (!waitingForAcceptedFeedback)
        {
            return;
        }

        MMFeedbacksEvent.Unregister(HandleFeedbackEvent);
        waitingForAcceptedFeedback = false;

        if (acceptedFeedback != null && acceptedFeedback.Events != null)
        {
            acceptedFeedback.Events.TriggerMMFeedbacksEvents = previousTriggerMMFeedbacksEvents;
        }
    }

    private void EnsureAcceptedFeedbackEvents()
    {
        if (acceptedFeedback.Events == null)
        {
            acceptedFeedback.Events = new MMFeedbacksEvents();
            acceptedFeedback.Events.Initialization();
        }
    }

    private void InvokePendingAcceptedCompletion()
    {
        Action completed = pendingAcceptedCompletion;
        pendingAcceptedCompletion = null;
        completed?.Invoke();
    }
}
