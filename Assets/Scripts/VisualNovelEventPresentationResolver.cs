using UnityEngine;
using UnityEngine.UI;

public class VisualNovelEventPresentationResolver : EventPresentationResolver
{
    private sealed class SpeakerSlot
    {
        private readonly DialogueSpeakerSide side;
        private readonly GameObject characterRoot;
        private readonly Image portraitImage;
        private readonly SpriteRenderer portraitRenderer;
        private readonly Color activeColor;
        private readonly Color inactiveColor;

        public SpeakerSlot(
            DialogueSpeakerSide side,
            GameObject characterRoot,
            Image portraitImage,
            SpriteRenderer portraitRenderer,
            Color activeColor,
            Color inactiveColor)
        {
            this.side = side;
            this.characterRoot = characterRoot;
            this.portraitImage = portraitImage;
            this.portraitRenderer = portraitRenderer;
            this.activeColor = activeColor;
            this.inactiveColor = inactiveColor;
        }

        public DialogueSpeakerSide Side => side;
        public bool HasPortrait => portraitImage != null || portraitRenderer != null;

        public void Hide()
        {
            if (portraitImage != null)
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }

            if (portraitRenderer != null)
            {
                portraitRenderer.sprite = null;
                portraitRenderer.enabled = false;
            }

            if (characterRoot != null)
            {
                characterRoot.SetActive(false);
            }
        }

        public void Show(Sprite portrait, bool active)
        {
            if (portrait == null)
            {
                Hide();
                return;
            }

            if (characterRoot != null)
            {
                characterRoot.SetActive(true);
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = true;
                portraitImage.color = active ? activeColor : inactiveColor;
            }

            if (portraitRenderer != null)
            {
                portraitRenderer.sprite = portrait;
                portraitRenderer.enabled = true;
                portraitRenderer.color = active ? activeColor : inactiveColor;
            }
        }

