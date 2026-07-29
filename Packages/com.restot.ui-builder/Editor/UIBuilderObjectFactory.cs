using Restot.UIBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Restot.UIBuilder.Editor
{
    public static class UIBuilderObjectFactory
    {
        public static GameObject CreateRow(Transform parent)
        {
            GameObject row = CreateUIObject("Row", ResolveRowInsertionParent(parent));
            ConfigureRowRectTransform((RectTransform)row.transform);
            Undo.AddComponent<UIRow>(row);
            row.GetComponent<UIRow>().ApplyLayout(CurrentColumnSpacing(), CurrentColumnCount());
            ApplyRowSpacingToLayoutParent(row.transform.parent);
            Selection.activeGameObject = row;
            return row;
        }

        public static GameObject CreateColumn(Transform parent, int span = 6)
        {
            GameObject column = CreateUIObject("Column", ResolveColumnInsertionParent(parent));
            UIColumn uiColumn = Undo.AddComponent<UIColumn>(column);
            uiColumn.Span = span;
            uiColumn.ApplyLayout(CurrentColumnCount());
            Selection.activeGameObject = column;
            return column;
        }

        public static GameObject InstantiatePrefab(UIBuilderPrefabEntry entry, Transform parent)
        {
            if (entry == null || entry.prefab == null)
            {
                return null;
            }

            Transform insertionParent = ResolvePrefabInsertionParent(parent);
            GameObject instance = PrefabUtility.InstantiatePrefab(entry.prefab, insertionParent) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(entry.prefab, insertionParent);
                instance.name = entry.prefab.name;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Add UI Builder Prefab");
            UIColumn column = ResolveColumnOwner(parent);
            if (column != null)
            {
                column.ApplyLayout(CurrentColumnCount());
            }

            UIRow row = ResolveRowOwner(parent);
            if (row != null)
            {
                row.ApplyLayout(CurrentColumnSpacing(), CurrentColumnCount());
            }

            Selection.activeGameObject = instance;
            return instance;
        }

        public static void PrepareCanvasFor16By9(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            Undo.RecordObject(canvas, "Prepare UI Builder Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            }

            Undo.RecordObject(scaler, "Prepare UI Builder Canvas");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            }

            VerticalLayoutGroup verticalLayout = canvas.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
            {
                verticalLayout = Undo.AddComponent<VerticalLayoutGroup>(canvas.gameObject);
            }

            Undo.RecordObject(verticalLayout, "Prepare UI Builder Canvas");
            verticalLayout.padding = new RectOffset();
            verticalLayout.spacing = CurrentRowSpacing();
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
        }

        public static void ReapplyLayout()
        {
            UIBuilderSettings settings = UIBuilderSettingsProvider.LoadOrCreate();
            float columnSpacing = settings.ColumnSpacing;
            float rowSpacing = settings.RowSpacing;
            int columnCount = settings.ColumnCount;

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !IsSceneObject(canvas.gameObject))
                {
                    continue;
                }

                if (canvas.TryGetComponent(out VerticalLayoutGroup verticalLayout))
                {
                    Undo.RecordObject(verticalLayout, "Reapply UI Builder Layout");
                    verticalLayout.spacing = rowSpacing;
                    EditorUtility.SetDirty(verticalLayout);
                }
            }

            UIRow[] rows = Object.FindObjectsByType<UIRow>(FindObjectsInactive.Include);
            for (int i = 0; i < rows.Length; i++)
            {
                UIRow row = rows[i];
                if (row == null || !IsSceneObject(row.gameObject))
                {
                    continue;
                }

                Undo.RecordObject(row, "Reapply UI Builder Layout");
                row.ApplyLayout(columnSpacing, columnCount);
                EditorUtility.SetDirty(row);
            }

            UIColumn[] columns = Object.FindObjectsByType<UIColumn>(FindObjectsInactive.Include);
            for (int i = 0; i < columns.Length; i++)
            {
                UIColumn column = columns[i];
                if (column == null || !IsSceneObject(column.gameObject))
                {
                    continue;
                }

                Undo.RecordObject(column, "Reapply UI Builder Layout");
                column.ApplyLayout(columnCount);
                EditorUtility.SetDirty(column);
            }

            EditorSceneManager.MarkAllScenesDirty();
        }

        public static bool IsValidRowParent(Transform parent)
        {
            Transform insertionParent = ResolveRowInsertionParent(parent);
            return insertionParent != null && insertionParent.GetComponent<RectTransform>() != null;
        }

        public static bool IsValidColumnParent(Transform parent)
        {
            return ResolveRowOwner(parent) != null;
        }

        public static bool IsValidPrefabParent(Transform parent)
        {
            return ResolveRowOwner(parent) != null || ResolveColumnOwner(parent) != null;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create UI Builder " + name);
            gameObject.transform.SetParent(parent, false);

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return gameObject;
        }

        private static void ConfigureRowRectTransform(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static float CurrentColumnSpacing()
        {
            return UIBuilderSettingsProvider.LoadOrCreate().ColumnSpacing;
        }

        private static float CurrentRowSpacing()
        {
            return UIBuilderSettingsProvider.LoadOrCreate().RowSpacing;
        }

        private static int CurrentColumnCount()
        {
            return UIBuilderSettingsProvider.LoadOrCreate().ColumnCount;
        }

        private static Transform ResolveRowInsertionParent(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            UIColumn column = ResolveColumnOwner(parent);
            if (column != null)
            {
                return column.GetContentParent();
            }

            return parent;
        }

        private static void ApplyRowSpacingToLayoutParent(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            if (parent.TryGetComponent(out VerticalLayoutGroup verticalLayout))
            {
                Undo.RecordObject(verticalLayout, "Update UI Builder Row Spacing");
                verticalLayout.spacing = CurrentRowSpacing();
            }
        }

        private static bool IsSceneObject(GameObject gameObject)
        {
            return gameObject.scene.IsValid() && !EditorUtility.IsPersistent(gameObject);
        }

        private static Transform ResolveColumnInsertionParent(Transform parent)
        {
            UIRow row = ResolveRowOwner(parent);
            return row != null ? row.transform : null;
        }

        private static Transform ResolvePrefabInsertionParent(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            UIColumn column = ResolveColumnOwner(parent);
            if (column != null)
            {
                return column.GetContentParent();
            }

            UIRow row = ResolveRowOwner(parent);
            return row != null ? row.transform : parent;
        }

        private static UIRow ResolveRowOwner(Transform target)
        {
            while (target != null)
            {
                if (target.TryGetComponent(out UIRow row))
                {
                    return row;
                }

                target = target.parent;
            }

            return null;
        }

        private static UIColumn ResolveColumnOwner(Transform target)
        {
            while (target != null)
            {
                if (target.TryGetComponent(out UIColumn column))
                {
                    return column;
                }

                target = target.parent;
            }

            return null;
        }
    }
}
