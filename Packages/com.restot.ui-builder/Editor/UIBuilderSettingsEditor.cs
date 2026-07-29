using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    [CustomEditor(typeof(UIBuilderSettings))]
    public sealed class UIBuilderSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty columnSpacing;
        private SerializedProperty rowSpacing;
        private SerializedProperty columnCount;
        private SerializedProperty prefabRegistry;

        private void OnEnable()
        {
            columnSpacing = serializedObject.FindProperty("columnSpacing");
            rowSpacing = serializedObject.FindProperty("rowSpacing");
            columnCount = serializedObject.FindProperty("columnCount");
            prefabRegistry = serializedObject.FindProperty("prefabRegistry");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(columnCount, new GUIContent("Column Count"));
            EditorGUILayout.PropertyField(columnSpacing, new GUIContent("Column Spacing"));
            EditorGUILayout.PropertyField(rowSpacing, new GUIContent("Row Spacing"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(prefabRegistry, new GUIContent("Prefab Registry"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
