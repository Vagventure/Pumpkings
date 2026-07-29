using NUnit.Framework;
using Restot.UIBuilder;

namespace Restot.UIBuilder.Tests.Editor
{
    public sealed class UIBuilderLayoutCalculatorTests
    {
        [Test]
        public void WidthShare_ColSixOfTwelve_IsHalf()
        {
            Assert.That(UIBuilderLayoutCalculator.WidthShare(6, 12), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void WidthShare_ColFourAndEight_HasOneToTwoRatio()
        {
            float colFour = UIBuilderLayoutCalculator.WidthShare(4, 12);
            float colEight = UIBuilderLayoutCalculator.WidthShare(8, 12);

            Assert.That(colEight / colFour, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ClampSpan_KeepsSpanWithinGrid()
        {
            Assert.That(UIBuilderLayoutCalculator.ClampSpan(0, 12), Is.EqualTo(1));
            Assert.That(UIBuilderLayoutCalculator.ClampSpan(20, 12), Is.EqualTo(12));
        }

        [Test]
        public void IsOverflow_ReturnsTrueWhenSpanSumExceedsColumnCount()
        {
            Assert.That(UIBuilderLayoutCalculator.IsOverflow(new[] { 6, 6, 1 }, 12), Is.True);
            Assert.That(UIBuilderLayoutCalculator.IsOverflow(new[] { 4, 8 }, 12), Is.False);
        }

        [Test]
        public void AvailableWidth_SubtractsGutterAndPadding()
        {
            UIBuilderPadding padding = new UIBuilderPadding(left: 10f, right: 20f, top: 0f, bottom: 0f);

            float width = UIBuilderLayoutCalculator.AvailableWidth(
                parentWidth: 1000f,
                childCount: 3,
                gutter: 16f,
                padding: padding);

            Assert.That(width, Is.EqualTo(938f).Within(0.0001f));
        }
    }
}
