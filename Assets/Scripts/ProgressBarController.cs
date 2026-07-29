using UnityEngine;
using UnityEngine.UI;

public class ProgressBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.35f;

    private float transitionStart;
    private float transitionTarget;
    private float transitionElapsed;
    private bool hasProgress;
    private bool isTransitioning;

    public float CurrentProgress => fillImage != null ? fillImage.fillAmount : 0f;

    private void Update()
    {
        Advance(Time.unscaledDeltaTime);
    }

    public void SetProgress(int currentValue, int maxValue)
    {
        if (fillImage == null)
        {
            return;
        }

        float progress = 0f;

        if (maxValue > 0)
        {
            progress = (float)currentValue / maxValue;
        }

        progress = Mathf.Clamp01(progress);

        if (!hasProgress || transitionDuration <= 0f)
        {
            fillImage.fillAmount = progress;
            transitionTarget = progress;
            hasProgress = true;
            isTransitioning = false;
            return;
        }

        transitionStart = fillImage.fillAmount;
        transitionTarget = progress;
        transitionElapsed = 0f;
        isTransitioning = !Mathf.Approximately(transitionStart, transitionTarget);
    }

    public void Advance(float unscaledDeltaTime)
    {
        if (!isTransitioning || fillImage == null || unscaledDeltaTime <= 0f)
        {
            return;
        }

        transitionElapsed += unscaledDeltaTime;
        float progress = Mathf.Clamp01(transitionElapsed / transitionDuration);
        fillImage.fillAmount = Mathf.Lerp(transitionStart, transitionTarget, progress);

        if (progress >= 1f)
        {
            isTransitioning = false;
        }
    }

    private void OnValidate()
    {
        transitionDuration = Mathf.Max(0f, transitionDuration);
    }
}
