using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.UI;

public abstract class EventPresentationResolver : MonoBehaviour
{
    private enum DialogueState
    {
        Idle,
        RevealingDialogueLine,
        WaitingForLineContinue,
        WaitingForChoices,
        RevealingPlayerLine,
        WaitingForFinalContinue
    }

    [Header("Lines")]
    [SerializeField] protected Transform lineContainer;
    [SerializeField] private DialogueLineView npcLinePrefab;
    [SerializeField] private DialogueLineView playerLinePrefab;

    [Header("Choices")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private DialogueChoiceView choiceSlotPrefab;
    [SerializeField] private DialogueChoiceView[] choiceSlots;

    [Header("Controls")]
    [FormerlySerializedAs("continueButtonPrefab")]
    [SerializeField] private Button continueButton;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    private readonly List<DialogueLineView> spawnedLines = new();
    private readonly List<DialogueChoiceView> runtimeChoiceSlots = new();
    private readonly List<DialogueChoiceView> availableChoiceSlots = new();

    private DialogueLineView inlineLineView;
    private Action eventFinished;
    private Action<RewardItem> rewardSelected;
    private EventDefinition currentDefinition;
    private IReadOnlyList<DialogueChoiceDefinition> currentChoices = Array.Empty<DialogueChoiceDefinition>();
    private DialogueSpeakerSide currentChoicesSide = DialogueSpeakerSide.Right;
    private DialogueLineView activeLineView;
    private DialogueChoiceDefinition selectedChoice;
    private RewardItem selectedReward;
    private string currentTimestamp;
    private int visibleChoiceCount;
    private int currentLineIndex;
    private bool isRunning;
    private bool keepPresentationRootActiveAfterEnd;
    private DialogueState state;

    public bool IsRunning => isRunning;
    public EventDefinition Definition => currentDefinition;
    public UIRevealController RevealController => activeLineView == null ? null : activeLineView.RevealController;
    public virtual bool IsPanelController => lineContainer != null;
    protected bool HasDialogueChoices => currentChoices != null && currentChoices.Count > 0;
    protected DialogueSpeakerSide CurrentChoicesSide => currentChoicesSide;

    protected void ApplyBindings(
        Transform resolvedLineContainer,
        Transform resolvedChoiceContainer,
        DialogueLineView resolvedNpcLinePrefab,
        DialogueLineView resolvedPlayerLinePrefab,
        DialogueChoiceView resolvedChoiceSlotPrefab,
        Button resolvedContinueButton,
        AudioSource resolvedVoiceSource)
    {
        lineContainer = resolvedLineContainer;
        choiceContainer = resolvedChoiceContainer;
        npcLinePrefab = resolvedNpcLinePrefab;
        playerLinePrefab = resolvedPlayerLinePrefab;
        choiceSlotPrefab = resolvedChoiceSlotPrefab;
        continueButton = resolvedContinueButton;
        voiceSource = resolvedVoiceSource;
    }

    protected virtual void Awake()
    {
        CacheInlineLineView();
        BindContinueButton();
        HideContinueButton();
        ClearChoiceSlots();
    }

    protected virtual void Update()
    {
        if (!isRunning)
        {
            return;
        }

        RefreshDialogueState();

        Keyboard keyboard = Keyboard.current;

        if (WasPressedThisFrame(keyboard?.spaceKey) && state != DialogueState.WaitingForChoices)
        {
            Continue();
            return;
        }

        if (state != DialogueState.WaitingForChoices)
        {
            return;
        }

        if (WasPressedThisFrame(keyboard?.digit1Key) || WasPressedThisFrame(keyboard?.numpad1Key))
        {
            SelectChoice(0);
        }
        else if (WasPressedThisFrame(keyboard?.digit2Key) || WasPressedThisFrame(keyboard?.numpad2Key))
        {
            SelectChoice(1);
        }
        else if (WasPressedThisFrame(keyboard?.digit3Key) || WasPressedThisFrame(keyboard?.numpad3Key))
        {
            SelectChoice(2);
        }
    }

    private static bool WasPressedThisFrame(ButtonControl button)
    {
        return button != null && button.wasPressedThisFrame;
    }

    public void StartEvent(EventDefinition definition, Action onEventFinished)
    {
        StartEvent(
            definition,
            Array.Empty<DialogueChoiceDefinition>(),
            DialogueSpeakerSide.Right,
            null,
            onEventFinished);
    }

    public void StartEvent(
        EventDefinition definition,
        IReadOnlyList<DialogueChoiceDefinition> dialogueChoices,
        DialogueSpeakerSide choicesSide,
        Action<RewardItem> onRewardSelected,
        Action onEventFinished)
    {
        CacheInlineLineView();
        currentDefinition = definition;
        currentChoices = dialogueChoices ?? Array.Empty<DialogueChoiceDefinition>();
        currentChoicesSide = choicesSide;
        rewardSelected = onRewardSelected;
        eventFinished = onEventFinished;
        selectedChoice = null;
        selectedReward = null;
        visibleChoiceCount = 0;
        currentLineIndex = -1;
        currentTimestamp = DialogueHistoryRuntime.GetNextTimestamp();
        keepPresentationRootActiveAfterEnd = false;
        state = DialogueState.Idle;
        isRunning = true;

        gameObject.SetActive(true);
        SetPresentationRootActive(true);
        BindContinueButton();
        ClearAuthoredDialogChildren();
        ClearSpawnedLines();
        ClearChoiceSlots();
        PreparePresentation(definition);
        ShowNextDialogueLineOrChoices();
    }

    public void Continue()
    {
        if (!isRunning)
        {
            return;
        }

        RefreshDialogueState();
        EventPresentationEvents.RaiseEventButtonClickSFX();

        if (TryCompleteActiveReveal())
        {
            return;
        }

        RefreshDialogueState();

        if (state == DialogueState.WaitingForLineContinue)
        {
            ShowNextDialogueLineOrChoices();
            return;
        }

        if (state == DialogueState.WaitingForFinalContinue)
        {
            CommitEventSelection();
            EndEvent();
        }
    }

    public void EndEvent()
    {
        if (!isRunning)
        {
            return;
        }

        bool keepPresentationRootActive = ShouldKeepPresentationRootActiveAfterEnd();
        StopVoicePlayback();
        UnbindContinueButton();
        isRunning = false;
        state = DialogueState.Idle;
        EventPresentationEvents.RaiseEventDurationStopSFX();
        EventPresentationEvents.RaiseEventEnded(this);
        CleanupPresentation();
        ClearEventContent();

        if (!keepPresentationRootActive)
        {
            HidePresentationRoot();
        }

        Action callback = eventFinished;
        eventFinished = null;
        callback?.Invoke();
    }

    public void HidePresentationRoot()
    {
        if (isRunning)
        {
            return;
        }

        SetPresentationRootActive(false);
        gameObject.SetActive(false);
    }

    public virtual bool TryPresentReward(RewardItem reward, Action completed)
    {
        return false;
    }

    protected virtual void PreparePresentation(EventDefinition definition)
    {
    }

    protected virtual void BeforePresentLine(EventDialogueLine line)
    {
    }

    protected virtual void AfterPresentLine(EventDialogueLine line, DialogueLineView lineView)
    {
    }

    protected virtual void CleanupPresentation()
    {
    }

    protected virtual void SetPresentationRootActive(bool active)
    {
    }

    protected virtual bool ShouldKeepPresentationRootActiveAfterEnd()
    {
        return keepPresentationRootActiveAfterEnd;
    }

    protected virtual void BeforeShowChoices()
    {
    }

    protected virtual bool ShouldShowSelectedChoiceLine()
    {
        return true;
    }

    protected virtual bool ShouldKeepChoicesVisibleForSelectedChoiceLine()
    {
        return true;
    }

    protected EventDialogueLine CreatePlayerChoicePresentationLine()
    {
        return EventDialogueLine.CreateRuntime(
            currentDefinition == null ? DialogueSpeakerSide.Right : currentDefinition.PlayerChoiceSpeakerSide,
            currentDefinition == null ? null : currentDefinition.PlayerChoiceSpeaker,
            currentDefinition == null ? SpeakerExpression.Neutral : currentDefinition.PlayerChoiceExpression,
            string.Empty,
            null);
    }

    protected virtual DialogueLineView PresentLine(EventDialogueLine line, bool past)
    {
        bool isPlayer = line != null && line.SpeakerSide == DialogueSpeakerSide.Right;
        DialogueLineView prefab = ResolveLinePrefab(isPlayer);
        string speakerName = line == null || line.Speaker == null ? string.Empty : line.Speaker.DisplayName;
        string speakerRole = line == null || line.Speaker == null ? string.Empty : line.Speaker.Role;
        string body = line == null ? string.Empty : line.Text;
        Sprite portrait = line == null || line.Speaker == null ? null : line.Speaker.GetPortrait(line.Expression);

        if (prefab == null)
        {
            DialogueLineView inlineView = TryBindInlineLineView(speakerName, speakerRole, body, portrait, past);
            if (inlineView != null)
            {
                return inlineView;
            }

            Debug.LogWarning(
                $"EventPresentationResolver: Missing {(isPlayer ? "right" : "left")} line prefab and no inline DialogueLineView fallback is available on '{name}'.");
            return null;
        }

        Transform parent = lineContainer == null ? transform : lineContainer;
        DialogueLineView lineView = Instantiate(prefab, parent);
        lineView.transform.SetAsLastSibling();
        DisableNestedResolvers(lineView);
        lineView.BindLine(currentTimestamp, speakerName, speakerRole, body, portrait);
        lineView.SetPast(past);
        ForceRebuildLineLayout(lineView);

        if (lineView.RevealController != null)
        {
            lineView.RevealController.ShowAllText();
            lineView.RevealController.MarkEntranceRevealComplete();
        }

        spawnedLines.Add(lineView);
        return lineView;
    }

    protected void MarkSpawnedLinesPast()
    {
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            spawnedLines[i]?.SetPast(true);
        }
    }

