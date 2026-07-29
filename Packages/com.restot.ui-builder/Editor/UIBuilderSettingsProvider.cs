using System.IO;
using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    public static class UIBuilderSettingsProvider
    {
        public const string DefaultSettingsPath = "Assets/RestotUIBuilder/RestotUIBuilderSettings.asset";

        public static UIBuilderSettings LoadOrCreate()
        {
            UIBuilderSettings settings = AssetDatabase.LoadAssetAtPath<UIBuilderSettings>(DefaultSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            string directory = Path.GetDirectoryName(DefaultSettingsPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets", "RestotUIBuilder");
            }

            settings = ScriptableObject.CreateInstance<UIBuilderSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
