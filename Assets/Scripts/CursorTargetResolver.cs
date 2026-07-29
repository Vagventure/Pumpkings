using UnityEngine;
using UnityEngine.UI;

public static class CursorTargetResolver
{
    public static bool IsCollectableTrash(Trash trash)
    {
        return trash != null
            && trash.gameObject.activeInHierarchy
            && !trash.IsBeingCollected;
    }

    public static bool IsClickableUi(GameObject hitObject)
    {
        if (hitObject == null || !hitObject.activeInHierarchy)
        {
            return false;
        }

        GrabCursorTarget marker = hitObject.GetComponentInParent<GrabCursorTarget>();

        if (marker != null)
        {
            return marker.IsInteractable;
        }

        Selectable selectable = hitObject.GetComponentInParent<Selectable>();

        return selectable != null
            && selectable.isActiveAndEnabled
            && selectable.IsInteractable();
    }
}
