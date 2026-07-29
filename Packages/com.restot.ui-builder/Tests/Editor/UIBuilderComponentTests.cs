using NUnit.Framework;
using Restot.UIBuilder;
using Restot.UIBuilder.Editor;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Restot.UIBuilder.Tests.Editor
{
    public sealed class UIBuilderComponentTests
    {
        private readonly System.Collections.Generic.List<GameObject> createdObjects = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void UIRow_ApplyLayout_ConfiguresNativeLayoutComponents()
        {
            GameObject gameObject = CreateGameObject("Row");
            UIRow row = gameObject.AddComponent<UIRow>();

            row.ApplyLayout(globalColumnSpacing: 24f, columnCount: 12);

            UIWrappingRowLayoutGroup wrapping = gameObject.GetComponent<UIWrappingRowLayoutGroup>();
            ContentSizeFitter fitter = gameObject.GetComponent<ContentSizeFitter>();
            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();

            Assert.That(wrapping, Is.Not.Null);
            Assert.That(wrapping.enabled, Is.True);
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.enabled, Is.True);
            Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.MinSize));
            Assert.That(layoutElement, Is.Not.Null);
        }

        [Test]
        public void UIRow_ApplyLayout_FixedHeightControlsChildHeight()
        {
            GameObject gameObject = CreateGameObject("Row");
            UIRow row = gameObject.AddComponent<UIRow>();
            GameObject columnObject = CreateGameObject("Column");
            UIColumn column = columnObject.AddComponent<UIColumn>();
            columnObject.transform.SetParent(gameObject.transform, false);

            row.FixedHeight = true;
            row.Height = 500f;
            row.ApplyLayout(globalColumnSpacing: 24f, columnCount: 12);

            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            ContentSizeFitter fitter = gameObject.GetComponent<ContentSizeFitter>();

            Assert.That(layoutElement.preferredHeight, Is.EqualTo(500f).Within(0.0001f));
            Assert.That(fitter.enabled, Is.False);
            Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.Unconstrained));
            Assert.That(((RectTransform)gameObject.transform).rect.height, Is.EqualTo(500f).Within(0.0001f));
            Assert.That(columnObject.GetComponent<LayoutElement>().flexibleHeight, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void UIRow_ApplyLayout_WrapsColumnsWhenSpansExceedColumnCount()
        {
            GameObject rowObject = CreateGameObject("Row");
            RectTransform rowTransform = (RectTransform)rowObject.transform;
            rowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1200f);
            rowTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 400f);
            UIRow row = rowObject.AddComponent<UIRow>();

            RectTransform first = CreateColumn(rowObject.transform, 6);
            RectTransform second = CreateColumn(rowObject.transform, 6);
            RectTransform third = CreateColumn(rowObject.transform, 6);

            row.ApplyLayout(globalColumnSpacing: 16f, columnCount: 12);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowTransform);

            Assert.That(first.anchoredPosition.y, Is.EqualTo(second.anchoredPosition.y).Within(0.0001f));
            Assert.That(third.anchoredPosition.y, Is.LessThan(first.anchoredPosition.y));
            Assert.That(third.anchoredPosition.x, Is.EqualTo(first.anchoredPosition.x).Within(0.0001f));
        }

        [Test]
        public void UIColumn_ApplyLayout_ConfiguresFlexibleWidthFromSpan()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();

            column.Span = 8;
            column.ApplyLayout(columnCount: 12);

            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            VerticalLayoutGroup vertical = gameObject.GetComponent<VerticalLayoutGroup>();

            Assert.That(layoutElement, Is.Not.Null);
            Assert.That(layoutElement.flexibleWidth, Is.EqualTo(8f).Within(0.0001f));
            Assert.That(vertical, Is.Not.Null);
            Assert.That(vertical.childControlWidth, Is.True);
            Assert.That(vertical.childControlHeight, Is.False);
            Assert.That(vertical.childForceExpandHeight, Is.False);
        }

        [Test]
        public void UIColumn_ApplyLayout_FixedHeightSetsLayoutElementAndRectTransformHeight()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();

            column.FixedHeight = true;
            column.Height = 320f;
            column.ApplyLayout(columnCount: 12);

            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();

            Assert.That(layoutElement.preferredHeight, Is.EqualTo(320f).Within(0.0001f));
            Assert.That(layoutElement.minHeight, Is.EqualTo(320f).Within(0.0001f));
            Assert.That(layoutElement.flexibleHeight, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(((RectTransform)gameObject.transform).rect.height, Is.EqualTo(320f).Within(0.0001f));
        }

        [Test]
        public void UIColumn_ApplyLayout_ScrollableCreatesSingleScrollStructure()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();

            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);
            column.ApplyLayout(columnCount: 12);

            UIScrollArea[] areas = gameObject.GetComponentsInChildren<UIScrollArea>(true);
            UIScrollContent[] contents = gameObject.GetComponentsInChildren<UIScrollContent>(true);

            Assert.That(areas.Length, Is.EqualTo(1));
            Assert.That(contents.Length, Is.EqualTo(1));
            Assert.That(column.GetContentParent(), Is.EqualTo(contents[0].transform));
        }

        [Test]
        public void UIColumn_ScrollableEnablesFixedHeightWithoutChangingConfiguredHeight()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();
            column.Height = 420f;
            column.FixedHeight = false;

            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);

            Assert.That(column.FixedHeight, Is.True);
            Assert.That(column.Height, Is.EqualTo(420f).Within(0.0001f));
        }

        [Test]
        public void UIColumn_DisablingScrollableKeepsExistingContentWrapper()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();
            GameObject child = CreateGameObject("Item");
            child.transform.SetParent(gameObject.transform, false);

            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);
            Transform contentParent = column.GetContentParent();

            column.Scrollable = false;
            column.ApplyLayout(columnCount: 12);

            Assert.That(column.GetContentParent(), Is.EqualTo(contentParent));
            Assert.That(child.transform.parent, Is.EqualTo(contentParent));
        }

        [Test]
        public void UIColumn_NotifyScrollContentChanged_AutoScrollsWhenContentOverflows()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();
            column.Height = 120f;
            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);

            RectTransform content = (RectTransform)column.GetContentParent();
            for (int i = 0; i < 3; i++)
            {
                GameObject child = CreateGameObject("Item" + i);
                child.transform.SetParent(content, false);
                RectTransform childRect = (RectTransform)child.transform;
                childRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);

                LayoutElement childLayout = child.AddComponent<LayoutElement>();
                childLayout.preferredHeight = 80f;
                childLayout.minHeight = 80f;
            }

            ScrollRect scrollRect = gameObject.GetComponentInChildren<ScrollRect>(true);
            scrollRect.verticalNormalizedPosition = 0f;
            column.NotifyScrollContentChanged();
            UIScrollAutoScroll autoScroll = gameObject.GetComponentInChildren<UIScrollAutoScroll>(true);
            InvokeNonPublic(autoScroll, "LateUpdate", repeatCount: 24);

            Assert.That(scrollRect.verticalNormalizedPosition, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void UIColumn_NotifyScrollContentChanged_DoesNotAutoScrollWhenUserIsAwayFromBottom()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();
            column.Height = 120f;
            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);

            RectTransform content = (RectTransform)column.GetContentParent();
            for (int i = 0; i < 3; i++)
            {
                GameObject child = CreateGameObject("Item" + i);
                child.transform.SetParent(content, false);
                RectTransform childRect = (RectTransform)child.transform;
                childRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);

                LayoutElement childLayout = child.AddComponent<LayoutElement>();
                childLayout.preferredHeight = 80f;
                childLayout.minHeight = 80f;
            }

            ScrollRect scrollRect = gameObject.GetComponentInChildren<ScrollRect>(true);
            scrollRect.verticalNormalizedPosition = 1f;

            column.NotifyScrollContentChanged();
            UIScrollAutoScroll autoScroll = gameObject.GetComponentInChildren<UIScrollAutoScroll>(true);
            InvokeNonPublic(autoScroll, "LateUpdate", repeatCount: 24);

            Assert.That(scrollRect.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void UIColumn_ApplyLayout_CleansLegacyDuplicateScrollArtifactsFromContent()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();
            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);

            Transform content = column.GetContentParent();
            GameObject legacyArea = CreateGameObject("Scroll Area");
            legacyArea.AddComponent<UIScrollArea>().Initialize(column);
            legacyArea.transform.SetParent(content, false);

            GameObject legacyContent = CreateGameObject("Content");
            legacyContent.AddComponent<UIScrollContent>().Initialize(column);
            legacyContent.transform.SetParent(legacyArea.transform, false);

            GameObject message = CreateGameObject("Message");
            message.transform.SetParent(legacyContent.transform, false);

            column.ApplyLayout(columnCount: 12);

            Assert.That(message.transform.parent, Is.EqualTo(content));
            Assert.That(gameObject.GetComponentsInChildren<UIScrollArea>(true).Length, Is.EqualTo(1));
            Assert.That(gameObject.GetComponentsInChildren<UIScrollContent>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void UIColumn_ApplyLayout_ClampsSpanToColumnCount()
        {
            GameObject gameObject = CreateGameObject("Column");
            UIColumn column = gameObject.AddComponent<UIColumn>();

            column.Span = 20;
            column.ApplyLayout(columnCount: 12);

            Assert.That(column.Span, Is.EqualTo(12));
            Assert.That(gameObject.GetComponent<LayoutElement>().flexibleWidth, Is.EqualTo(12f).Within(0.0001f));
        }

        [Test]
        public void UIColumn_ApplyLayout_AddsFallbackHeightToZeroHeightChildren()
        {
            GameObject columnObject = CreateGameObject("Column");
            UIColumn column = columnObject.AddComponent<UIColumn>();
            GameObject child = CreateGameObject("Button");
            child.transform.SetParent(columnObject.transform, false);

            column.ApplyLayout(columnCount: 12);

            LayoutElement childLayoutElement = child.GetComponent<LayoutElement>();
            Assert.That(childLayoutElement, Is.Not.Null);
            Assert.That(childLayoutElement.preferredHeight, Is.EqualTo(UIBuilderConstants.DefaultElementHeight).Within(0.0001f));
        }

        [Test]
        public void UIColumn_ApplyLayout_FillEquallyExpandsChildrenByFlexibleHeight()
        {
            GameObject columnObject = CreateGameObject("Column");
            UIColumn column = columnObject.AddComponent<UIColumn>();
            column.ChildrenHeightMode = UIColumnChildrenHeightMode.FillEqually;
            GameObject firstRow = CreateGameObject("Row");
            GameObject secondRow = CreateGameObject("Row");
            firstRow.AddComponent<UIRow>();
            secondRow.AddComponent<UIRow>();
            firstRow.transform.SetParent(columnObject.transform, false);
            secondRow.transform.SetParent(columnObject.transform, false);

            column.ApplyLayout(columnCount: 12);

            VerticalLayoutGroup vertical = columnObject.GetComponent<VerticalLayoutGroup>();
            LayoutElement firstLayoutElement = firstRow.GetComponent<LayoutElement>();
            LayoutElement secondLayoutElement = secondRow.GetComponent<LayoutElement>();
            ContentSizeFitter firstFitter = firstRow.GetComponent<ContentSizeFitter>();

            Assert.That(vertical.childControlHeight, Is.True);
            Assert.That(vertical.childForceExpandHeight, Is.True);
            Assert.That(firstLayoutElement.flexibleHeight, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(secondLayoutElement.flexibleHeight, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(firstFitter.enabled, Is.False);
            Assert.That(firstFitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.Unconstrained));
            Assert.That(firstRow.GetComponent<UIWrappingRowLayoutGroup>(), Is.Not.Null);
            Assert.That(firstRow.GetComponent<UIWrappingRowLayoutGroup>().enabled, Is.True);
        }

        [Test]
        public void UIColumn_ApplyLayout_AppliesConfiguredSpacing()
        {
            GameObject columnObject = CreateGameObject("Column");
            UIColumn column = columnObject.AddComponent<UIColumn>();
            column.Spacing = 18f;

            column.ApplyLayout(columnCount: 12);

            VerticalLayoutGroup vertical = columnObject.GetComponent<VerticalLayoutGroup>();
            Assert.That(vertical.spacing, Is.EqualTo(18f).Within(0.0001f));
        }

        [Test]
        public void UIBuilderObjectFactory_ResolvesColumnOwnerThroughScrollWrappers()
        {
            GameObject rowObject = CreateGameObject("Row");
            rowObject.AddComponent<UIRow>();
            GameObject columnObject = CreateGameObject("Column");
            UIColumn column = columnObject.AddComponent<UIColumn>();
            columnObject.transform.SetParent(rowObject.transform, false);
            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);

            Transform content = column.GetContentParent();
            GameObject nestedChild = CreateGameObject("Nested");
            nestedChild.transform.SetParent(content, false);

            Transform resolvedParent = (Transform)InvokePrivateStatic(
                typeof(UIBuilderObjectFactory),
                "ResolveRowInsertionParent",
                nestedChild.transform);

            Assert.That(resolvedParent, Is.EqualTo(content));
        }

        [Test]
        public void UIBuilderObjectFactory_CreateRow_UsesTopAnchoredDefaultRectTransform()
        {
            GameObject parentObject = CreateGameObject("Parent");

            GameObject rowObject = UIBuilderObjectFactory.CreateRow(parentObject.transform);
            RectTransform rowTransform = (RectTransform)rowObject.transform;

            Assert.That(rowTransform.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rowTransform.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(rowTransform.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(rowTransform.anchoredPosition.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void UIBuilderObjectFactory_ResolvesRowOwnerThroughScrollWrappers()
        {
            GameObject rowObject = CreateGameObject("Row");
            UIRow row = rowObject.AddComponent<UIRow>();
            GameObject columnObject = CreateGameObject("Column");
            UIColumn column = columnObject.AddComponent<UIColumn>();
            columnObject.transform.SetParent(rowObject.transform, false);
            column.Scrollable = true;
            column.ApplyLayout(columnCount: 12);

            GameObject nestedChild = CreateGameObject("Nested");
            nestedChild.transform.SetParent(column.GetContentParent(), false);

            UIRow resolvedRow = (UIRow)InvokePrivateStatic(
                typeof(UIBuilderObjectFactory),
                "ResolveRowOwner",
                nestedChild.transform);

            Assert.That(resolvedRow, Is.EqualTo(row));
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private RectTransform CreateColumn(Transform parent, int span)
        {
            GameObject columnObject = CreateGameObject("Column");
            columnObject.transform.SetParent(parent, false);
            UIColumn column = columnObject.AddComponent<UIColumn>();
            column.Span = span;
            column.ApplyLayout(columnCount: 12);
            return (RectTransform)columnObject.transform;
        }

        private static object InvokePrivateStatic(System.Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method {methodName} on {type.Name}.");
            return method.Invoke(null, args);
        }

        private static void InvokeNonPublic(object instance, string methodName, int repeatCount = 1)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method {methodName} on {instance.GetType().Name}.");
            for (int i = 0; i < repeatCount; i++)
            {
                method.Invoke(instance, null);
            }
        }
    }
}
