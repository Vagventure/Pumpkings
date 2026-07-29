using UnityEngine;
using UnityEngine.UI;

public class GrabCursorTarget : MonoBehaviour
{
    [SerializeField] private Selectable selectable;

    public bool IsInteractable
    {
        get
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            Selectable resolvedSelectable = selectable != null
                ? selectable
                : GetComponentInParent<Selectable>();

            return resolvedSelectable == null
                || (resolvedSelectable.isActiveAndEnabled && resolvedSelectable.IsInteractable());
        }
    }
}
