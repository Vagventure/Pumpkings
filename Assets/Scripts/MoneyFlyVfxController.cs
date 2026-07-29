using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyFlyVfxController : MonoBehaviour
{
    [Header("Overlay Canvas")]
    [SerializeField] private RectTransform animationRoot;
    [SerializeField] private RectTransform moneyTarget;
    [SerializeField] private RectTransform moneyIconPrefab;

    [Header("Animation")]
    [SerializeField] private Camera worldCamera;
    [SerializeField, Min(0.01f)] private float duration = 0.6f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly List<RectTransform> activeIcons = new List<RectTransform>();

    private void OnEnable()
    {
        ScoringService.TrashIncomeAwarded += HandleTrashIncomeAwarded;
    }

    private void OnDisable()
    {
        ScoringService.TrashIncomeAwarded -= HandleTrashIncomeAwarded;
        StopAllCoroutines();

        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            if (activeIcons[i] != null)
            {
                Destroy(activeIcons[i].gameObject);
            }
        }

        activeIcons.Clear();
    }

    private void HandleTrashIncomeAwarded(Trash trash, int income)
    {
        if (trash == null
            || income <= 0
            || animationRoot == null
            || moneyTarget == null
            || moneyIconPrefab == null)
        {
            return;
        }

        Camera camera = InputRaycastCameraResolver.ResolveFallback(worldCamera);

        if (camera == null)
        {
            return;
        }

        Vector3 screenPosition = camera.WorldToScreenPoint(trash.transform.position);

        if (screenPosition.z <= 0f
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                animationRoot,
                screenPosition,
                null,
                out Vector2 startPosition))
        {
            return;
        }

        RectTransform icon = Instantiate(moneyIconPrefab, animationRoot);
        PrepareOverlayIcon(icon, startPosition);
        activeIcons.Add(icon);
        StartCoroutine(AnimateIcon(icon, startPosition));
    }

    private IEnumerator AnimateIcon(RectTransform icon, Vector2 startPosition)
    {
        float elapsedTime = 0f;

        while (icon != null && elapsedTime < duration)
        {
            Vector2 targetPosition = GetMoneyTargetPosition();
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float curvedProgress = movementCurve != null
                ? movementCurve.Evaluate(progress)
                : progress;

            PrepareOverlayIcon(
                icon,
                Vector2.LerpUnclamped(startPosition, targetPosition, curvedProgress));

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (icon != null)
        {
            PrepareOverlayIcon(icon, GetMoneyTargetPosition());
            activeIcons.Remove(icon);
            Destroy(icon.gameObject);
        }
    }

    public static void PrepareOverlayIcon(RectTransform icon, Vector2 anchoredPosition)
    {
        if (icon == null)
        {
            return;
        }

        icon.localRotation = Quaternion.identity;
        icon.localScale = Vector3.one;
        icon.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
    }

    private Vector2 GetMoneyTargetPosition()
    {
        Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(null, moneyTarget.position);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            animationRoot,
            targetScreenPosition,
            null,
            out Vector2 targetPosition)
            ? targetPosition
            : Vector2.zero;
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
    }
}
