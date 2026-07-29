using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Restot.UIBuilder
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UIScrollAutoScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        private const float Epsilon = 0.01f;
        private static readonly Vector3[] Corners = new Vector3[4];

        [SerializeField, HideInInspector] private ScrollRect scrollRect;
        [SerializeField, HideInInspector] private RectTransform viewport;
        [SerializeField, HideInInspector] private RectTransform content;
        [SerializeField, HideInInspector] private float overflowThreshold = 0.8f;
        [SerializeField, HideInInspector] private float followLatestTolerance = 0.1f;
        [SerializeField, HideInInspector] private float previousItemVisibility = 0.5f;
        [SerializeField, HideInInspector] private float autoScrollDuration = 0.3f;
        [SerializeField, HideInInspector] private bool pendingScroll;
        [SerializeField, HideInInspector] private bool pendingFollowLatest;
        [SerializeField, HideInInspector] private bool isAnimating;
        [SerializeField, HideInInspector] private bool userDragging;
        [SerializeField, HideInInspector] private float animationElapsed;
        [SerializeField, HideInInspector] private float animationStartNormalizedPosition;
        [SerializeField, HideInInspector] private float animationTargetNormalizedPosition;

        public void Configure(
            ScrollRect targetScrollRect,
            RectTransform targetViewport,
            RectTransform targetContent,
            float configuredOverflowThreshold,
            float configuredFollowLatestTolerance,
            float configuredPreviousItemVisibility,
            float configuredAutoScrollDuration)
        {
            scrollRect = targetScrollRect;
            viewport = targetViewport;
            content = targetContent;
            overflowThreshold = Mathf.Clamp01(configuredOverflowThreshold);
            followLatestTolerance = Mathf.Clamp01(configuredFollowLatestTolerance);
            previousItemVisibility = Mathf.Clamp01(configuredPreviousItemVisibility);
            autoScrollDuration = Mathf.Max(0f, configuredAutoScrollDuration);
        }

        public void RequestScrollToBottom()
        {
            pendingFollowLatest = isAnimating || IsNearBottom();
            pendingScroll = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            userDragging = true;
            CancelAnimation();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            userDragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            CancelAnimation();
        }

        private void LateUpdate()
        {
            if (pendingScroll)
            {
                ProcessPendingScroll();
            }

            if (!isAnimating)
            {
                return;
            }

            if (userDragging || scrollRect == null || !scrollRect.enabled)
            {
                CancelAnimation();
                return;
            }

            animationElapsed += GetDeltaTime();
            float duration = Mathf.Max(Epsilon, autoScrollDuration);
            float t = autoScrollDuration <= Epsilon ? 1f : Mathf.Clamp01(animationElapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(
                animationStartNormalizedPosition,
                animationTargetNormalizedPosition,
                eased);

            if (t >= 1f - Epsilon)
            {
                scrollRect.verticalNormalizedPosition = animationTargetNormalizedPosition;
                isAnimating = false;
            }
        }

        private void ProcessPendingScroll()
        {
            pendingScroll = false;
            if (!pendingFollowLatest)
            {
                return;
            }

            if (scrollRect == null || viewport == null || content == null || !scrollRect.enabled)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float viewportHeight = viewport.rect.height;
            float contentHeight = content.rect.height;
            float maxOffset = Mathf.Max(0f, contentHeight - viewportHeight);
            if (viewportHeight <= Epsilon || maxOffset <= Epsilon || content.childCount == 0)
            {
                CancelAnimation();
                return;
            }

            float currentOffset = GetCurrentTopOffset(maxOffset);
            if (!TryGetLatestItemBounds(out float latestTop, out float latestBottom, out float previousVisibleHeight))
            {
                return;
            }

            bool latestFullyVisible = latestTop >= currentOffset - Epsilon
                && latestBottom <= currentOffset + viewportHeight + Epsilon;
            float occupiedHeight = latestBottom - currentOffset;
            float thresholdHeight = viewportHeight * overflowThreshold;
            if (latestFullyVisible && occupiedHeight <= thresholdHeight + Epsilon)
            {
                return;
            }

            float minimumOffsetForLatestVisibility = Mathf.Max(0f, latestBottom - viewportHeight);
            float targetOffset = minimumOffsetForLatestVisibility;
            if (previousVisibleHeight > Epsilon)
            {
                float previousAwareOffset = Mathf.Max(0f, latestTop - previousVisibleHeight);
                targetOffset = Mathf.Min(targetOffset, previousAwareOffset);
                targetOffset = Mathf.Max(targetOffset, minimumOffsetForLatestVisibility);
            }

            targetOffset = Mathf.Clamp(targetOffset, 0f, maxOffset);
            float targetNormalizedPosition = OffsetToNormalizedPosition(targetOffset, maxOffset);
            if (Mathf.Abs(targetNormalizedPosition - scrollRect.verticalNormalizedPosition) <= Epsilon)
            {
                scrollRect.verticalNormalizedPosition = targetNormalizedPosition;
                isAnimating = false;
                return;
            }

            animationElapsed = 0f;
            animationStartNormalizedPosition = scrollRect.verticalNormalizedPosition;
            animationTargetNormalizedPosition = targetNormalizedPosition;
            isAnimating = true;

            if (autoScrollDuration <= Epsilon)
            {
                scrollRect.verticalNormalizedPosition = animationTargetNormalizedPosition;
                isAnimating = false;
            }
        }

        private bool IsNearBottom()
        {
            if (scrollRect == null || viewport == null || content == null || !scrollRect.enabled)
            {
                return false;
            }

            float viewportHeight = viewport.rect.height;
            float contentHeight = content.rect.height;
            float maxOffset = Mathf.Max(0f, contentHeight - viewportHeight);
            if (maxOffset <= Epsilon)
            {
                return true;
            }

            float currentOffset = GetCurrentTopOffset(maxOffset);
            float hiddenBelow = Mathf.Max(0f, contentHeight - (currentOffset + viewportHeight));
            return hiddenBelow <= viewportHeight * followLatestTolerance + Epsilon;
        }

        private bool TryGetLatestItemBounds(out float latestTop, out float latestBottom, out float previousVisibleHeight)
        {
            latestTop = 0f;
            latestBottom = 0f;
            previousVisibleHeight = 0f;

            if (content == null || content.childCount == 0)
            {
                return false;
            }

            RectTransform latest = content.GetChild(content.childCount - 1) as RectTransform;
            if (latest == null)
            {
                return false;
            }

            GetBoundsInContent(latest, out latestTop, out latestBottom);

            if (content.childCount < 2)
            {
                return true;
            }

            RectTransform previous = content.GetChild(content.childCount - 2) as RectTransform;
            if (previous == null)
            {
                return true;
            }

            GetBoundsInContent(previous, out float previousTop, out float previousBottom);
            previousVisibleHeight = (previousBottom - previousTop) * previousItemVisibility;
            return true;
        }

        private void GetBoundsInContent(RectTransform target, out float top, out float bottom)
        {
            target.GetWorldCorners(Corners);
            float contentTop = content.rect.yMax;
            float highestY = float.NegativeInfinity;
            float lowestY = float.PositiveInfinity;
            for (int i = 0; i < Corners.Length; i++)
            {
                float localY = content.InverseTransformPoint(Corners[i]).y;
                highestY = Mathf.Max(highestY, localY);
                lowestY = Mathf.Min(lowestY, localY);
            }

            top = contentTop - highestY;
            bottom = contentTop - lowestY;
        }

        private float GetCurrentTopOffset(float maxOffset)
        {
            return Mathf.Clamp01(1f - scrollRect.verticalNormalizedPosition) * maxOffset;
        }

        private static float OffsetToNormalizedPosition(float offset, float maxOffset)
        {
            if (maxOffset <= Epsilon)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01(offset / maxOffset);
        }

        private void CancelAnimation()
        {
            pendingScroll = false;
            isAnimating = false;
        }

        private static float GetDeltaTime()
        {
            if (Application.isPlaying)
            {
                return Mathf.Max(Time.unscaledDeltaTime, 1f / 120f);
            }

            return 1f / 60f;
        }
    }
}
