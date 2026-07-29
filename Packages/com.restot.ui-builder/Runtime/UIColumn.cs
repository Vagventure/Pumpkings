using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Restot.UIBuilder
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIColumn : MonoBehaviour
    {
        private const string ScrollAreaObjectName = "Scroll Area";
        private const string ScrollContentObjectName = "Content";

        [SerializeField, Min(1)] private int span = 6;
        [SerializeField] private UIBuilderPadding padding;
        [SerializeField] private bool fixedHeight;
        [SerializeField, Min(0f)] private float height = 64f;
        [SerializeField] private bool scrollable;
        [SerializeField, Min(0f)] private float spacing;
        [SerializeField, Range(0f, 100f)] private float autoScrollOverflowThresholdPercent = 80f;
        [SerializeField, Range(0f, 100f)] private float autoScrollFollowLatestTolerancePercent = 10f;
        [SerializeField, Range(0f, 100f)] private float autoScrollPreviousItemVisibilityPercent = 50f;
        [SerializeField, Min(0f)] private float autoScrollDuration = 0.3f;
        [SerializeField] private UIColumnChildrenHeightMode childrenHeightMode;
        [SerializeField, HideInInspector] private bool parentControlsHeight;
        [SerializeField, HideInInspector] private UIScrollArea scrollArea;
        [SerializeField, HideInInspector] private UIScrollContent scrollContent;
        [SerializeField, HideInInspector] private bool isApplyingLayout;
        [SerializeField, HideInInspector] private bool pendingApplyLayout;
        [SerializeField, HideInInspector] private int pendingColumnCount = UIBuilderConstants.DefaultColumnCount;

        public int Span
        {
            get => Mathf.Max(1, span);
            set
            {
                span = Mathf.Max(1, value);
                ApplyLayout();
            }
        }

        public UIBuilderPadding Padding
        {
            get => padding;
            set
            {
                padding = value;
                ApplyLayout();
            }
        }

        public UIColumnChildrenHeightMode ChildrenHeightMode
        {
            get => childrenHeightMode;
            set
            {
                childrenHeightMode = value;
                ApplyLayout();
            }
        }

        public bool FixedHeight
        {
            get => fixedHeight;
            set
            {
                fixedHeight = value;
                ApplyLayout();
            }
        }

        public float Height
        {
            get => Mathf.Max(0f, height);
            set
            {
                height = Mathf.Max(0f, value);
                ApplyLayout();
            }
        }

        public bool Scrollable
        {
            get => scrollable;
            set
            {
                scrollable = value;
                if (scrollable && !fixedHeight)
                {
                    fixedHeight = true;
                }

                ApplyLayout();
            }
        }

        public float Spacing
        {
            get => Mathf.Max(0f, spacing);
            set
            {
                spacing = Mathf.Max(0f, value);
                ApplyLayout();
            }
        }

        public float AutoScrollOverflowThresholdPercent => Mathf.Clamp(autoScrollOverflowThresholdPercent, 0f, 100f);

        public float AutoScrollFollowLatestTolerancePercent => Mathf.Clamp(autoScrollFollowLatestTolerancePercent, 0f, 100f);

        public float AutoScrollPreviousItemVisibilityPercent => Mathf.Clamp(autoScrollPreviousItemVisibilityPercent, 0f, 100f);

        public float AutoScrollDuration => Mathf.Max(0f, autoScrollDuration);

        public void SetParentControlsHeight(bool controlsHeight)
        {
            parentControlsHeight = controlsHeight;
            ApplyLayout();
        }

        public Transform GetContentParent()
        {
            if (TryGetScrollStructure(out _, out UIScrollContent content))
            {
                return content.transform;
            }

            if (scrollable)
            {
                EnsureScrollStructure();
                return scrollContent != null ? scrollContent.transform : transform;
            }

            return transform;
        }

        public void NotifyScrollContentChanged()
        {
            ApplyLayout(Mathf.Max(UIBuilderConstants.DefaultColumnCount, span));
            RequestAutoScroll();
        }

        public void NotifyScrollContentGeometryChanged()
        {
            RequestAutoScroll();
        }

        private void Reset()
        {
            ApplyLayout();
        }

        private void OnValidate()
        {
            span = Mathf.Max(1, span);
            height = Mathf.Max(0f, height);
            spacing = Mathf.Max(0f, spacing);
            autoScrollOverflowThresholdPercent = Mathf.Clamp(autoScrollOverflowThresholdPercent, 0f, 100f);
            autoScrollFollowLatestTolerancePercent = Mathf.Clamp(autoScrollFollowLatestTolerancePercent, 0f, 100f);
            autoScrollPreviousItemVisibilityPercent = Mathf.Clamp(autoScrollPreviousItemVisibilityPercent, 0f, 100f);
            autoScrollDuration = Mathf.Max(0f, autoScrollDuration);
            if (scrollable && !fixedHeight)
            {
                fixedHeight = true;
            }

            ApplyLayout(Mathf.Max(UIBuilderConstants.DefaultColumnCount, span));
        }

        private void OnEnable()
        {
            ApplyLayout(Mathf.Max(UIBuilderConstants.DefaultColumnCount, span));
        }

        private void OnTransformChildrenChanged()
        {
            ApplyLayout(Mathf.Max(UIBuilderConstants.DefaultColumnCount, span));
        }

        public void ApplyLayout(int columnCount = UIBuilderConstants.DefaultColumnCount)
        {
            int requestedColumnCount = UIBuilderLayoutCalculator.ClampColumnCount(columnCount);
            if (isApplyingLayout)
            {
                pendingApplyLayout = true;
                pendingColumnCount = Mathf.Max(pendingColumnCount, requestedColumnCount);
                return;
            }

            isApplyingLayout = true;
            try
            {
                ApplyLayoutInternal(requestedColumnCount);
            }
            finally
            {
                isApplyingLayout = false;
            }

            if (!pendingApplyLayout)
            {
                pendingColumnCount = UIBuilderConstants.DefaultColumnCount;
                return;
            }

            int deferredColumnCount = pendingColumnCount;
            pendingApplyLayout = false;
            pendingColumnCount = UIBuilderConstants.DefaultColumnCount;
            ApplyLayout(deferredColumnCount);
        }

        private void ApplyLayoutInternal(int columnCount)
        {
            int layoutSpan = UIBuilderLayoutCalculator.ClampSpan(span, columnCount);
            span = layoutSpan;

            if (scrollable && !fixedHeight)
            {
                fixedHeight = true;
            }

            RectTransform rootRect = (RectTransform)transform;
            bool usesScrollStructure = scrollable || TryGetScrollStructure(out _, out _);
            if (usesScrollStructure)
            {
                EnsureScrollStructure();
                CleanupLegacyScrollArtifacts();
            }

            RectTransform layoutRoot = usesScrollStructure && scrollContent != null
                ? (RectTransform)scrollContent.transform
                : rootRect;

            ConfigureRootLayout(layoutSpan, rootRect, layoutRoot);
            ConfigureLayoutGroup(layoutRoot);
            ConfigureScrollStructure(rootRect, layoutRoot);

            ApplyChildHeightMode(layoutRoot);
            UpdateHeightReporting(rootRect, layoutRoot);

            LayoutRebuilder.MarkLayoutForRebuild(rootRect);
            if (layoutRoot != rootRect)
            {
                LayoutRebuilder.MarkLayoutForRebuild(layoutRoot);
            }
        }

        private void ConfigureRootLayout(int layoutSpan, RectTransform rootRect, RectTransform layoutRoot)
        {
            bool controlsOwnHeight = fixedHeight && !parentControlsHeight;
            LayoutElement layoutElement = EnsureComponent<LayoutElement>(gameObject);
            layoutElement.ignoreLayout = false;
            layoutElement.flexibleWidth = layoutSpan;
            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = -1f;
            layoutElement.flexibleHeight = parentControlsHeight ? 1f : fixedHeight ? 0f : -1f;
            layoutElement.preferredHeight = controlsOwnHeight ? Height : -1f;
            layoutElement.minHeight = controlsOwnHeight ? Height : parentControlsHeight ? 0f : -1f;

            VerticalLayoutGroup rootLayoutGroup = GetComponent<VerticalLayoutGroup>();
            if (rootLayoutGroup != null)
            {
                rootLayoutGroup.enabled = layoutRoot == rootRect;
                if (rootLayoutGroup.enabled)
                {
                    ConfigureLayoutGroup(rootRect);
                }
            }

            if (controlsOwnHeight)
            {
                rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);
            }
        }

        private void ConfigureLayoutGroup(RectTransform layoutRoot)
        {
            VerticalLayoutGroup layoutGroup = EnsureComponent<VerticalLayoutGroup>(layoutRoot.gameObject);
            layoutGroup.enabled = true;
            layoutGroup.padding = padding.ToRectOffset();
            layoutGroup.spacing = Spacing;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = childrenHeightMode == UIColumnChildrenHeightMode.FillEqually;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = childrenHeightMode == UIColumnChildrenHeightMode.FillEqually;
        }

        private void ConfigureScrollStructure(RectTransform rootRect, RectTransform layoutRoot)
        {
            if (layoutRoot == rootRect || scrollArea == null || scrollContent == null)
            {
                return;
            }

            RectTransform scrollAreaRect = (RectTransform)scrollArea.transform;
            RectTransform scrollContentRect = (RectTransform)scrollContent.transform;
            bool stretchContentToViewport = childrenHeightMode == UIColumnChildrenHeightMode.FillEqually;

            ConfigureScrollAreaRect(scrollAreaRect);
            ConfigureScrollContentRect(scrollContentRect, stretchContentToViewport);

            ScrollRect scrollRect = EnsureComponent<ScrollRect>(scrollArea.gameObject);
            scrollRect.content = scrollContentRect;
            scrollRect.viewport = scrollAreaRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;
            scrollRect.enabled = scrollable;

            RectMask2D mask = EnsureComponent<RectMask2D>(scrollArea.gameObject);
            mask.enabled = true;

            Image viewportImage = EnsureComponent<Image>(scrollArea.gameObject);
            viewportImage.color = Color.clear;
            viewportImage.raycastTarget = true;

            ContentSizeFitter contentFitter = EnsureComponent<ContentSizeFitter>(scrollContent.gameObject);
            contentFitter.enabled = !stretchContentToViewport;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = stretchContentToViewport
                ? ContentSizeFitter.FitMode.Unconstrained
                : ContentSizeFitter.FitMode.MinSize;

            UIScrollAutoScroll autoScroll = EnsureComponent<UIScrollAutoScroll>(scrollArea.gameObject);
            autoScroll.Configure(
                scrollRect,
                scrollAreaRect,
                scrollContentRect,
                AutoScrollOverflowThresholdPercent * 0.01f,
                AutoScrollFollowLatestTolerancePercent * 0.01f,
                AutoScrollPreviousItemVisibilityPercent * 0.01f,
                AutoScrollDuration);
        }

        private void UpdateHeightReporting(RectTransform rootRect, RectTransform layoutRoot)
        {
            if (layoutRoot == rootRect || fixedHeight || parentControlsHeight)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
            float preferredHeight = LayoutUtility.GetPreferredHeight(layoutRoot);
            if (preferredHeight <= 0f)
            {
                return;
            }

            LayoutElement layoutElement = EnsureComponent<LayoutElement>(gameObject);
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.minHeight = preferredHeight;
        }

        private void ApplyChildHeightMode(Transform layoutRoot)
        {
            foreach (Transform child in layoutRoot)
            {
                if (!child.TryGetComponent(out RectTransform rectTransform))
                {
                    continue;
                }

                LayoutElement childLayoutElement = child.GetComponent<LayoutElement>();
                if (childLayoutElement == null)
                {
                    childLayoutElement = child.gameObject.AddComponent<LayoutElement>();
                }

                if (childrenHeightMode == UIColumnChildrenHeightMode.FillEqually)
                {
                    SetRowParentControlsHeight(child, true);
                    childLayoutElement.flexibleHeight = 1f;
                    childLayoutElement.preferredHeight = -1f;
                    childLayoutElement.minHeight = -1f;
                    continue;
                }

                SetRowParentControlsHeight(child, false);
                childLayoutElement.flexibleHeight = -1f;
                childLayoutElement.minHeight = -1f;

                float preferredHeight = LayoutUtility.GetPreferredHeight(rectTransform);
                if (preferredHeight > 0f || rectTransform.rect.height > 0f || rectTransform.sizeDelta.y > 0f)
                {
                    childLayoutElement.preferredHeight = -1f;
                    continue;
                }

                childLayoutElement.preferredHeight = UIBuilderConstants.DefaultElementHeight;
            }
        }

        private void EnsureScrollStructure()
        {
            TryGetScrollStructure(out UIScrollArea ownedArea, out UIScrollContent ownedContent);

            bool createdArea = false;
            if (ownedArea == null)
            {
                GameObject areaObject = new GameObject(ScrollAreaObjectName, typeof(RectTransform), typeof(UIScrollArea));
                ownedArea = areaObject.GetComponent<UIScrollArea>();
                ownedArea.Initialize(this);
                scrollArea = ownedArea;
                areaObject.transform.SetParent(transform, false);
                createdArea = true;
            }

            ownedArea.Initialize(this);
            scrollArea = ownedArea;

            bool createdContent = false;
            if (ownedContent == null || ownedContent.transform.parent != ownedArea.transform)
            {
                if (ownedContent == null)
                {
                    GameObject contentObject = new GameObject(ScrollContentObjectName, typeof(RectTransform), typeof(UIScrollContent));
                    ownedContent = contentObject.GetComponent<UIScrollContent>();
                    ownedContent.Initialize(this);
                    scrollContent = ownedContent;
                    contentObject.transform.SetParent(ownedArea.transform, false);
                    createdContent = true;
                }
                else
                {
                    ownedContent.transform.SetParent(ownedArea.transform, false);
                }
            }

            ownedContent.Initialize(this);
            scrollContent = ownedContent;

            bool movedChildren = MoveDirectChildrenToContent();
            if ((createdArea || createdContent || movedChildren) && scrollable)
            {
                RequestAutoScroll();
            }
        }

        private bool MoveDirectChildrenToContent()
        {
            if (scrollContent == null || scrollArea == null)
            {
                return false;
            }

            List<Transform> childrenToMove = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child == scrollArea.transform)
                {
                    continue;
                }

                childrenToMove.Add(child);
            }

            for (int i = 0; i < childrenToMove.Count; i++)
            {
                childrenToMove[i].SetParent(scrollContent.transform, false);
            }

            return childrenToMove.Count > 0;
        }

        private void CleanupLegacyScrollArtifacts()
        {
            if (scrollArea == null || scrollContent == null)
            {
                return;
            }

            RectTransform destination = (RectTransform)scrollContent.transform;
            CleanupLegacyScrollArtifactsUnder(transform, scrollArea.transform, destination);
            CleanupLegacyScrollArtifactsUnder(scrollContent.transform, null, destination);
        }

        private void CleanupLegacyScrollArtifactsUnder(Transform parent, Transform excludedChild, RectTransform destination)
        {
            List<Transform> artifacts = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (child == excludedChild)
                {
                    continue;
                }

                if (IsLegacyOwnedScrollArtifact(child))
                {
                    artifacts.Add(child);
                }
            }

            for (int i = 0; i < artifacts.Count; i++)
            {
                FlattenLegacyScrollArtifact(artifacts[i], destination);
            }
        }

        private void FlattenLegacyScrollArtifact(Transform artifact, RectTransform destination)
        {
            if (artifact == null || artifact == destination)
            {
                return;
            }

            List<Transform> children = new List<Transform>();
            foreach (Transform child in artifact)
            {
                children.Add(child);
            }

            for (int i = 0; i < children.Count; i++)
            {
                Transform child = children[i];
                if (child == destination)
                {
                    continue;
                }

                if (IsLegacyOwnedScrollArtifact(child))
                {
                    FlattenLegacyScrollArtifact(child, destination);
                    continue;
                }

                child.SetParent(destination, false);
            }

                DestroyUnityObject(artifact.gameObject);
        }

        private bool TryGetScrollStructure(out UIScrollArea ownedArea, out UIScrollContent ownedContent)
        {
            ownedArea = IsOwnedScrollArea(scrollArea) ? scrollArea : FindOwnedScrollArea();
            ownedContent = IsOwnedScrollContent(ownedContent: scrollContent, expectedArea: ownedArea)
                ? scrollContent
                : FindOwnedScrollContent(ownedArea);

            scrollArea = ownedArea;
            scrollContent = ownedContent;
            return ownedArea != null && ownedContent != null;
        }

        private UIScrollArea FindOwnedScrollArea()
        {
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent(out UIScrollArea area))
                {
                    continue;
                }

                if (area.Owner == this || area.Owner == null)
                {
                    return area;
                }
            }

            return null;
        }

        private UIScrollContent FindOwnedScrollContent(UIScrollArea ownedArea)
        {
            if (ownedArea == null)
            {
                return null;
            }

            foreach (Transform child in ownedArea.transform)
            {
                if (!child.TryGetComponent(out UIScrollContent content))
                {
                    continue;
                }

                if (content.Owner == this || content.Owner == null)
                {
                    return content;
                }
            }

            return null;
        }

        private bool IsOwnedScrollArea(UIScrollArea ownedArea)
        {
            return ownedArea != null && ownedArea.Owner == this && ownedArea.transform.parent == transform;
        }

        private bool IsOwnedScrollContent(UIScrollContent ownedContent, UIScrollArea expectedArea)
        {
            return ownedContent != null
                && ownedContent.Owner == this
                && expectedArea != null
                && ownedContent.transform.parent == expectedArea.transform;
        }

        private bool IsLegacyOwnedScrollArtifact(Transform target)
        {
            if (target == null || target == scrollArea?.transform || target == scrollContent?.transform)
            {
                return false;
            }

            if (target.TryGetComponent(out UIColumn _))
            {
                return false;
            }

            if (target.TryGetComponent(out UIScrollArea legacyArea))
            {
                return legacyArea.Owner == this || legacyArea.Owner == null;
            }

            if (target.TryGetComponent(out UIScrollContent legacyContent))
            {
                return legacyContent.Owner == this || legacyContent.Owner == null;
            }

            return false;
        }

        private void RequestAutoScroll()
        {
            if (!scrollable || scrollArea == null)
            {
                return;
            }

            if (scrollArea.TryGetComponent(out UIScrollAutoScroll autoScroll))
            {
                autoScroll.RequestScrollToBottom();
            }
        }

        private static void ConfigureScrollAreaRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void ConfigureScrollContentRect(RectTransform rectTransform, bool stretchToViewport)
        {
            rectTransform.anchorMin = stretchToViewport ? Vector2.zero : new Vector2(0f, 1f);
            rectTransform.anchorMax = stretchToViewport ? Vector2.one : new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void SetRowParentControlsHeight(Transform child, bool controlsHeight)
        {
            if (child.TryGetComponent(out UIRow row))
            {
                row.SetParentControlsHeight(controlsHeight);
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }
}
