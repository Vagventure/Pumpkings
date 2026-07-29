using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum ProgressEventCompletionEffect
{
    None,
    StartWind,
    GoToNextStage
}

[Serializable]
public class ProgressEventDefinition
{
    [Header("Progress")]
    [FormerlySerializedAs("requiredAwareness")]
    [SerializeField] private int requiredValue = 100;
    [TextArea] [SerializeField] private string goal;

    [Header("Event")]
    [SerializeField] private EventDefinition eventDefinition;
    [SerializeField] private ProgressEventCompletionEffect completionEffect;

    [Header("Rewards")]
    [FormerlySerializedAs("rewards")]
    [HideInInspector]
    [SerializeField] private List<RewardItem> rewardItems = new();
    [SerializeField] private DialogueSpeakerSide choicesSide = DialogueSpeakerSide.Right;
    [SerializeField] private List<DialogueChoiceDefinition> dialogueChoices = new();

    [Header("Music")]
    [SerializeField] private MusicStateDefinition musicStateAfterCompletion;

    public int RequiredValue => requiredValue;
    public string Goal => goal;
    public EventDefinition EventDefinition => eventDefinition;
    public ProgressEventCompletionEffect CompletionEffect => completionEffect;
    public IReadOnlyList<RewardItem> RewardItems => rewardItems;
    public DialogueSpeakerSide ChoicesSide => choicesSide;
    public IReadOnlyList<DialogueChoiceDefinition> DialogueChoices => dialogueChoices;
    public MusicStateDefinition MusicStateAfterCompletion => musicStateAfterCompletion;

    public void Validate()
    {
        requiredValue = Mathf.Max(1, requiredValue);

        if (rewardItems == null)
        {
            rewardItems = new List<RewardItem>();
        }

        if (dialogueChoices == null)
        {
            dialogueChoices = new List<DialogueChoiceDefinition>();
        }
    }
}
