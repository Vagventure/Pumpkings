using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    public sealed class UIBuilderSettingsWindow : EditorWindow
    {
        private UnityEditor.Editor settingsEditor;
        private UIBuilderSettings settings;

        public static void ShowWindow()
        {
            UIBuilderSettingsWindow window = GetWindow<UIBuilderSettingsWindow>("Restot UI Builder Settings");
            window.minSize = new Vector2(360f, 320f);
            window.Focus();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            if (settingsEditor != null)
            {
                Object.DestroyImmediate(settingsEditor);
            }
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                LoadSettings();
            }

            EditorGUILayout.LabelField("Restot UI Builder Settings", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Settings Asset", settings, typeof(UIBuilderSettings), false);

            if (GUILayout.Button("Select Settings Asset"))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }

            EditorGUILayout.Space();
            if (settingsEditor != null)
            {
                settingsEditor.OnInspectorGUI();
            }
        }

        private void LoadSettings()
        {
            settings = UIBuilderSettingsProvider.LoadOrCreate();
            if (settingsEditor != null)
            {
                Object.DestroyImmediate(settingsEditor);
            }

            settingsEditor = UnityEditor.Editor.CreateEditor(settings);
        }
    }
}