    protected void ClearSpawnedLines()
    {
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            if (spawnedLines[i] != null)
            {
                spawnedLines[i].gameObject.SetActive(false);
                Destroy(spawnedLines[i].gameObject);
            }
        }

        spawnedLines.Clear();
        activeLineView = null;
    }

    private void ShowNextDialogueLineOrChoices()
    {
        IReadOnlyList<EventDialogueLine> lines = currentDefinition == null
            ? Array.Empty<EventDialogueLine>()
            : currentDefinition.DialogueLines;

        currentLineIndex++;

        if (lines == null || currentLineIndex >= lines.Count)
        {
            ShowChoices();
            return;
        }

        EventDialogueLine line = lines[currentLineIndex];
        BeforePresentLine(line);
        activeLineView = PresentLine(line, false);
        AfterPresentLine(line, activeLineView);
        state = DialogueState.RevealingDialogueLine;
        ShowContinueButton();
        BeginReveal(line, activeLineView);
    }

    private void ShowPlayerLine()
    {
        if (selectedChoice == null)
        {
            state = DialogueState.WaitingForFinalContinue;
            ShowContinueButton();
            return;
        }

        EventDialogueLine line = EventDialogueLine.CreateRuntime(
            currentDefinition == null ? DialogueSpeakerSide.Right : currentDefinition.PlayerChoiceSpeakerSide,
            currentDefinition == null ? null : currentDefinition.PlayerChoiceSpeaker,
            currentDefinition == null ? SpeakerExpression.Neutral : currentDefinition.PlayerChoiceExpression,
            selectedChoice.GetPlayerLine(),
            selectedChoice.PlayerVoiceClip);

        BeforePresentLine(line);
        activeLineView = PresentLine(line, false);
        AfterPresentLine(line, activeLineView);
        state = DialogueState.RevealingPlayerLine;
        ShowContinueButton();
        BeginReveal(line, activeLineView, true);
    }

    private void DisableNestedResolvers(DialogueLineView lineView)
    {
        if (lineView == null)
        {
            return;
        }

        EventPresentationResolver[] resolvers = lineView.GetComponentsInChildren<EventPresentationResolver>(true);

        for (int i = 0; i < resolvers.Length; i++)
        {
            EventPresentationResolver resolver = resolvers[i];
            if (resolver != null && resolver != this)
            {
                resolver.enabled = false;
            }
        }
    }

    private DialogueLineView ResolveLinePrefab(bool isPlayer)
    {
        if (isPlayer)
        {
            return playerLinePrefab != null ? playerLinePrefab : npcLinePrefab;
        }

        return npcLinePrefab != null ? npcLinePrefab : playerLinePrefab;
    }

    private DialogueLineView TryBindInlineLineView(
        string speakerName,
        string speakerRole,
        string body,
        Sprite portrait,
        bool past)
    {
        CacheInlineLineView();

        if (inlineLineView == null || past)
        {
            return null;
        }

        inlineLineView.BindLine(currentTimestamp, speakerName, speakerRole, body, portrait);
        inlineLineView.SetPast(false);
        ForceRebuildLineLayout(inlineLineView);

        if (inlineLineView.RevealController != null)
        {
            inlineLineView.RevealController.ShowAllText();
            inlineLineView.RevealController.MarkEntranceRevealComplete();
        }

        return inlineLineView;
    }

    private static void ForceRebuildLineLayout(DialogueLineView lineView)
    {
        RuntimeUILayoutRefresher.Refresh(lineView);
    }

    private void CacheInlineLineView()
    {
        if (inlineLineView != null)
        {
            return;
        }

        inlineLineView = GetComponent<DialogueLineView>();

        if (inlineLineView != null)
        {
            return;
        }

        DialogueLineView[] lineViews = GetComponentsInChildren<DialogueLineView>(true);
        if (lineViews.Length == 1)
        {
            inlineLineView = lineViews[0];
        }
    }

    private void BeginReveal(EventDialogueLine line, DialogueLineView lineView, bool forceReveal = false)
    {
        StartVoicePlayback(line == null ? null : line.VoiceClip);

        if (lineView == null || lineView.RevealController == null)
        {
            return;
        }

        UIVfxController controller = UIVfxController.Instance;
        bool revealText = forceReveal || currentDefinition == null || currentDefinition.RevealTextDuringEntrance;

        if (controller != null)
        {
            controller.PlayReveal(lineView.RevealController, revealText);
            return;
        }

        lineView.RevealController.PrepareRevealTexts(false);
        lineView.RevealController.MarkEntranceRevealComplete();
    }

    private void RefreshDialogueState()
    {
        if (!isRunning || !IsActiveLineReady())
        {
            return;
        }

        switch (state)
        {
            case DialogueState.RevealingDialogueLine:
                StopVoicePlayback();
                activeLineView = null;
                state = DialogueState.WaitingForLineContinue;
                ShowContinueButton();
                break;
            case DialogueState.RevealingPlayerLine:
                StopVoicePlayback();
                activeLineView = null;
                state = DialogueState.WaitingForFinalContinue;
                ShowContinueButton();
                break;
        }
    }

    private void ShowChoices()
    {
        activeLineView = null;
        ClearChoiceSlots();
        visibleChoiceCount = 0;

        if (currentChoices == null || currentChoices.Count == 0)
        {
            state = DialogueState.WaitingForFinalContinue;
            ShowContinueButton();
            return;
        }

        IReadOnlyList<DialogueChoiceView> availableSlots = GetChoiceSlots(currentChoices.Count);
        int availableSlotCount = availableSlots.Count;
        int visibleChoices = Mathf.Min(currentChoices.Count, availableSlotCount);

        if (visibleChoices < currentChoices.Count)
        {
            Debug.LogWarning("EventPresentationResolver: Not enough choice slots to show all dialogue choices.");
        }

        if (visibleChoices == 0)
        {
            state = DialogueState.WaitingForFinalContinue;
            ShowContinueButton();
            return;
        }

        BeforeShowChoices();

        for (int i = 0; i < visibleChoices; i++)
        {
            int slotIndex = i;
            DialogueChoiceDefinition choice = currentChoices[i];
            string label = choice == null ? string.Empty : choice.GetButtonText();
            availableSlots[i].Configure(i + 1, label, () => SelectChoice(slotIndex));
        }

        visibleChoiceCount = visibleChoices;
        RewardManager.RaiseRewardChoiceShownSFX();
        state = DialogueState.WaitingForChoices;
        HideContinueButton();
        RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, choiceContainer);
    }

    private void SelectChoice(int index)
    {
        if (state != DialogueState.WaitingForChoices
            || currentChoices == null
            || index < 0
            || index >= visibleChoiceCount)
        {
            return;
        }

        selectedChoice = currentChoices[index];
        if (selectedChoice == null)
        {
            return;
        }

        selectedReward = selectedChoice.Reward;

        if (!ShouldShowSelectedChoiceLine())
        {
            ClearChoiceSlots();
            state = DialogueState.WaitingForFinalContinue;
            ShowContinueButton();
            return;
        }

        if (ShouldKeepChoicesVisibleForSelectedChoiceLine())
        {
            MarkChoiceSelection(index);
        }
        else
        {
            ClearChoiceSlots();
        }

        ShowPlayerLine();
    }

    private bool TryCompleteActiveReveal()
    {
        UIRevealController controller = RevealController;

        if (controller == null || controller.IsReadyToContinue)
        {
            return false;
        }

        EventPresentationEvents.RaiseEventTextRevealCompleteRequested(controller);

        if (!controller.AreRevealTextsFullyVisible)
        {
            controller.CompleteTextReveal();
        }

        controller.MarkEntranceRevealComplete();
        StopVoicePlayback();
        return true;
    }

    private bool IsActiveLineReady()
    {
        UIRevealController controller = RevealController;
        return controller == null || controller.IsReadyToContinue;
    }

    private void CommitEventSelection()
    {
        keepPresentationRootActiveAfterEnd = selectedReward != null && rewardSelected != null;
        rewardSelected?.Invoke(selectedReward);
        rewardSelected = null;
    }

    private void StartVoicePlayback(AudioClip clip)
    {
        if (voiceSource == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = clip;

        if (clip != null)
        {
            voiceSource.Play();
        }
    }

    private void StopVoicePlayback()
    {
        if (voiceSource == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = null;
    }

    private void BindContinueButton()
    {
        if (!CanUseContinueButton())
        {
            return;
        }

        continueButton.onClick.RemoveListener(Continue);
        continueButton.onClick.AddListener(Continue);
    }

    private void UnbindContinueButton()
    {
        if (!CanUseContinueButton(false))
        {
            return;
        }

        continueButton.onClick.RemoveListener(Continue);
    }

    private void ShowContinueButton()
    {
        if (CanUseContinueButton() && isRunning)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.transform.SetAsLastSibling();
        }
    }

    private void HideContinueButton()
    {
        if (CanUseContinueButton(false))
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    private bool CanUseContinueButton(bool logError = true)
    {
        if (continueButton == null)
        {
            return false;
        }

        if (continueButton.gameObject.scene.IsValid())
        {
            return true;
        }

        if (logError)
        {
            Debug.LogError($"EventPresentationResolver: Continue Button on '{name}' must be a scene object, not a prefab asset.");
        }

        return false;
    }

    private void ClearChoiceSlots()
    {
        if (choiceSlots != null)
        {
            for (int i = 0; i < choiceSlots.Length; i++)
            {
                if (choiceSlots[i] != null)
                {
                    choiceSlots[i].Clear();
                }
            }
        }

        for (int i = 0; i < runtimeChoiceSlots.Count; i++)
        {
            if (runtimeChoiceSlots[i] != null)
            {
                runtimeChoiceSlots[i].Clear();
            }
        }
    }

    private void MarkChoiceSelection(int selectedIndex)
    {
        IReadOnlyList<DialogueChoiceView> availableSlots = GetChoiceSlots(visibleChoiceCount);

        for (int i = 0; i < visibleChoiceCount && i < availableSlots.Count; i++)
        {
            DialogueChoiceView slot = availableSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (i == selectedIndex)
            {
                slot.MarkSelected();
                continue;
            }

            slot.MarkDiscarded();
        }
    }

    private IReadOnlyList<DialogueChoiceView> GetChoiceSlots(int requiredCount)
    {
        PruneRuntimeChoiceSlots();

        if (choiceSlots != null && choiceSlots.Length > 0)
        {
            availableChoiceSlots.Clear();

            for (int i = 0; i < choiceSlots.Length; i++)
            {
                if (choiceSlots[i] != null)
                {
                    availableChoiceSlots.Add(choiceSlots[i]);
                }
            }

            return availableChoiceSlots;
        }

        if (choiceSlotPrefab == null || choiceContainer == null)
        {
            return Array.Empty<DialogueChoiceView>();
        }

        while (runtimeChoiceSlots.Count < requiredCount)
        {
            DialogueChoiceView slot = Instantiate(choiceSlotPrefab, choiceContainer);
            slot.transform.SetAsLastSibling();
            runtimeChoiceSlots.Add(slot);
        }

        return runtimeChoiceSlots;
    }

    private void PruneRuntimeChoiceSlots()
    {
        for (int i = runtimeChoiceSlots.Count - 1; i >= 0; i--)
        {
            if (runtimeChoiceSlots[i] == null)
            {
                runtimeChoiceSlots.RemoveAt(i);
            }
        }
    }

    private void ClearAuthoredDialogChildren()
    {
        ClearAuthoredChildren(lineContainer);

        if (choiceContainer != lineContainer)
        {
            ClearAuthoredChildren(choiceContainer);
        }
    }

    private void ClearAuthoredChildren(Transform container)
    {
        if (container == null)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);

            if (continueButton != null && child == continueButton.transform)
            {
                continue;
            }

            DialogueChoiceView choiceView = child.GetComponent<DialogueChoiceView>();
            if (choiceView != null && runtimeChoiceSlots.Contains(choiceView))
            {
                continue;
            }

            if (child.GetComponent<DialogueLineView>() != null || child.GetComponent<DialogueChoiceView>() != null)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private void ClearEventContent()
    {
        HideContinueButton();
        ClearChoiceSlots();
        ClearSpawnedLines();
        activeLineView = null;
        selectedChoice = null;
        selectedReward = null;
        keepPresentationRootActiveAfterEnd = false;
        currentDefinition = null;
        currentChoices = Array.Empty<DialogueChoiceDefinition>();
        currentChoicesSide = DialogueSpeakerSide.Right;
        currentTimestamp = null;
        visibleChoiceCount = 0;
        currentLineIndex = -1;
    }

    protected virtual void OnDestroy()
    {
        StopVoicePlayback();

        if (isRunning)
        {
            EventPresentationEvents.RaiseEventDurationStopSFX();
            EventPresentationEvents.RaiseEventEnded(this);
            isRunning = false;
        }

        UnbindContinueButton();
    }
}