        public void SetActiveState(bool active)
        {
            if (portraitImage != null && portraitImage.enabled)
            {
                portraitImage.color = active ? activeColor : inactiveColor;
            }

            if (portraitRenderer != null && portraitRenderer.enabled)
            {
                portraitRenderer.color = active ? activeColor : inactiveColor;
            }
        }
    }

    [Header("Visual Novel Panel")]
    [SerializeField] private GameObject visualNovelPanel;

    private VisualNovelPanelBindings panelBindings;
    private SpeakerSlot leftSlot;
    private SpeakerSlot rightSlot;
    private RewardItemView activeRewardView;
    private System.Action rewardPresentationCompleted;

    public override bool IsPanelController
    {
        get
        {
            ApplyPanelBindings();
            return base.IsPanelController;
        }
    }

    protected override void Awake()
    {
        ApplyPanelBindings();
        base.Awake();
    }

    private void OnValidate()
    {
        ApplyPanelBindings();
    }

    protected override void PreparePresentation(EventDefinition definition)
    {
        if (!ValidatePanelBindings())
        {
            return;
        }

        leftSlot?.Hide();
        rightSlot?.Hide();
        ShowInitialSlotPortraits(definition);
    }

    protected override void BeforePresentLine(EventDialogueLine line)
    {
        ClearSpawnedLines();
        UpdateSpeakerSlot(line, true);
    }

    protected override void AfterPresentLine(EventDialogueLine line, DialogueLineView lineView)
    {
        SetActiveSpeaker(line == null ? DialogueSpeakerSide.Left : line.SpeakerSide);
    }

    protected override void CleanupPresentation()
    {
        leftSlot?.Hide();
        rightSlot?.Hide();
    }

    protected override void SetPresentationRootActive(bool active)
    {
        if (visualNovelPanel == null)
        {
            return;
        }

        if (!active)
        {
            ClearRewardPresentation();
        }

        visualNovelPanel.SetActive(active);

        if (active)
        {
            ApplyPanelBindings();
            RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, visualNovelPanel);
        }
    }

    public override bool TryPresentReward(RewardItem reward, System.Action completed)
    {
        ApplyPanelBindings();

        if (reward == null || panelBindings == null || !panelBindings.HasRewardBindings())
        {
            return false;
        }

        ClearRewardPresentation();
        activeRewardView = Instantiate(panelBindings.RewardPrefab, panelBindings.RewardContainer);
        activeRewardView.gameObject.SetActive(true);
        activeRewardView.Configure(reward);
        panelBindings.ContinueButton.onClick.AddListener(HandleRewardContinue);
        panelBindings.ContinueButton.gameObject.SetActive(true);
        rewardPresentationCompleted = completed;
        RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, visualNovelPanel);
        return true;
    }

    private void HandleRewardContinue()
    {
        if (activeRewardView == null)
        {
            return;
        }

        if (panelBindings != null && panelBindings.ContinueButton != null)
        {
            panelBindings.ContinueButton.interactable = false;
        }

        RewardItemView acceptedView = activeRewardView;
        acceptedView.PlayAcceptedFeedback(() => CompleteRewardContinue(acceptedView));
    }

    private void CompleteRewardContinue(RewardItemView acceptedView)
    {
        if (activeRewardView != acceptedView)
        {
            return;
        }

        System.Action completed = rewardPresentationCompleted;
        ClearRewardPresentation();
        completed?.Invoke();
    }

    private void ClearRewardPresentation()
    {
        if (activeRewardView == null)
        {
            rewardPresentationCompleted = null;
            return;
        }

        if (panelBindings != null && panelBindings.ContinueButton != null)
        {
            panelBindings.ContinueButton.onClick.RemoveListener(HandleRewardContinue);
            panelBindings.ContinueButton.interactable = true;
            panelBindings.ContinueButton.gameObject.SetActive(false);
        }

        Destroy(activeRewardView.gameObject);
        activeRewardView = null;
        rewardPresentationCompleted = null;
    }

    protected override bool ShouldKeepChoicesVisibleForSelectedChoiceLine()
    {
        return false;
    }

    protected override void BeforeShowChoices()
    {
        EventDialogueLine playerChoiceLine = CreatePlayerChoicePresentationLine();

        UpdateSpeakerSlot(playerChoiceLine, true);
        SetActiveSpeaker(CurrentChoicesSide);
    }

    private void ShowInitialSlotPortraits(EventDefinition definition)
    {
        if (definition == null || definition.DialogueLines == null)
        {
            return;
        }

        EventDialogueLine openingLine = null;
        EventDialogueLine firstLeft = null;
        EventDialogueLine firstRight = null;

        for (int i = 0; i < definition.DialogueLines.Count; i++)
        {
            EventDialogueLine line = definition.DialogueLines[i];
            if (line == null)
            {
                continue;
            }

            if (openingLine == null)
            {
                openingLine = line;
            }

            if (line.SpeakerSide == DialogueSpeakerSide.Left && firstLeft == null)
            {
                firstLeft = line;
            }
            else if (line.SpeakerSide == DialogueSpeakerSide.Right && firstRight == null)
            {
                firstRight = line;
            }
        }

        if (HasDialogueChoices)
        {
            EventDialogueLine playerChoiceLine = CreatePlayerChoicePresentationLine();
            if (playerChoiceLine != null && playerChoiceLine.Speaker != null)
            {
                if (playerChoiceLine.SpeakerSide == DialogueSpeakerSide.Left && firstLeft == null)
                {
                    firstLeft = playerChoiceLine;
                }
                else if (playerChoiceLine.SpeakerSide == DialogueSpeakerSide.Right && firstRight == null)
                {
                    firstRight = playerChoiceLine;
                }
            }
        }

        if (firstLeft != null && firstRight != null)
        {
            DialogueSpeakerSide activeSide = openingLine == null ? DialogueSpeakerSide.Left : openingLine.SpeakerSide;
            UpdateSpeakerSlot(firstLeft, activeSide == DialogueSpeakerSide.Left);
            UpdateSpeakerSlot(firstRight, activeSide == DialogueSpeakerSide.Right);
        }
        else if (firstLeft != null)
        {
            UpdateSpeakerSlot(firstLeft, true);
        }
        else if (firstRight != null)
        {
            UpdateSpeakerSlot(firstRight, true);
        }
    }

    private void UpdateSpeakerSlot(EventDialogueLine line, bool active)
    {
        if (line == null || line.Speaker == null)
        {
            return;
        }

        SpeakerSlot slot = GetSlot(line.SpeakerSide);
        if (slot == null || !slot.HasPortrait)
        {
            return;
        }

        slot.Show(line.Speaker.GetPortrait(line.Expression), active);
    }

    private void SetActiveSpeaker(DialogueSpeakerSide activeSide)
    {
        leftSlot?.SetActiveState(activeSide == DialogueSpeakerSide.Left);
        rightSlot?.SetActiveState(activeSide == DialogueSpeakerSide.Right);
    }

    private SpeakerSlot GetSlot(DialogueSpeakerSide side)
    {
        if (leftSlot != null && leftSlot.Side == side)
        {
            return leftSlot;
        }

        if (rightSlot != null && rightSlot.Side == side)
        {
            return rightSlot;
        }

        return side == DialogueSpeakerSide.Left ? leftSlot : rightSlot;
    }

    private void ApplyPanelBindings()
    {
        panelBindings = visualNovelPanel == null
            ? null
            : visualNovelPanel.GetComponent<VisualNovelPanelBindings>();

        if (panelBindings == null)
        {
            ClearPanelBindings();
            return;
        }

        ApplyBindings(
            panelBindings.LineContainer,
            panelBindings.ChoiceContainer,
            panelBindings.NpcLinePrefab,
            panelBindings.PlayerLinePrefab,
            panelBindings.ChoiceSlotPrefab,
            panelBindings.ContinueButton,
            panelBindings.VoiceSource);

        leftSlot = new SpeakerSlot(
            DialogueSpeakerSide.Left,
            panelBindings.LeftCharacterRoot,
            panelBindings.LeftPortraitImage,
            panelBindings.LeftPortraitRenderer,
            panelBindings.ActiveColor,
            panelBindings.InactiveColor);
        rightSlot = new SpeakerSlot(
            DialogueSpeakerSide.Right,
            panelBindings.RightCharacterRoot,
            panelBindings.RightPortraitImage,
            panelBindings.RightPortraitRenderer,
            panelBindings.ActiveColor,
            panelBindings.InactiveColor);
    }

    private void ClearPanelBindings()
    {
        ApplyBindings(null, null, null, null, null, null, null);
        leftSlot = null;
        rightSlot = null;
    }

    private bool ValidatePanelBindings()
    {
        if (visualNovelPanel == null)
        {
            Debug.LogError($"VisualNovelEventPresentationResolver: Visual Novel Panel is not assigned on '{name}'.");
            return false;
        }

        if (panelBindings == null)
        {
            Debug.LogError($"VisualNovelEventPresentationResolver: Visual Novel Panel '{visualNovelPanel.name}' is missing VisualNovelPanelBindings.");
            return false;
        }

        if (!panelBindings.HasRequiredBindings())
        {
            Debug.LogError($"VisualNovelEventPresentationResolver: Visual Novel Panel '{visualNovelPanel.name}' has missing required bindings.");
            return false;
        }

        if (leftSlot == null || !leftSlot.HasPortrait)
        {
            Debug.LogError($"VisualNovelEventPresentationResolver: Visual Novel Panel '{visualNovelPanel.name}' is missing a left portrait binding.");
            return false;
        }

        if (rightSlot == null || !rightSlot.HasPortrait)
        {
            Debug.LogError($"VisualNovelEventPresentationResolver: Visual Novel Panel '{visualNovelPanel.name}' is missing a right portrait binding.");
            return false;
        }

        return true;
    }
}
