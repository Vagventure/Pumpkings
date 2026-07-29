using System.Collections.Generic;
using System.Linq;
using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    public sealed class UIBuilderPaletteWindow : EditorWindow
    {
        private UIBuilderSettings settings;
        private Vector2 scroll;

        public static void ShowWindow()
        {
            UIBuilderPaletteWindow window = GetWindow<UIBuilderPaletteWindow>("Restot UI Builder Palette");
            window.minSize = new Vector2(320f, 420f);
            window.Focus();
        }

        private void OnEnable()
        {
            settings = UIBuilderSettingsProvider.LoadOrCreate();
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                settings = UIBuilderSettingsProvider.LoadOrCreate();
            }

            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawLayoutItems();
            DrawPrefabEntries();
            EditorGUILayout.Space(8f);
            UIBuilderNoCodeFieldDrawer.DrawForSelection(settings);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Restot UI Builder", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reapply Layout", EditorStyles.toolbarButton, GUILayout.Width(96f)))
            {
                UIBuilderObjectFactory.ReapplyLayout();
            }
            if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                UIBuilderSettingsWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLayoutItems()
        {
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            DrawDraggableItem("Row", "Drop onto any UI RectTransform.", new UIBuilderDragPayload(UIBuilderDragKind.Row));
            DrawDraggableItem("Column", "Drop onto a UIRow.", new UIBuilderDragPayload(UIBuilderDragKind.Column));

            Transform selection = Selection.activeTransform;
            using (new EditorGUI.DisabledScope(selection == null || !UIBuilderObjectFactory.IsValidRowParent(selection)))
            {
                if (GUILayout.Button("Add Row To Selection"))
                {
                    UIBuilderObjectFactory.CreateRow(selection);
                }
            }

            using (new EditorGUI.DisabledScope(selection == null || !UIBuilderObjectFactory.IsValidColumnParent(selection)))
            {
                if (GUILayout.Button("Add Column To Selected Row"))
                {
                    UIBuilderObjectFactory.CreateColumn(selection);
                }
            }
        }

        private void DrawPrefabEntries()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

            List<UIBuilderPrefabEntry> entries = settings.prefabRegistry
                .Where(entry => entry != null && entry.prefab != null)
                .OrderBy(entry => entry.CategoryOrFallback)
                .ThenBy(entry => entry.DisplayNameOrFallback)
                .ToList();

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Register prefabs in Window > Restot UI Builder > Settings.", MessageType.Info);
                return;
            }

            foreach (IGrouping<string, UIBuilderPrefabEntry> group in entries.GroupBy(entry => entry.CategoryOrFallback))
            {
                EditorGUILayout.LabelField(group.Key, EditorStyles.miniBoldLabel);
                foreach (UIBuilderPrefabEntry entry in group)
                {
                    DrawPrefabItem(entry);
                }
            }
        }

        private void DrawPrefabItem(UIBuilderPrefabEntry entry)
        {
            GUIContent content = new GUIContent(entry.DisplayNameOrFallback, entry.icon != null ? entry.icon : AssetPreview.GetMiniThumbnail(entry.prefab));
            DrawDraggableItem(content, $"Drop {entry.DisplayNameOrFallback} onto a UIRow or UIColumn.", new UIBuilderDragPayload(UIBuilderDragKind.Prefab, entry));

            Transform selection = Selection.activeTransform;
            using (new EditorGUI.DisabledScope(selection == null || !UIBuilderObjectFactory.IsValidPrefabParent(selection)))
            {
                if (GUILayout.Button($"Add {entry.DisplayNameOrFallback} To Selection"))
                {
                    UIBuilderObjectFactory.InstantiatePrefab(entry, selection);
                }
            }
        }

        private void DrawDraggableItem(string label, string tooltip, UIBuilderDragPayload payload)
        {
            DrawDraggableItem(new GUIContent(label), tooltip, payload);
        }

        private void DrawDraggableItem(GUIContent content, string tooltip, UIBuilderDragPayload payload)
        {
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.helpBox, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
            GUI.Box(rect, new GUIContent(content.text, content.image, tooltip), EditorStyles.helpBox);

            Event current = Event.current;
            if (current.type == EventType.MouseDrag && rect.Contains(current.mousePosition))
            {
                UIBuilderHierarchyDropHandler.BeginDrag(payload, content.text);
                current.Use();
            }
        }
    }
}
