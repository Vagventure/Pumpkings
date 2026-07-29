using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SpawnService))]
public class SpawnServiceEditor : Editor
{
    private SerializedProperty trashTypes;
    private SerializedProperty poolParent;
    private SerializedProperty spawnLimit;
    private SerializedProperty spawnTickInterval;
    private SerializedProperty spawnTickRandomVariation;
    private ReorderableList trashTypesList;

    private void OnEnable()
    {
        trashTypes = serializedObject.FindProperty("trashTypes");
        poolParent = serializedObject.FindProperty("poolParent");
        spawnLimit = serializedObject.FindProperty("spawnLimit");
        spawnTickInterval = serializedObject.FindProperty("spawnTickInterval");
        spawnTickRandomVariation = serializedObject.FindProperty("spawnTickRandomVariation");

        trashTypesList = new ReorderableList(serializedObject, trashTypes, true, true, true, true);
        trashTypesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Trash Types");
        trashTypesList.elementHeightCallback = GetTrashTypeElementHeight;
        trashTypesList.drawElementCallback = DrawTrashTypeElement;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(poolParent);
        EditorGUILayout.PropertyField(spawnLimit);

        EditorGUILayout.Space();
        trashTypesList.DoLayoutList();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn Timing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnTickInterval);
        EditorGUILayout.PropertyField(spawnTickRandomVariation);

        serializedObject.ApplyModifiedProperties();
    }

    private float GetTrashTypeElementHeight(int index)
    {
        SerializedProperty element = trashTypes.GetArrayElementAtIndex(index);
        SerializedProperty spawnMode = element.FindPropertyRelative("spawnMode");
        SerializedProperty paths = element.FindPropertyRelative("paths");
        int lineCount = 3;

        if (spawnMode.enumValueIndex == (int)SpawnMode.EventSpawn)
        {
            SerializedProperty eventSpawnPattern = element.FindPropertyRelative("eventSpawnPattern");
            lineCount = eventSpawnPattern.enumValueIndex == (int)EventSpawnPattern.DirectionalBurst
                ? 8
                : 6;
        }

        return lineCount * EditorGUIUtility.singleLineHeight
            + EditorGUI.GetPropertyHeight(paths, true)
            + (lineCount + 2) * EditorGUIUtility.standardVerticalSpacing;
    }

    private void DrawTrashTypeElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = trashTypes.GetArrayElementAtIndex(index);
        SerializedProperty data = element.FindPropertyRelative("data");
        SerializedProperty spawnArea = element.FindPropertyRelative("spawnArea");
        SerializedProperty paths = element.FindPropertyRelative("paths");
        SerializedProperty spawnMode = element.FindPropertyRelative("spawnMode");
        SerializedProperty spawnTrigger = element.FindPropertyRelative("spawnTrigger");
        SerializedProperty eventSpawnPattern = element.FindPropertyRelative("eventSpawnPattern");
        SerializedProperty eventSpawnCount = element.FindPropertyRelative("eventSpawnCount");
        SerializedProperty directionalBurstDurationRange = element.FindPropertyRelative("directionalBurstDurationRange");
        SerializedProperty directionalEdgeInsetRange = element.FindPropertyRelative("directionalEdgeInsetRange");

        rect.y += EditorGUIUtility.standardVerticalSpacing;
        rect.height = EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(rect, data);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(rect, spawnArea);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        rect.height = EditorGUI.GetPropertyHeight(paths, true);
        EditorGUI.PropertyField(rect, paths, new GUIContent("Trash Paths"), true);
        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        rect.height = EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(rect, spawnMode, new GUIContent("Spawn Type"));
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (spawnMode.enumValueIndex != (int)SpawnMode.EventSpawn)
        {
            return;
        }

        EditorGUI.PropertyField(rect, spawnTrigger);
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(rect, eventSpawnPattern, new GUIContent("Event Pattern"));
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(rect, eventSpawnCount, new GUIContent("Spawn Count"));

        if (eventSpawnPattern.enumValueIndex == (int)EventSpawnPattern.Instant)
        {
            return;
        }

        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(rect, directionalBurstDurationRange, new GUIContent("Burst Duration Range"));
        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(rect, directionalEdgeInsetRange, new GUIContent("Edge Inset Range"));
    }
}
