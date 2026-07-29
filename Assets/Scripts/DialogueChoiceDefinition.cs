using System;
using UnityEngine;

[Serializable]
public class DialogueChoiceDefinition
{
    [SerializeField] private RewardPath rewardPath;
    [SerializeField] private string buttonText;
    [SerializeField, TextArea] private string playerLine;
    [SerializeField] private AudioClip playerVoiceClip;

    [NonSerialized] private RewardItem resolvedReward;

    public RewardPath RewardPath => rewardPath;
    public RewardItem Reward => resolvedReward;
    public AudioClip PlayerVoiceClip => playerVoiceClip;

    public string GetButtonText()
    {
        if (!string.IsNullOrWhiteSpace(buttonText))
        {
            return buttonText;
        }

        return resolvedReward == null ? string.Empty : resolvedReward.Title;
    }

    public string GetPlayerLine()
    {
        if (!string.IsNullOrWhiteSpace(playerLine))
        {
            return playerLine;
        }

        return GetButtonText();
    }

    public static DialogueChoiceDefinition CreateRuntime(RewardItem rewardItem)
    {
        return new DialogueChoiceDefinition
        {
            rewardPath = rewardItem == null ? RewardPath.None : rewardItem.Path,
            resolvedReward = rewardItem
        };
    }

    public DialogueChoiceDefinition Resolve(RewardItem rewardItem)
    {
        return new DialogueChoiceDefinition
        {
            rewardPath = rewardPath,
            resolvedReward = rewardItem,
            buttonText = buttonText,
            playerLine = playerLine,
            playerVoiceClip = playerVoiceClip
        };
    }
}
