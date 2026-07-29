using Restot.UIBuilder;

namespace Restot.UIBuilder.Editor
{
    public enum UIBuilderDragKind
    {
        Row,
        Column,
        Prefab
    }

    public sealed class UIBuilderDragPayload
    {
        public UIBuilderDragPayload(UIBuilderDragKind kind, UIBuilderPrefabEntry prefabEntry = null)
        {
            Kind = kind;
            PrefabEntry = prefabEntry;
        }

        public UIBuilderDragKind Kind { get; }
        public UIBuilderPrefabEntry PrefabEntry { get; }
    }
}
