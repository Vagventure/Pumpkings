using UnityEngine;

namespace Restot.UIBuilder
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIScrollArea : MonoBehaviour
    {
        [SerializeField, HideInInspector] private UIColumn owner;

        public UIColumn Owner => owner;

        public void Initialize(UIColumn scrollOwner)
        {
            owner = scrollOwner;
        }
    }
}
