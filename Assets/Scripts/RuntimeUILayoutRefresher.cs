using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUILayoutRefresher
{
    private const int MaxAncestorDepth = 12;

    public static void Refresh(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Refresh(target.transform as RectTransform);
    }

    public static void Refresh(Component target)
    {
        if (target == null)
        {
            return;
        }

        Refresh(target.transform as RectTransform);
    }

    public static void Refresh(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform current = target;
        for (int i = 0; current != null && i < MaxAncestorDepth; i++)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(current);
            current = current.parent as RectTransform;
        }

        Canvas.ForceUpdateCanvases();
    }

    public static void RefreshNowAndNextFrame(MonoBehaviour owner, Component target)
    {
        if (target == null)
        {
            return;
        }

        Refresh(target);

        if (owner != null && owner.isActiveAndEnabled)
        {
            owner.StartCoroutine(RefreshNextFrame(target));
        }
    }

    public static void RefreshNowAndNextFrame(MonoBehaviour owner, GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Refresh(target);

        if (owner != null && owner.isActiveAndEnabled)
        {
            owner.StartCoroutine(RefreshNextFrame(target));
        }
    }

    private static IEnumerator RefreshNextFrame(Component target)
    {
        yield return null;
        Refresh(target);
    }

    private static IEnumerator RefreshNextFrame(GameObject target)
    {
        yield return null;
        Refresh(target);
    }
}
