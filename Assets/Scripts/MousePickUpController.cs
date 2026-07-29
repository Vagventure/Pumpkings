using System;
using UnityEngine;

// Publishes confirmed trash pickup. Movement decides when the player is close enough to collect.
public class MousePickUpController : MonoBehaviour
{
    public static event Action<Trash> OnTrashClicked;

    public static void CollectTrash(Trash trash)
    {
        if (trash == null)
        {
            return;
        }

        OnTrashClicked?.Invoke(trash);
    }
}
