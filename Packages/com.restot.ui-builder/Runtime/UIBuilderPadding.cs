using System;
using UnityEngine;

namespace Restot.UIBuilder
{
    [Serializable]
    public struct UIBuilderPadding
    {
        [Min(0f)] public float left;
        [Min(0f)] public float right;
        [Min(0f)] public float top;
        [Min(0f)] public float bottom;

        public UIBuilderPadding(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public RectOffset ToRectOffset()
        {
            return new RectOffset(
                Mathf.RoundToInt(Mathf.Max(0f, left)),
                Mathf.RoundToInt(Mathf.Max(0f, right)),
                Mathf.RoundToInt(Mathf.Max(0f, top)),
                Mathf.RoundToInt(Mathf.Max(0f, bottom)));
        }

        public float Horizontal => Mathf.Max(0f, left) + Mathf.Max(0f, right);
        public float Vertical => Mathf.Max(0f, top) + Mathf.Max(0f, bottom);
    }
}
