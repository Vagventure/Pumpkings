using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueSpeakerSide
{
    Left = 0,
    Right = 1
}

[Serializable]
public class EventDialogueLine
{
    [SerializeField] private DialogueSpeakerSide speakerSide;
    [SerializeField] private SpeakerDefinition speaker;
    [SerializeField] private SpeakerExpression expression;
    [SerializeField, TextArea] private string text;
    [SerializeField] private AudioClip voiceClip;

    public DialogueSpeakerSide SpeakerSide => speakerSide;
    public SpeakerDefinition Speaker => speaker;
    public SpeakerExpression Expression => expression;
    public string Text => text;
    public AudioClip VoiceClip => voiceClip;

    public static EventDialogueLine CreateRuntime(
        DialogueSpeakerSide side,
        SpeakerDefinition speakerDefinition,
        SpeakerExpression speakerExpression,
        string body,
        AudioClip clip)
    {
        return new EventDialogueLine
        {
            speakerSide = side,
            speaker = speakerDefinition,
            expression = speakerExpression,
            text = body,
            voiceClip = clip
        };
    }
}

[CreateAssetMenu(fileName = "EventDefinition", menuName = "Pumpkins/Event Definition")]
public class EventDefinition : ScriptableObject
{
    [Header("Dialogue")]
    [SerializeField] private List<EventDialogueLine> dialogueLines = new();

    [Header("Presentation")]
    [SerializeField] private bool revealTextDuringEntrance = true;

    [Header("Player Choice Line")]
    [SerializeField] private SpeakerDefinition playerChoiceSpeaker;
    [SerializeField] private DialogueSpeakerSide playerChoiceSpeakerSide = DialogueSpeakerSide.Right;
    [SerializeField] private SpeakerExpression playerChoiceExpression = SpeakerExpression.Neutral;

    [Header("Reward Choice")]
    [SerializeField] private string rewardTitle;

    public IReadOnlyList<EventDialogueLine> DialogueLines => dialogueLines;
    public bool RevealTextDuringEntrance => revealTextDuringEntrance;
    public SpeakerDefinition PlayerChoiceSpeaker => playerChoiceSpeaker;
    public DialogueSpeakerSide PlayerChoiceSpeakerSide => playerChoiceSpeakerSide;
    public SpeakerExpression PlayerChoiceExpression => playerChoiceExpression;
    public string RewardTitle => rewardTitle;

    private void OnValidate()
    {
        if (dialogueLines == null)
        {
            dialogueLines = new List<EventDialogueLine>();
        }
    }
}
