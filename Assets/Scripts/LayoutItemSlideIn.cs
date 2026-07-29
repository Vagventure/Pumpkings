using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class LayoutItemSlideIn : MonoBehaviour
{
    public enum SlideDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    [Header("References")]
    [SerializeField] private RectTransform animatedElement;
    [SerializeField] private CanvasGroup interactionCanvasGroup;

    [Header("Slide")]
    [SerializeField] private SlideDirection direction = SlideDirection.LeftToRight;
    [SerializeField, Min(0f)] private float distance = 100f;
    [SerializeField, Min(0f)] private float duration = 0.35f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine activeSlide;
    private Vector2 targetPosition;
    private bool interactionStateCaptured;
    private bool previousInteractable;
    private bool previousBlocksRaycasts;

    public bool IsPlaying => activeSlide != null;

    public void Play()
    {
        StopActiveSlide(true);

        if (animatedElement == null || animatedElement == transform)
        {
            Debug.LogWarning(
                $"{nameof(LayoutItemSlideIn)} on '{name}' needs an Animated Element below the layout root.",
                this);
            return;
        }

        targetPosition = animatedElement.anchoredPosition;
        animatedElement.anchoredPosition = targetPosition + GetStartOffset();
        BlockInteraction();

        if (duration <= 0f || !isActiveAndEnabled)
        {
            CompleteSlide();
            return;
        }

        activeSlide = StartCoroutine(PlaySlide());
    }

    private void OnDisable()
    {
        StopActiveSlide(true);
    }

    private IEnumerator PlaySlide()
    {
        Vector2 startPosition = animatedElement.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float curvedProgress = animationCurve == null ? progress : animationCurve.Evaluate(progress);
            animatedElement.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, curvedProgress);
            yield return null;
        }

        activeSlide = null;
        CompleteSlide();
    }

    private Vector2 GetStartOffset()
    {
        return direction switch
        {
            SlideDirection.LeftToRight => Vector2.left * distance,
            SlideDirection.RightToLeft => Vector2.right * distance,
            SlideDirection.TopToBottom => Vector2.up * distance,
            SlideDirection.BottomToTop => Vector2.down * distance,
            _ => Vector2.zero
        };
    }

    private void BlockInteraction()
    {
        if (interactionCanvasGroup == null)
        {
            return;
        }

        previousInteractable = interactionCanvasGroup.interactable;
        previousBlocksRaycasts = interactionCanvasGroup.blocksRaycasts;
        interactionStateCaptured = true;
        interactionCanvasGroup.interactable = false;
        interactionCanvasGroup.blocksRaycasts = false;
    }

    private void StopActiveSlide(bool complete)
    {
        if (activeSlide != null)
        {
            StopCoroutine(activeSlide);
            activeSlide = null;
        }

        if (complete && animatedElement != null && interactionStateCaptured)
        {
            CompleteSlide();
        }
    }

    private void CompleteSlide()
    {
        if (animatedElement != null)
        {
            animatedElement.anchoredPosition = targetPosition;
        }

        if (interactionCanvasGroup != null && interactionStateCaptured)
        {
            interactionCanvasGroup.interactable = previousInteractable;
            interactionCanvasGroup.blocksRaycasts = previousBlocksRaycasts;
        }

        interactionStateCaptured = false;
    }
}
