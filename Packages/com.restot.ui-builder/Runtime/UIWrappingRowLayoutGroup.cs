using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Restot.UIBuilder
{
    [DisallowMultipleComponent]
    public sealed class UIWrappingRowLayoutGroup : LayoutGroup
    {
        private readonly List<RowLine> lines = new List<RowLine>();

        [SerializeField, Min(1)] private int columnCount = UIBuilderConstants.DefaultColumnCount;
        [SerializeField, Min(0f)] private float gutter = UIBuilderConstants.DefaultGutter;
        [SerializeField] private bool controlsChildHeight;

        public void Configure(UIBuilderPadding rowPadding, float rowGutter, int rowColumnCount, bool rowControlsChildHeight)
        {
            padding = rowPadding.ToRectOffset();
            gutter = Mathf.Max(0f, rowGutter);
            columnCount = UIBuilderLayoutCalculator.ClampColumnCount(rowColumnCount);
            controlsChildHeight = rowControlsChildHeight;
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            BuildLines(rectTransform.rect.width);
            float preferredHeight = padding.vertical;

            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    preferredHeight += gutter;
                }

                preferredHeight += lines[i].Height;
            }

            SetLayoutInputForAxis(preferredHeight, preferredHeight, controlsChildHeight ? 1f : -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            BuildLines(rectTransform.rect.width);

            foreach (RowLine line in lines)
            {
                foreach (RowItem item in line.Items)
                {
                    SetChildAlongAxis(item.RectTransform, 0, item.X, item.Width);
                }
            }
        }

        public override void SetLayoutVertical()
        {
            BuildLines(rectTransform.rect.width);
            float y = padding.top;

            foreach (RowLine line in lines)
            {
                foreach (RowItem item in line.Items)
                {
                    float height = controlsChildHeight ? line.Height : item.Height;
                    SetChildAlongAxis(item.RectTransform, 1, y, height);
                }

                y += line.Height + gutter;
            }
        }

        private void BuildLines(float parentWidth)
        {
            lines.Clear();

            float availableWidth = Mathf.Max(0f, parentWidth - padding.horizontal);
            RowLine currentLine = new RowLine();
            int currentSpan = 0;

            foreach (RectTransform child in rectChildren)
            {
                int childSpan = GetChildSpan(child);
                if (currentLine.Items.Count > 0 && currentSpan + childSpan > columnCount)
                {
                    FinalizeLine(currentLine, availableWidth);
                    lines.Add(currentLine);
                    currentLine = new RowLine();
                    currentSpan = 0;
                }

                currentLine.Items.Add(new RowItem(child, childSpan, PreferredHeight(child)));
                currentSpan += childSpan;
            }

            if (currentLine.Items.Count > 0)
            {
                FinalizeLine(currentLine, availableWidth);
                lines.Add(currentLine);
            }
        }

        private int GetChildSpan(RectTransform child)
        {
            if (child.TryGetComponent(out UIColumn column))
            {
                return UIBuilderLayoutCalculator.ClampSpan(column.Span, columnCount);
            }

            return columnCount;
        }

        private float PreferredHeight(RectTransform child)
        {
            float preferredHeight = LayoutUtility.GetPreferredHeight(child);
            if (preferredHeight > 0f)
            {
                return preferredHeight;
            }

            if (child.rect.height > 0f)
            {
                return child.rect.height;
            }

            if (child.sizeDelta.y > 0f)
            {
                return child.sizeDelta.y;
            }

            return UIBuilderConstants.DefaultElementHeight;
        }

        private void FinalizeLine(RowLine line, float availableWidth)
        {
            float unitWidth = columnCount <= 1
                ? availableWidth
                : Mathf.Max(0f, availableWidth - gutter * (columnCount - 1)) / columnCount;

            float x = padding.left;
            float height = 0f;

            for (int i = 0; i < line.Items.Count; i++)
            {
                RowItem item = line.Items[i];
                item.X = x;
                item.Width = unitWidth * item.Span + gutter * Mathf.Max(0, item.Span - 1);
                line.Items[i] = item;

                x += item.Width + gutter;
                height = Mathf.Max(height, item.Height);
            }

            line.Height = height;
        }

        private struct RowItem
        {
            public RowItem(RectTransform rectTransform, int span, float height)
            {
                RectTransform = rectTransform;
                Span = span;
                Height = height;
                X = 0f;
                Width = 0f;
            }

            public RectTransform RectTransform { get; }
            public int Span { get; }
            public float Height { get; }
            public float X { get; set; }
            public float Width { get; set; }
        }

        private sealed class RowLine
        {
            public readonly List<RowItem> Items = new List<RowItem>();
            public float Height;
        }
    }
}
