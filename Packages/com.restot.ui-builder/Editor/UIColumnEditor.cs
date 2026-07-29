using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    [CustomEditor(typeof(UIColumn))]
    public sealed class UIColumnEditor : UnityEditor.Editor
    {
        private SerializedProperty span;
        private SerializedProperty padding;
        private SerializedProperty fixedHeight;
        private SerializedProperty height;
        private SerializedProperty scrollable;
        private SerializedProperty spacing;
        private SerializedProperty autoScrollOverflowThresholdPercent;
        private SerializedProperty autoScrollFollowLatestTolerancePercent;
        private SerializedProperty autoScrollPreviousItemVisibilityPercent;
        private SerializedProperty autoScrollDuration;
        private SerializedProperty childrenHeightMode;

        private void OnEnable()
        {
            span = serializedObject.FindProperty("span");
            padding = serializedObject.FindProperty("padding");
            fixedHeight = serializedObject.FindProperty("fixedHeight");
            height = serializedObject.FindProperty("height");
            scrollable = serializedObject.FindProperty("scrollable");
            spacing = serializedObject.FindProperty("spacing");
            autoScrollOverflowThresholdPercent = serializedObject.FindProperty("autoScrollOverflowThresholdPercent");
            autoScrollFollowLatestTolerancePercent = serializedObject.FindProperty("autoScrollFollowLatestTolerancePercent");
            autoScrollPreviousItemVisibilityPercent = serializedObject.FindProperty("autoScrollPreviousItemVisibilityPercent");
            autoScrollDuration = serializedObject.FindProperty("autoScrollDuration");
            childrenHeightMode = serializedObject.FindProperty("childrenHeightMode");
        }

        public override void OnInspectorGUI()
        {
            UIBuilderSettings settings = UIBuilderSettingsProvider.LoadOrCreate();
            serializedObject.Update();

            EditorGUILayout.LabelField("Restot Column", EditorStyles.boldLabel);
            span.intValue = EditorGUILayout.IntSlider("Span", span.intValue, 1, settings.ColumnCount);
            EditorGUILayout.LabelField("Width Share", UIBuilderLayoutCalculator.WidthShare(span.intValue, settings.ColumnCount).ToString("P0"));
            EditorGUILayout.PropertyField(scrollable, new GUIContent("Scrollable"));
            if (!scrollable.boolValue)
            {
                EditorGUILayout.PropertyField(spacing, new GUIContent("Spacing"));
            }
            if (scrollable.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Auto Scroll Parameters", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(spacing, new GUIContent("Spacing"));
                EditorGUILayout.PropertyField(autoScrollOverflowThresholdPercent, new GUIContent("Overflow Threshold %"));
                EditorGUILayout.PropertyField(autoScrollFollowLatestTolerancePercent, new GUIContent("Follow Latest Tolerance %"));
                EditorGUILayout.PropertyField(autoScrollPreviousItemVisibilityPercent, new GUIContent("Previous Item Visibility %"));
                EditorGUILayout.PropertyField(autoScrollDuration, new GUIContent("Auto Scroll Duration"));
            }
            EditorGUILayout.PropertyField(fixedHeight, new GUIContent("Fixed Height"));
            if (fixedHeight.boolValue)
            {
                EditorGUILayout.PropertyField(height, new GUIContent("Height"));
            }
            EditorGUILayout.PropertyField(childrenHeightMode, new GUIContent("Children Height Mode"));
            UIRowEditor.DrawPadding(padding);

            UIColumn column = (UIColumn)target;
            if (serializedObject.ApplyModifiedProperties())
            {
                column.ApplyLayout(settings.ColumnCount);
                EditorUtility.SetDirty(column);
            }
        }
    }
}
