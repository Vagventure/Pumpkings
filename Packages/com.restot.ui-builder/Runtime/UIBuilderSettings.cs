using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Restot.UIBuilder
{
    public enum UIBuilderEditableFieldKind
    {
        Text,
        Sprite,
        Color,
        Bool,
        Number,
        String
    }

    [Serializable]
    public sealed class UIBuilderEditableField
    {
        public string label;
        public string childPath;
        public string componentTypeName;
        public string serializedPropertyPath;
        public UIBuilderEditableFieldKind kind;
    }

    [Serializable]
    public sealed class UIBuilderPrefabEntry
    {
        public string displayName;
        public string category = "General";
        public GameObject prefab;
        public Texture2D icon;
        public bool isContainer;
        public List<UIBuilderEditableField> editableFields = new List<UIBuilderEditableField>();

        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName)
            ? prefab != null ? prefab.name : "Unnamed"
            : displayName;

        public string CategoryOrFallback => string.IsNullOrWhiteSpace(category) ? "General" : category;
    }

    [CreateAssetMenu(fileName = "RestotUIBuilderSettings", menuName = "Restot/UI Builder Settings")]
    public sealed class UIBuilderSettings : ScriptableObject
    {
        [FormerlySerializedAs("gutter")]
        [Min(0f)] public float columnSpacing = UIBuilderConstants.DefaultGutter;
        [Min(0f)] public float rowSpacing = UIBuilderConstants.DefaultGutter;
        [Min(1)] public int columnCount = UIBuilderConstants.DefaultColumnCount;
        public List<UIBuilderPrefabEntry> prefabRegistry = new List<UIBuilderPrefabEntry>();

        public int ColumnCount => UIBuilderLayoutCalculator.ClampColumnCount(columnCount);
        public float ColumnSpacing => Mathf.Max(0f, columnSpacing);
        public float RowSpacing => Mathf.Max(0f, rowSpacing);
    }
}
