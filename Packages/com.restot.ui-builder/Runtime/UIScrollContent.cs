using UnityEngine;

namespace Restot.UIBuilder
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIScrollContent : MonoBehaviour
    {
        [SerializeField, HideInInspector] private UIColumn owner;

        public UIColumn Owner => owner;

        public void Initialize(UIColumn scrollOwner)
        {
            owner = scrollOwner;
        }

        private void OnTransformChildrenChanged()
        {
            if (owner != null)
            {
                owner.NotifyScrollContentChanged();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (owner != null)
            {
                owner.NotifyScrollContentGeometryChanged();
            }
        }
    }
}
