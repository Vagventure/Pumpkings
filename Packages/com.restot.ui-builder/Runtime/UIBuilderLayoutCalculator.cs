using System.Collections.Generic;
using UnityEngine;

namespace Restot.UIBuilder
{
    public readonly struct ColumnWidthResult
    {
        public ColumnWidthResult(int span, int columnCount, float share, float availableWidth, float width)
        {
            Span = span;
            ColumnCount = columnCount;
            Share = share;
            AvailableWidth = availableWidth;
            Width = width;
        }

        public int Span { get; }
        public int ColumnCount { get; }
        public float Share { get; }
        public float AvailableWidth { get; }
        public float Width { get; }
    }

    public static class UIBuilderLayoutCalculator
    {
        public static int ClampColumnCount(int columnCount)
        {
            return Mathf.Max(1, columnCount);
        }

        public static int ClampSpan(int span, int columnCount = UIBuilderConstants.DefaultColumnCount)
        {
            return Mathf.Clamp(span, 1, ClampColumnCount(columnCount));
        }

        public static float WidthShare(int span, int columnCount = UIBuilderConstants.DefaultColumnCount)
        {
            int clampedColumnCount = ClampColumnCount(columnCount);
            return ClampSpan(span, clampedColumnCount) / (float)clampedColumnCount;
        }

        public static float AvailableWidth(float parentWidth, int childCount, float gutter, UIBuilderPadding padding)
        {
            float spacing = Mathf.Max(0, childCount - 1) * Mathf.Max(0f, gutter);
            return Mathf.Max(0f, parentWidth - spacing - padding.Horizontal);
        }

        public static ColumnWidthResult CalculateColumnWidth(
            int span,
            int columnCount,
            float parentWidth,
            int childCount,
            float gutter,
            UIBuilderPadding rowPadding)
        {
            int clampedColumnCount = ClampColumnCount(columnCount);
            int clampedSpan = ClampSpan(span, clampedColumnCount);
            float availableWidth = AvailableWidth(parentWidth, childCount, gutter, rowPadding);
            float share = clampedSpan / (float)clampedColumnCount;
            return new ColumnWidthResult(clampedSpan, clampedColumnCount, share, availableWidth, availableWidth * share);
        }

        public static int SumSpans(IEnumerable<int> spans, int columnCount = UIBuilderConstants.DefaultColumnCount)
        {
            int total = 0;
            int clampedColumnCount = ClampColumnCount(columnCount);

            foreach (int span in spans)
            {
                total += ClampSpan(span, clampedColumnCount);
            }

            return total;
        }

        public static bool IsOverflow(IEnumerable<int> spans, int columnCount = UIBuilderConstants.DefaultColumnCount)
        {
            return SumSpans(spans, columnCount) > ClampColumnCount(columnCount);
        }
    }
}
