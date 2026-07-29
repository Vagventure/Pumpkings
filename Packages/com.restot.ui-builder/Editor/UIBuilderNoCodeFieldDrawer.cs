using System;
using System.Reflection;
using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    public static class UIBuilderNoCodeFieldDrawer
    {
        public static void DrawForSelection(UIBuilderSettings settings)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || settings == null)
            {
                return;
            }

            UIBuilderPrefabEntry entry = FindEntryForSelection(settings, selected);
            if (entry == null || entry.editableFields == null || entry.editableFields.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("No-Code Fields", EditorStyles.boldLabel);

            foreach (UIBuilderEditableField field in entry.editableFields)
            {
                DrawField(selected, field);
            }
        }

        private static UIBuilderPrefabEntry FindEntryForSelection(UIBuilderSettings settings, GameObject selected)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(selected);
            foreach (UIBuilderPrefabEntry entry in settings.prefabRegistry)
            {
                if (entry != null && entry.prefab != null && (entry.prefab == source || entry.prefab == selected))
                {
                    return entry;
                }
            }

            return null;
        }

        private static void DrawField(GameObject selected, UIBuilderEditableField field)
        {
            if (field == null)
            {
                return;
            }

            Transform targetTransform = string.IsNullOrEmpty(field.childPath) ? selected.transform : selected.transform.Find(field.childPath);
            if (targetTransform == null)
            {
                EditorGUILayout.HelpBox($"Missing child path: {field.childPath}", MessageType.Warning);
                return;
            }

            Type componentType = ResolveType(field.componentTypeName);
            if (componentType == null)
            {
                EditorGUILayout.HelpBox($"Missing component type: {field.componentTypeName}", MessageType.Warning);
                return;
            }

            Component component = targetTransform.GetComponent(componentType);
            if (component == null)
            {
                EditorGUILayout.HelpBox($"Missing component {componentType.Name} on {targetTransform.name}.", MessageType.Warning);
                return;
            }

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(field.serializedPropertyPath);
            if (property == null)
            {
                EditorGUILayout.HelpBox($"Missing property: {field.serializedPropertyPath}", MessageType.Warning);
                return;
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, new GUIContent(string.IsNullOrEmpty(field.label) ? property.displayName : field.label), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(component);
            }
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            Type direct = Type.GetType(typeName);
            if (direct != null)
            {
                return direct;
            }

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type.Name == typeName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static Type[] GetLoadableTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return Array.FindAll(exception.Types, type => type != null);
            }
        }
    }
}
