using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    [CustomEditor(typeof(UIRow))]
    public sealed class UIRowEditor : UnityEditor.Editor
    {
        private SerializedProperty padding;
        private SerializedProperty overrideColumnSpacing;
        private SerializedProperty columnSpacing;
        private SerializedProperty fixedHeight;
        private SerializedProperty height;
        private void OnEnable()
        {
            padding = serializedObject.FindProperty("padding");
            overrideColumnSpacing = serializedObject.FindProperty("overrideColumnSpacing");
            columnSpacing = serializedObject.FindProperty("columnSpacing");
            fixedHeight = serializedObject.FindProperty("fixedHeight");
            height = serializedObject.FindProperty("height");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Restot Row", EditorStyles.boldLabel);
            DrawPadding(padding);
            EditorGUILayout.PropertyField(overrideColumnSpacing, new GUIContent("Override Column Spacing"));
            if (overrideColumnSpacing.boolValue)
            {
                EditorGUILayout.PropertyField(columnSpacing, new GUIContent("Column Spacing"));
            }
            else
            {
                EditorGUILayout.LabelField("Global Column Spacing", UIBuilderSettingsProvider.LoadOrCreate().ColumnSpacing.ToString("0.##"));
            }

            EditorGUILayout.PropertyField(fixedHeight, new GUIContent("Fixed Height"));
            if (fixedHeight.boolValue)
            {
                EditorGUILayout.PropertyField(height, new GUIContent("Height"));
            }

            UIRow row = (UIRow)target;
            if (serializedObject.ApplyModifiedProperties())
            {
                UIBuilderSettings settings = UIBuilderSettingsProvider.LoadOrCreate();
                row.ApplyLayout(settings.ColumnSpacing, settings.ColumnCount);
                EditorUtility.SetDirty(row);
            }

            int columnCount = UIBuilderSettingsProvider.LoadOrCreate().ColumnCount;
            int total = row.ChildColumnSpanTotal(columnCount);
            if (total > columnCount)
            {
                EditorGUILayout.HelpBox($"Column spans total {total}/{columnCount}. Columns will wrap onto additional lines.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Column spans total {total}/{columnCount}.", MessageType.Info);
            }
        }

        internal static void DrawPadding(SerializedProperty paddingProperty)
        {
            EditorGUILayout.LabelField("Padding", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(paddingProperty.FindPropertyRelative("left"));
            EditorGUILayout.PropertyField(paddingProperty.FindPropertyRelative("right"));
            EditorGUILayout.PropertyField(paddingProperty.FindPropertyRelative("top"));
            EditorGUILayout.PropertyField(paddingProperty.FindPropertyRelative("bottom"));
            EditorGUI.indentLevel--;
        }
    }
}
