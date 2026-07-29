using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Restot.UIBuilder
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIRow : MonoBehaviour
    {
        [SerializeField] private UIBuilderPadding padding;
        [FormerlySerializedAs("overrideGutter")]
        [SerializeField] private bool overrideColumnSpacing;
        [FormerlySerializedAs("gutter")]
        [SerializeField, Min(0f)] private float columnSpacing = UIBuilderConstants.DefaultGutter;
        [SerializeField] private bool fixedHeight;
        [SerializeField, Min(0f)] private float height = 64f;
        [SerializeField, HideInInspector] private bool parentControlsHeight;
        [FormerlySerializedAs("lastAppliedGlobalGutter")]
        [SerializeField, HideInInspector] private float lastAppliedGlobalColumnSpacing = UIBuilderConstants.DefaultGutter;
        [SerializeField, HideInInspector] private int lastAppliedColumnCount = UIBuilderConstants.DefaultColumnCount;

        public UIBuilderPadding Padding
        {
            get => padding;
            set
            {
                padding = value;
                ApplyLayout();
            }
        }

        public bool OverrideColumnSpacing
        {
            get => overrideColumnSpacing;
            set
            {
                overrideColumnSpacing = value;
                ApplyLayout();
            }
        }

        public float ColumnSpacing
        {
            get => Mathf.Max(0f, columnSpacing);
            set
            {
                columnSpacing = Mathf.Max(0f, value);
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

        public void SetParentControlsHeight(bool controlsHeight)
        {
            parentControlsHeight = controlsHeight;
            ApplyLayout();
        }

        private void Reset()
        {
            ApplyLayout();
        }

        private void OnValidate()
        {
            columnSpacing = Mathf.Max(0f, columnSpacing);
            height = Mathf.Max(0f, height);
            ApplyLayout();
        }

        private void OnEnable()
        {
            ApplyLayout();
        }

        public void ApplyLayout(float? globalColumnSpacing = null, int? columnCount = null)
        {
            bool controlsChildHeight = fixedHeight || parentControlsHeight;
            RectTransform rectTransform = (RectTransform)transform;
            if (globalColumnSpacing.HasValue)
            {
                lastAppliedGlobalColumnSpacing = Mathf.Max(0f, globalColumnSpacing.Value);
            }

            if (columnCount.HasValue)
            {
                lastAppliedColumnCount = UIBuilderLayoutCalculator.ClampColumnCount(columnCount.Value);
            }

            if (TryGetComponent(out HorizontalLayoutGroup legacyHorizontalLayout))
            {
                legacyHorizontalLayout.enabled = false;
            }

            UIWrappingRowLayoutGroup layoutGroup = EnsureComponent<UIWrappingRowLayoutGroup>();
            layoutGroup.enabled = true;
            layoutGroup.Configure(
                padding,
                overrideColumnSpacing ? ColumnSpacing : lastAppliedGlobalColumnSpacing,
                lastAppliedColumnCount,
                controlsChildHeight);

            LayoutElement layoutElement = EnsureComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;
            layoutElement.preferredHeight = fixedHeight ? Height : -1f;
            layoutElement.minHeight = fixedHeight ? Height : -1f;
            layoutElement.flexibleHeight = parentControlsHeight ? 1f : fixedHeight ? 0f : -1f;

            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>();
            fitter.enabled = !controlsChildHeight;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = controlsChildHeight ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.MinSize;

            if (fixedHeight && !parentControlsHeight)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);
            }

            ApplyChildColumnHeightOwnership(controlsChildHeight);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void ApplyChildColumnHeightOwnership(bool controlsChildHeight)
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out UIColumn column))
                {
                    column.SetParentControlsHeight(controlsChildHeight);
                }
            }
        }

        public int ChildColumnSpanTotal(int columnCount)
        {
            int total = 0;

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out UIColumn column))
                {
                    total += UIBuilderLayoutCalculator.ClampSpan(column.Span, columnCount);
                }
            }

            return total;
        }

        private T EnsureComponent<T>() where T : Component
        {
            if (!TryGetComponent(out T component))
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
