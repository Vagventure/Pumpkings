using Restot.UIBuilder;
using UnityEditor;
using UnityEngine;

namespace Restot.UIBuilder.Editor
{
    [InitializeOnLoad]
    public static class UIBuilderHierarchyDropHandler
    {
        private const string DragKey = "Restot.UIBuilder.DragPayload";

        static UIBuilderHierarchyDropHandler()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindowItemOnGUI;
        }

        public static void BeginDrag(UIBuilderDragPayload payload, string title)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(DragKey, payload);
            DragAndDrop.objectReferences = new Object[0];
            DragAndDrop.StartDrag(title);
        }

        private static UIBuilderDragPayload CurrentPayload()
        {
            return DragAndDrop.GetGenericData(DragKey) as UIBuilderDragPayload;
        }

        private static void OnHierarchyWindowItemOnGUI(EntityId entityId, Rect selectionRect)
        {
            Event current = Event.current;
            if (current == null || !selectionRect.Contains(current.mousePosition))
            {
                return;
            }

            UIBuilderDragPayload payload = CurrentPayload();
            if (payload == null)
            {
                return;
            }

            GameObject target = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (target == null)
            {
                return;
            }

            bool valid = IsValidDrop(payload, target.transform);
            if (current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                current.Use();
            }
            else if (current.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                DragAndDrop.AcceptDrag();
                if (valid)
                {
                    PerformDrop(payload, target.transform);
                }
                else
                {
                    Debug.LogWarning($"Restot UI Builder: cannot drop {payload.Kind} on {target.name}.");
                }

                DragAndDrop.SetGenericData(DragKey, null);
                current.Use();
            }
        }

        private static bool IsValidDrop(UIBuilderDragPayload payload, Transform target)
        {
            switch (payload.Kind)
            {
                case UIBuilderDragKind.Row:
                    return UIBuilderObjectFactory.IsValidRowParent(target);
                case UIBuilderDragKind.Column:
                    return UIBuilderObjectFactory.IsValidColumnParent(target);
                case UIBuilderDragKind.Prefab:
                    return payload.PrefabEntry != null && payload.PrefabEntry.prefab != null && UIBuilderObjectFactory.IsValidPrefabParent(target);
                default:
                    return false;
            }
        }

        private static void PerformDrop(UIBuilderDragPayload payload, Transform target)
        {
            switch (payload.Kind)
            {
                case UIBuilderDragKind.Row:
                    UIBuilderObjectFactory.CreateRow(target);
                    break;
                case UIBuilderDragKind.Column:
                    UIBuilderObjectFactory.CreateColumn(target);
                    break;
                case UIBuilderDragKind.Prefab:
                    UIBuilderObjectFactory.InstantiatePrefab(payload.PrefabEntry, target);
                    break;
            }
        }
    }
}
