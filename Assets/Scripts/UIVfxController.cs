using System;
using System.Collections;
using UnityEngine;

public class UIVfxController : MonoBehaviour
{
    public static event Action OnEventTextRevealStartedSFX;
    public static event Action OnEventTextRevealStoppedSFX;

    public static UIVfxController Instance { get; private set; }

    [Header("Progress Event")]
    [SerializeField, Min(1f)] private float revealCharactersPerSecond = 80f;
    [SerializeField, Min(0f)] private float eventEntranceDuration = 0.45f;
    [SerializeField] private Vector2 eventEntranceOffset = new Vector2(-36f, 0f);
    [SerializeField, Range(0.01f, 1f)] private float eventEntranceStartScale = 0.96f;

    private UIRevealController activeRevealController;
    private Coroutine activeEventReveal;
    private bool activeTextCompletedEarly;
    private bool eventTypingSfxPlaying;

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }
    }

    private void OnEnable()
    {
        EventPresentationEvents.OnEventTextRevealCompleteRequested += HandleEventTextRevealCompleteRequested;
        EventPresentationEvents.OnEventEnded += HandleEventEnded;
    }

    private void OnDisable()
    {
        EventPresentationEvents.OnEventTextRevealCompleteRequested -= HandleEventTextRevealCompleteRequested;
        EventPresentationEvents.OnEventEnded -= HandleEventEnded;
        StopActiveEventReveal(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    public void PlayReveal(UIRevealController revealController, bool revealTextDuringEntrance)
    {
        StopActiveEventReveal(true);

        if (revealController == null)
        {
            return;
        }

        activeRevealController = revealController;
        activeTextCompletedEarly = false;
        activeEventReveal = StartCoroutine(PlayProgressEventReveal(revealController, revealTextDuringEntrance));
    }

    private IEnumerator PlayProgressEventReveal(UIRevealController revealController, bool revealTextDuringEntrance)
    {
        revealController.MarkEntranceRevealInProgress();
        revealController.PrepareRevealTexts(revealTextDuringEntrance);

        RectTransform revealRoot = revealController.RevealRoot;
        bool useFeelEntrance = revealController.EntranceFeedback != null;
        float entranceDuration = GetEntranceDuration(revealController, useFeelEntrance);
        CanvasGroup canvasGroup = useFeelEntrance ? revealController.RevealCanvasGroup : revealController.GetOrCreateRevealCanvasGroup();

        Vector2 originalPosition = revealRoot == null ? Vector2.zero : revealRoot.anchoredPosition;
        Vector2 startPosition = originalPosition + eventEntranceOffset;
        Vector3 originalScale = revealRoot == null ? Vector3.one : revealRoot.localScale;
        Vector3 startScale = originalScale * eventEntranceStartScale;
        float originalAlpha = canvasGroup == null ? 1f : canvasGroup.alpha;

        if (!useFeelEntrance && revealRoot != null)
        {
            revealRoot.anchoredPosition = startPosition;
            revealRoot.localScale = startScale;
        }

        if (!useFeelEntrance && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (useFeelEntrance)
        {
            revealController.EntranceFeedback.PlayFeedbacks();
        }

        float entranceElapsed = 0f;
        float textElapsed = 0f;
        int currentRevealIndex = -1;
        UIRevealController.RevealTextEntry currentRevealEntry = null;
        bool entranceComplete = entranceDuration <= 0f;
        bool textRevealComplete = !revealTextDuringEntrance || revealController.AreRevealTextsFullyVisible;

        if (entranceComplete)
        {
            revealController.MarkEntranceRevealComplete();
        }

        while (!entranceComplete || !textRevealComplete)
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (!entranceComplete)
            {
                entranceElapsed += deltaTime;

                if (!useFeelEntrance)
                {
                    float entranceProgress = entranceDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(entranceElapsed / entranceDuration);
                    ApplyEventEntranceState(
                        revealRoot,
                        canvasGroup,
                        startPosition,
                        originalPosition,
                        startScale,
                        originalScale,
                        originalAlpha,
                        EaseOutCubic(entranceProgress));
                }

                if (entranceElapsed >= entranceDuration)
                {
                    entranceComplete = true;
                    revealController.MarkEntranceRevealComplete();
                }
            }

            if (entranceComplete && revealTextDuringEntrance && !textRevealComplete)
            {
                if (activeTextCompletedEarly)
                {
                    revealController.ShowAllRevealTexts();
                    textRevealComplete = true;
                }
                else
                {
                    if (currentRevealEntry == null
                        || revealController.IsRevealTextFullyVisible(currentRevealEntry))
                    {
                        StopEventTypingSFX();
                        currentRevealIndex = GetNextRevealEntryIndex(revealController, currentRevealIndex + 1);
                        currentRevealEntry = currentRevealIndex < 0
                            ? null
                            : revealController.RevealTextEntries[currentRevealIndex];
                        textElapsed = 0f;

                        if (currentRevealEntry != null
                            && revealController.GetRevealTextCharacterCount(currentRevealEntry) > 0)
                        {
                            StartEventTypingSFX();
                        }
                    }

                    if (currentRevealEntry == null)
                    {
                        textRevealComplete = true;
                        revealController.RefreshRevealTextState();
                    }
                    else
                    {
                        textElapsed += deltaTime;
                        int visibleCharacters = Mathf.FloorToInt(textElapsed * revealCharactersPerSecond);
                        revealController.SetRevealTextVisibleCharacters(currentRevealEntry, visibleCharacters);
                        textRevealComplete = revealController.AreRevealTextsFullyVisible;
                    }
                }

                if (textRevealComplete)
                {
                    StopEventTypingSFX();
                }
            }

            yield return null;
        }

        if (!useFeelEntrance)
        {
            ApplyEventEntranceState(
                revealRoot,
                canvasGroup,
                startPosition,
                originalPosition,
                startScale,
                originalScale,
                originalAlpha,
                1f);
        }

        revealController.MarkEntranceRevealComplete();

        if (!revealTextDuringEntrance && !activeTextCompletedEarly)
        {
            revealController.ShowAllText();
        }

        StopEventTypingSFX();
        activeEventReveal = null;
    }

    private void HandleEventTextRevealCompleteRequested(UIRevealController revealController)
    {
        if (revealController == null)
        {
            return;
        }

        if (revealController != activeRevealController)
        {
            revealController.ShowAllRevealTexts();
            return;
        }

        activeTextCompletedEarly = true;
        revealController.ShowAllRevealTexts();
        StopEventTypingSFX();
    }

    private void HandleEventEnded(EventPresentationResolver resolver)
    {
        StopActiveEventReveal(false);
    }

    private void StopActiveEventReveal(bool showFullText)
    {
        if (activeEventReveal != null)
        {
            StopCoroutine(activeEventReveal);
            activeEventReveal = null;
        }

        if (showFullText && activeRevealController != null)
        {
            activeRevealController.ShowAllRevealTexts();
            activeRevealController.MarkEntranceRevealComplete();
        }

        StopEventTypingSFX();
        activeRevealController = null;
        activeTextCompletedEarly = false;
    }

    private void StartEventTypingSFX()
    {
        if (eventTypingSfxPlaying)
        {
            return;
        }

        eventTypingSfxPlaying = true;
        OnEventTextRevealStartedSFX?.Invoke();
    }

    private void StopEventTypingSFX()
    {
        if (!eventTypingSfxPlaying)
        {
            return;
        }

        eventTypingSfxPlaying = false;
        OnEventTextRevealStoppedSFX?.Invoke();
    }

    private static int GetNextRevealEntryIndex(UIRevealController revealController, int startIndex)
    {
        if (revealController == null)
        {
            return -1;
        }

        var entries = revealController.RevealTextEntries;

        for (int i = Mathf.Max(0, startIndex); i < entries.Count; i++)
        {
            UIRevealController.RevealTextEntry entry = entries[i];

            if (entry != null
                && entry.Reveal
                && !revealController.IsRevealTextFullyVisible(entry))
            {
                return i;
            }
        }

        return -1;
    }

    private float GetEntranceDuration(UIRevealController revealController, bool useFeelEntrance)
    {
        if (!useFeelEntrance || revealController == null || revealController.EntranceFeedback == null)
        {
            return eventEntranceDuration;
        }

        revealController.EntranceFeedback.ComputeCachedTotalDuration();
        float feelDuration = revealController.EntranceFeedback.TotalDuration;

        if (feelDuration <= 0f)
        {
            return eventEntranceDuration;
        }

        return feelDuration;
    }

    private static void ApplyEventEntranceState(
        RectTransform revealRoot,
        CanvasGroup canvasGroup,
        Vector2 startPosition,
        Vector2 originalPosition,
        Vector3 startScale,
        Vector3 originalScale,
        float originalAlpha,
        float progress)
    {
        if (revealRoot != null)
        {
            revealRoot.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                originalPosition,
                progress);
            revealRoot.localScale = Vector3.LerpUnclamped(
                startScale,
                originalScale,
                progress);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, originalAlpha, progress);
        }
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }
}
