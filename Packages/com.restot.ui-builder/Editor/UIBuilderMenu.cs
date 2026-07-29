using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Restot.UIBuilder.Editor
{
    public static class UIBuilderMenu
    {
        [MenuItem("GameObject/Restot UI Builder/Create Row", false, 10)]
        public static void CreateRow()
        {
            if (Selection.activeTransform != null && UIBuilderObjectFactory.IsValidRowParent(Selection.activeTransform))
            {
                UIBuilderObjectFactory.CreateRow(Selection.activeTransform);
            }
        }

        [MenuItem("GameObject/Restot UI Builder/Create Row", true)]
        public static bool ValidateCreateRow()
        {
            return Selection.activeTransform != null && UIBuilderObjectFactory.IsValidRowParent(Selection.activeTransform);
        }

        [MenuItem("GameObject/Restot UI Builder/Create Column", false, 11)]
        public static void CreateColumn()
        {
            if (Selection.activeTransform != null && UIBuilderObjectFactory.IsValidColumnParent(Selection.activeTransform))
            {
                UIBuilderObjectFactory.CreateColumn(Selection.activeTransform);
            }
        }

        [MenuItem("GameObject/Restot UI Builder/Create Column", true)]
        public static bool ValidateCreateColumn()
        {
            return Selection.activeTransform != null && UIBuilderObjectFactory.IsValidColumnParent(Selection.activeTransform);
        }

        [MenuItem("GameObject/Restot UI Builder/Prepare Canvas 16:9", false, 12)]
        public static void PrepareCanvas()
        {
            Canvas canvas = Selection.activeTransform != null ? Selection.activeTransform.GetComponent<Canvas>() : null;
            UIBuilderObjectFactory.PrepareCanvasFor16By9(canvas);
        }

        [MenuItem("GameObject/Restot UI Builder/Prepare Canvas 16:9", true)]
        public static bool ValidatePrepareCanvas()
        {
            return Selection.activeTransform != null && Selection.activeTransform.GetComponent<Canvas>() != null;
        }

        [MenuItem("Window/Restot UI Builder/Settings")]
        public static void OpenSettings()
        {
            UIBuilderSettingsWindow.ShowWindow();
        }

        [MenuItem("Window/Restot UI Builder/Palette")]
        public static void OpenPalette()
        {
            UIBuilderPaletteWindow.ShowWindow();
        }
    }
}
