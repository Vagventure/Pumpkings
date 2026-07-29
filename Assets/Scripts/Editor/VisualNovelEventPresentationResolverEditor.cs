using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisualNovelEventPresentationResolver))]
public class VisualNovelEventPresentationResolverEditor : Editor
{
    private SerializedProperty visualNovelPanel;

    private void OnEnable()
    {
        visualNovelPanel = serializedObject.FindProperty("visualNovelPanel");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(visualNovelPanel, new GUIContent("Visual Novel Panel"));
        ValidateVisualNovelPanel();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script",
                MonoScript.FromMonoBehaviour((VisualNovelEventPresentationResolver)target),
                typeof(VisualNovelEventPresentationResolver),
                false);
        }
    }

    private void ValidateVisualNovelPanel()
    {
        GameObject panel = visualNovelPanel.objectReferenceValue as GameObject;

        if (panel == null)
        {
            return;
        }

        if (EditorUtility.IsPersistent(panel))
        {
            EditorGUILayout.HelpBox("Assign a scene GameObject instance, not a prefab asset.", MessageType.Warning);
            return;
        }

        VisualNovelPanelBindings bindings = panel.GetComponent<VisualNovelPanelBindings>();

        if (bindings == null)
        {
            EditorGUILayout.HelpBox("Assigned GameObject must contain VisualNovelPanelBindings.", MessageType.Error);
            return;
        }

        if (!bindings.HasRewardBindings())
        {
            EditorGUILayout.HelpBox("VisualNovelPanelBindings is missing Reward Container or Reward Prefab.", MessageType.Warning);
        }
    }
}
