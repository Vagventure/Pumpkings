using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class UIRevealController : MonoBehaviour
{
    [Serializable]
    public class RevealTextEntry
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private bool reveal = true;

        public TMP_Text Text => text;
        public bool Reveal => reveal;
    }

    [Header("Entrance")]
    [SerializeField] private RectTransform revealRoot;
    [SerializeField] private CanvasGroup revealCanvasGroup;
    [SerializeField] private MMF_Player entranceFeedback;

    [Header("Text Reveal")]
    [SerializeField] private List<RevealTextEntry> revealTextEntries = new();

    private bool areRevealTextsFullyVisible = true;
    private bool isEntranceRevealComplete = true;

    public bool AreRevealTextsFullyVisible => areRevealTextsFullyVisible;
    public bool IsEntranceRevealComplete => isEntranceRevealComplete;
    public bool IsReadyToContinue => areRevealTextsFullyVisible && isEntranceRevealComplete;
    public RectTransform RevealRoot => revealRoot == null ? transform as RectTransform : revealRoot;
    public CanvasGroup RevealCanvasGroup => revealCanvasGroup;
    public MMF_Player EntranceFeedback => entranceFeedback;
    public IReadOnlyList<RevealTextEntry> RevealTextEntries => revealTextEntries;

    public void PrepareRevealTexts(bool revealEnabled)
    {
        ShowAllText();

        if (!revealEnabled)
        {
            areRevealTextsFullyVisible = true;
            return;
        }

        bool hasHiddenText = false;

        for (int i = 0; i < revealTextEntries.Count; i++)
        {
            RevealTextEntry entry = revealTextEntries[i];
            TMP_Text text = GetRevealText(entry);

            if (text == null)
            {
                continue;
            }

            if (entry.Reveal)
            {
                SetTextVisibleCharacters(text, 0);
                hasHiddenText = true;
            }
            else
            {
                ShowFullText(text);
            }
        }

        areRevealTextsFullyVisible = !hasHiddenText;
    }

    public TMP_Text GetRevealText(RevealTextEntry entry)
    {
        return entry == null ? null : entry.Text;
    }

    public bool IsRevealTextFullyVisible(RevealTextEntry entry)
    {
        TMP_Text text = GetRevealText(entry);
        return text == null || IsTextFullyVisible(text);
    }

    public void SetRevealTextVisibleCharacters(RevealTextEntry entry, int visibleCharacters)
    {
        TMP_Text text = GetRevealText(entry);
        SetTextVisibleCharacters(text, visibleCharacters);
        RefreshRevealTextState();
    }

    public int GetRevealTextCharacterCount(RevealTextEntry entry)
    {
        TMP_Text text = GetRevealText(entry);
        return GetTextCharacterCount(text);
    }

    public void ShowRevealText(RevealTextEntry entry)
    {
        TMP_Text text = GetRevealText(entry);
        ShowFullText(text);
        RefreshRevealTextState();
    }

    public void CompleteTextReveal()
    {
        ShowAllRevealTexts();
    }

    public void ShowAllRevealTexts()
    {
        for (int i = 0; i < revealTextEntries.Count; i++)
        {
            RevealTextEntry entry = revealTextEntries[i];

            if (entry != null && entry.Reveal)
            {
                ShowFullText(GetRevealText(entry));
            }
        }

        areRevealTextsFullyVisible = true;
    }

    public void ShowAllText()
    {
        for (int i = 0; i < revealTextEntries.Count; i++)
        {
            ShowFullText(GetRevealText(revealTextEntries[i]));
        }

        areRevealTextsFullyVisible = true;
    }

    public void RefreshRevealTextState()
    {
        for (int i = 0; i < revealTextEntries.Count; i++)
        {
            RevealTextEntry entry = revealTextEntries[i];

            if (entry != null && entry.Reveal && !IsRevealTextFullyVisible(entry))
            {
                areRevealTextsFullyVisible = false;
                return;
            }
        }

        areRevealTextsFullyVisible = true;
    }

    public CanvasGroup GetOrCreateRevealCanvasGroup()
    {
        if (revealCanvasGroup != null)
        {
            return revealCanvasGroup;
        }

        RectTransform root = RevealRoot;

        if (root == null)
        {
            return null;
        }

        revealCanvasGroup = root.GetComponent<CanvasGroup>();

        if (revealCanvasGroup == null)
        {
            revealCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        }

        return revealCanvasGroup;
    }

    public void MarkEntranceRevealInProgress()
    {
        isEntranceRevealComplete = false;
    }

    public void MarkEntranceRevealComplete()
    {
        isEntranceRevealComplete = true;
    }

    private static void SetTextVisibleCharacters(TMP_Text text, int visibleCharacters)
    {
        if (text == null)
        {
            return;
        }

        text.ForceMeshUpdate();
        int characterCount = text.textInfo.characterCount;
        text.maxVisibleCharacters = Mathf.Clamp(visibleCharacters, 0, characterCount);
    }

    private static void ShowFullText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.maxVisibleCharacters = int.MaxValue;
    }

    private static int GetTextCharacterCount(TMP_Text text)
    {
        if (text == null)
        {
            return 0;
        }

        text.ForceMeshUpdate();
        return text.textInfo.characterCount;
    }

    private static bool IsTextFullyVisible(TMP_Text text)
    {
        if (text == null)
        {
            return true;
        }

        text.ForceMeshUpdate();
        return text.maxVisibleCharacters >= text.textInfo.characterCount;
    }
}
