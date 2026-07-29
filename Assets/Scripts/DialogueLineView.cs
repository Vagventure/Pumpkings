using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueLineView : MonoBehaviour
{
    [Serializable]
    private class ColorTarget
    {
        [SerializeField] private Graphic target;
        [SerializeField] private Color currentColor = Color.white;
        [SerializeField] private Color pastColor = Color.gray;

        public void Apply(bool past)
        {
            if (target == null)
            {
                return;
            }

            target.color = past ? pastColor : currentColor;
        }
    }

    [SerializeField] private TMP_Text timestampText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private UIRevealController revealController;
    [SerializeField] private List<ColorTarget> colorTargets = new();

    public UIRevealController RevealController => revealController;

    public void BindLine(string timestamp, string speakerName, string speakerRole, string body, Sprite portrait)
    {
        string normalizedRole = NormalizeSpeakerRole(speakerRole);

        if (timestampText != null)
        {
            timestampText.text = timestamp ?? string.Empty;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = FormatSpeakerLabel(speakerName, normalizedRole);
        }

        if (bodyText != null)
        {
            bodyText.text = body ?? string.Empty;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    public void SetPast(bool past)
    {
        for (int i = 0; i < colorTargets.Count; i++)
        {
            colorTargets[i]?.Apply(past);
        }
    }

    private static string NormalizeSpeakerRole(string speakerRole)
    {
        if (string.IsNullOrWhiteSpace(speakerRole))
        {
            return string.Empty;
        }

        return speakerRole.Trim().TrimStart('[').TrimEnd(']');
    }

    private static string FormatSpeakerLabel(string speakerName, string normalizedRole)
    {
        string normalizedName = speakerName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedRole))
        {
            return normalizedName;
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return $"[{normalizedRole}]";
        }

        return $"{normalizedName} [{normalizedRole}]";
    }
}
