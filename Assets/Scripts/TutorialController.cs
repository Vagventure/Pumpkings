using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    private enum PointerMode
    {
        None,
        Bottle,
        ShopItem
    }

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform pointerRect;
    [SerializeField] private Image pointerImage;
    [SerializeField] private Transform playerCharacter;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SpawnService spawnService;
    [SerializeField] private ShopController shopController;

    [Header("Placement")]
    [SerializeField] private Vector3 firstBottleNormalizedSpawnPosition = new Vector3(0.1f, 0.5f, 0.1f);
    [SerializeField, Min(0f)] private float bottleDistance = 90f;
    [FormerlySerializedAs("shopItemDistance")]
    [SerializeField, Min(0f)] private float shopItemGap = 24f;
    [SerializeField, Min(0f)] private float movementDistance = 10f;

    [Header("Pulse")]
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.7f;
    [SerializeField, Min(0f)] private float pulseMinScale = 0.9f;
    [SerializeField, Min(0f)] private float pulseMaxScale = 1.1f;

    [Header("Entrance And Exit")]
    [SerializeField, Min(0f)] private float entranceDuration = 0.18f;
    [SerializeField, Min(0f)] private float exitDuration = 0.12f;
    [SerializeField, Min(0f)] private float entranceStartScale = 0.75f;
    [SerializeField, Min(0f)] private float entranceOvershootScale = 1.15f;
    [SerializeField] private Color flashColor = Color.white;

    private PointerMode pointerMode;
    private Trash bottleTarget;
    private RewardItemView pendingShopItemTarget;
    private RewardItemView shopItemTarget;
    private Coroutine pointerAnimation;
    private Coroutine pendingShopPresentation;
    private Coroutine armBottlePointerForWaveMinimum;
    private Vector3 basePointerScale = Vector3.one;
    private Color basePointerColor = Color.white;
    private bool bottleTutorialStarted;
    private bool shopTutorialStarted;
    private bool pointerRequested;
    private bool bottlePointerWaitingForWaveMinimum;
    private readonly Vector3[] shopTargetCorners = new Vector3[4];

    public bool IsPointerVisible => pointerRect != null && pointerRect.gameObject.activeSelf;

    private void Awake()
    {
        CachePointerDefaults();
        SetPointerObjectActive(false);
    }

    private void OnEnable()
    {
        SpawnService.TrashAdded += HandleTrashAdded;
        SpawnService.TrashRemoved += HandleTrashRemoved;
        RewardItemView.Clicked += HandleRewardItemClicked;
        SpawnTriggerEvents.Triggered += HandleSpawnTriggered;

        if (shopController != null)
        {
            shopController.ItemUnlocked += HandleShopItemUnlocked;
        }

        if (spawnService != null && !bottleTutorialStarted)
        {
            spawnService.SetNextSpawnNormalizedPosition(
                TrashType.Bottle,
                firstBottleNormalizedSpawnPosition);
        }
    }

    private void OnDisable()
    {
        SpawnService.TrashAdded -= HandleTrashAdded;
        SpawnService.TrashRemoved -= HandleTrashRemoved;
        RewardItemView.Clicked -= HandleRewardItemClicked;
        SpawnTriggerEvents.Triggered -= HandleSpawnTriggered;

        if (shopController != null)
        {
            shopController.ItemUnlocked -= HandleShopItemUnlocked;
        }

        if (spawnService != null)
        {
            spawnService.SetSpawnBlocked(TrashType.Bottle, false);

            if (!bottleTutorialStarted)
            {
                spawnService.ClearNextSpawnNormalizedPosition(TrashType.Bottle);
            }
        }

        StopAllPresentationCoroutines();
        pointerMode = PointerMode.None;
        bottleTarget = null;
        bottlePointerWaitingForWaveMinimum = false;
        pendingShopItemTarget = null;
        shopItemTarget = null;
        pointerRequested = false;
        SetPointerObjectActive(false);
    }

    private void HandleTrashAdded(Trash trash)
    {
        if (bottleTutorialStarted
            || trash == null
            || trash.TrashType != TrashType.Bottle)
        {
            return;
        }

        bottleTutorialStarted = true;

        if (!CanPresentBottlePointer())
        {
            Debug.LogError($"TutorialController: Cannot present the bottle pointer on '{name}'. Check its Canvas, pointer, player, camera, and Spawn Service references.");
            return;
        }

        bottleTarget = trash;
        spawnService.SetSpawnBlocked(TrashType.Bottle, true);

        if (armBottlePointerForWaveMinimum != null)
        {
            StopCoroutine(armBottlePointerForWaveMinimum);
        }

        armBottlePointerForWaveMinimum = StartCoroutine(ArmBottlePointerForNextWaveEvent());
    }

    private IEnumerator ArmBottlePointerForNextWaveEvent()
    {
        // TrashAdded is raised from the first WaveSpawnEvent. Waiting one frame
        // prevents that same event from immediately revealing the pointer.
        yield return null;
        armBottlePointerForWaveMinimum = null;
        bottlePointerWaitingForWaveMinimum = bottleTarget != null;
    }

    private void HandleSpawnTriggered(SpawnTriggerContext context)
    {
        if (context.Trigger != SpawnTrigger.WaveSpawnTrigger
            || !bottlePointerWaitingForWaveMinimum
            || bottleTarget == null)
        {
            return;
        }

        bottlePointerWaitingForWaveMinimum = false;
        ShowPointer(PointerMode.Bottle);
    }

    private void HandleTrashRemoved(Trash trash)
    {
        if (bottleTarget == null || trash != bottleTarget)
        {
            return;
        }

        if (spawnService != null)
        {
            spawnService.SetSpawnBlocked(TrashType.Bottle, false);
        }

        bottleTarget = null;
        bottlePointerWaitingForWaveMinimum = false;
        HidePointer();
    }

    private void HandleShopItemUnlocked(RewardItemView view)
    {
        if (shopTutorialStarted || view == null)
        {
            return;
        }

        shopTutorialStarted = true;
        pendingShopItemTarget = view;

        if (pendingShopPresentation != null)
        {
            StopCoroutine(pendingShopPresentation);
        }

        pendingShopPresentation = StartCoroutine(ShowShopPointerAfterProgressEvent());
    }

    private IEnumerator ShowShopPointerAfterProgressEvent()
    {
        yield return null;

        while (IsProgressEventBlockingPointer()
            || IsShopItemEntrancePlaying(pendingShopItemTarget))
        {
            yield return null;
        }

        pendingShopPresentation = null;

        if (pendingShopItemTarget == null)
        {
            yield break;
        }

        if (!CanPresentShopPointer())
        {
            Debug.LogError($"TutorialController: Cannot present the shop pointer on '{name}'. Check its Canvas and pointer references.");
            pendingShopItemTarget = null;
            yield break;
        }

        shopItemTarget = pendingShopItemTarget;
        pendingShopItemTarget = null;
        ShowPointer(PointerMode.ShopItem);
    }

    private static bool IsProgressEventBlockingPointer()
    {
        RewardManager rewardManager = RewardManager.Instance;

        if (rewardManager != null && rewardManager.IsProgressEventFlowOpen)
        {
            return true;
        }

        GameManager gameManager = GameManager.Instance;
        return gameManager != null && !gameManager.IsGameplayActive;
    }

    private static bool IsShopItemEntrancePlaying(RewardItemView view)
    {
        return view != null
            && view.TryGetComponent(out LayoutItemSlideIn slideIn)
            && slideIn.IsPlaying;
    }

    private void HandleRewardItemClicked(RewardItemView view)
    {
        if (shopItemTarget == null || view != shopItemTarget)
        {
            return;
        }

        shopItemTarget = null;
        HidePointer();
    }

    private void ShowPointer(PointerMode mode)
    {
        if (pointerAnimation != null)
        {
            StopCoroutine(pointerAnimation);
        }

        pointerMode = mode;
        pointerRequested = true;
        SetPointerObjectActive(true);
        pointerAnimation = StartCoroutine(AnimatePointer());
    }

    private void HidePointer()
    {
        pointerRequested = false;
    }

    private IEnumerator AnimatePointer()
    {
        float elapsed = 0f;
        float safeEntranceDuration = Mathf.Max(0.0001f, entranceDuration);

        while (pointerRequested && elapsed < entranceDuration)
        {
            if (!EnsureCurrentTargetIsValid())
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / safeEntranceDuration);
            float scale = GetEntranceScale(progress);
            Color color = Color.Lerp(flashColor, basePointerColor, Smooth(progress));
            color.a = basePointerColor.a * progress;

            ApplyPointerPresentation(scale, 0f, color);
            yield return null;
        }

        float pulseTime = 0f;

        while (pointerRequested)
        {
            if (!EnsureCurrentTargetIsValid())
            {
                break;
            }

            pulseTime += Time.unscaledDeltaTime;
            float phase = Mathf.Repeat(pulseTime, Mathf.Max(0.01f, pulseDuration))
                / Mathf.Max(0.01f, pulseDuration);
            float wave = (Mathf.Sin((phase * Mathf.PI * 2f) - (Mathf.PI * 0.5f)) + 1f) * 0.5f;
            float eased = Smooth(wave);
            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, eased);
            float movement = movementDistance * eased;

            ApplyPointerPresentation(scale, movement, basePointerColor);
            yield return null;
        }

        Vector3 exitStartScale = pointerRect == null ? basePointerScale : pointerRect.localScale;
        Color exitStartColor = pointerImage == null ? basePointerColor : pointerImage.color;
        elapsed = 0f;
        float safeExitDuration = Mathf.Max(0.0001f, exitDuration);

        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Smooth(Mathf.Clamp01(elapsed / safeExitDuration));

            if (pointerRect != null)
            {
                pointerRect.localScale = Vector3.Lerp(exitStartScale, basePointerScale * entranceStartScale, progress);
            }

            if (pointerImage != null)
            {
                Color color = exitStartColor;
                color.a = Mathf.Lerp(exitStartColor.a, 0f, progress);
                pointerImage.color = color;
            }

            yield return null;
        }

        pointerMode = PointerMode.None;
        pointerAnimation = null;
        ResetPointerVisuals();
        SetPointerObjectActive(false);
    }

    private void ApplyPointerPresentation(float scale, float movement, Color color)
    {
        if (pointerRect == null || pointerImage == null)
        {
            return;
        }

        if (TryGetPointerPlacement(out Vector2 screenPosition, out Vector2 direction))
        {
            RectTransform parentRect = pointerRect.parent as RectTransform;
            Camera canvasCamera = GetCanvasCamera();

            if (parentRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition + (direction * movement),
                    canvasCamera,
                    out Vector2 localPosition))
            {
                pointerRect.anchoredPosition = localPosition;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            pointerRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        pointerRect.localScale = basePointerScale * scale;
        pointerImage.color = color;
        pointerImage.raycastTarget = false;
    }

    private bool TryGetPointerPlacement(out Vector2 screenPosition, out Vector2 direction)
    {
        screenPosition = default;
        direction = Vector2.right;

        switch (pointerMode)
        {
            case PointerMode.Bottle:
                return TryGetBottlePlacement(out screenPosition, out direction);
            case PointerMode.ShopItem:
                return TryGetShopPlacement(out screenPosition, out direction);
            default:
                return false;
        }
    }

    private bool TryGetBottlePlacement(out Vector2 screenPosition, out Vector2 direction)
    {
        screenPosition = default;
        direction = Vector2.right;

        if (bottleTarget == null || playerCharacter == null || worldCamera == null)
        {
            return false;
        }

        Vector3 targetWorldPosition = GetBottleCenter(bottleTarget);
        Vector3 targetScreenPosition = worldCamera.WorldToScreenPoint(targetWorldPosition);
        Vector3 playerScreenPosition = worldCamera.WorldToScreenPoint(playerCharacter.position);

        if (targetScreenPosition.z <= 0f || playerScreenPosition.z <= 0f)
        {
            return false;
        }

        Vector2 playerToTarget = (Vector2)(targetScreenPosition - playerScreenPosition);
        direction = playerToTarget.sqrMagnitude <= Mathf.Epsilon
            ? Vector2.right
            : playerToTarget.normalized;
        screenPosition = (Vector2)targetScreenPosition - (direction * bottleDistance);
        return true;
    }

    private bool TryGetShopPlacement(out Vector2 screenPosition, out Vector2 direction)
    {
        screenPosition = default;
        direction = Vector2.right;

        if (shopItemTarget == null
            || shopItemTarget.transform is not RectTransform targetRect)
        {
            return false;
        }

        targetRect.GetWorldCorners(shopTargetCorners);
        Vector3 targetLeftCenter = (shopTargetCorners[0] + shopTargetCorners[1]) * 0.5f;
        Vector2 targetLeftScreenPosition = RectTransformUtility.WorldToScreenPoint(
            GetCanvasCamera(),
            targetLeftCenter);
        float canvasScale = targetCanvas == null ? 1f : targetCanvas.scaleFactor;
        float pointerHalfWidth = pointerRect.rect.width * canvasScale * pulseMaxScale * 0.5f;
        float gap = shopItemGap * canvasScale;
        screenPosition = targetLeftScreenPosition - (Vector2.right * (pointerHalfWidth + gap));
        return true;
    }

    private static Vector3 GetBottleCenter(Trash trash)
    {
        Renderer targetRenderer = trash.GetComponentInChildren<Renderer>();
        return targetRenderer == null ? trash.transform.position : targetRenderer.bounds.center;
    }

    private bool EnsureCurrentTargetIsValid()
    {
        bool targetIsValid = pointerMode switch
        {
            PointerMode.Bottle => bottleTarget != null && bottleTarget.gameObject.activeInHierarchy,
            PointerMode.ShopItem => shopItemTarget != null && shopItemTarget.gameObject.activeInHierarchy,
            _ => false
        };

        if (targetIsValid)
        {
            return true;
        }

        if (pointerMode == PointerMode.Bottle && spawnService != null)
        {
            spawnService.SetSpawnBlocked(TrashType.Bottle, false);
        }

        bottleTarget = null;
        shopItemTarget = null;
        pointerRequested = false;
        return false;
    }

    private Camera GetCanvasCamera()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return targetCanvas.worldCamera;
    }

    private bool CanPresentBottlePointer()
    {
        return CanPresentPointer()
            && playerCharacter != null
            && worldCamera != null
            && spawnService != null;
    }

    private bool CanPresentShopPointer()
    {
        return CanPresentPointer();
    }

    private bool CanPresentPointer()
    {
        return targetCanvas != null
            && pointerRect != null
            && pointerImage != null
            && pointerRect.parent is RectTransform;
    }

    private void CachePointerDefaults()
    {
        if (pointerRect != null)
        {
            basePointerScale = pointerRect.localScale;
        }

        if (pointerImage != null)
        {
            basePointerColor = pointerImage.color;
            pointerImage.raycastTarget = false;
        }
    }

    private void ResetPointerVisuals()
    {
        if (pointerRect != null)
        {
            pointerRect.localScale = basePointerScale;
        }

        if (pointerImage != null)
        {
            pointerImage.color = basePointerColor;
            pointerImage.raycastTarget = false;
        }
    }

    private void SetPointerObjectActive(bool active)
    {
        if (pointerRect != null && pointerRect.gameObject != gameObject)
        {
            pointerRect.gameObject.SetActive(active);
        }
    }

    private void StopAllPresentationCoroutines()
    {
        if (pointerAnimation != null)
        {
            StopCoroutine(pointerAnimation);
            pointerAnimation = null;
        }

        if (pendingShopPresentation != null)
        {
            StopCoroutine(pendingShopPresentation);
            pendingShopPresentation = null;
        }

        if (armBottlePointerForWaveMinimum != null)
        {
            StopCoroutine(armBottlePointerForWaveMinimum);
            armBottlePointerForWaveMinimum = null;
        }

        bottlePointerWaitingForWaveMinimum = false;

        ResetPointerVisuals();
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - (2f * value));
    }

    private float GetEntranceScale(float progress)
    {
        const float OvershootPeak = 0.65f;

        if (progress < OvershootPeak)
        {
            float rise = Smooth(progress / OvershootPeak);
            return Mathf.Lerp(entranceStartScale, entranceOvershootScale, rise);
        }

        float settle = Smooth((progress - OvershootPeak) / (1f - OvershootPeak));
        return Mathf.Lerp(entranceOvershootScale, pulseMinScale, settle);
    }

    private void OnValidate()
    {
        bottleDistance = Mathf.Max(0f, bottleDistance);
        firstBottleNormalizedSpawnPosition = new Vector3(
            Mathf.Clamp01(firstBottleNormalizedSpawnPosition.x),
            Mathf.Clamp01(firstBottleNormalizedSpawnPosition.y),
            Mathf.Clamp01(firstBottleNormalizedSpawnPosition.z));
        shopItemGap = Mathf.Max(0f, shopItemGap);
        movementDistance = Mathf.Max(0f, movementDistance);
        pulseDuration = Mathf.Max(0.01f, pulseDuration);
        pulseMinScale = Mathf.Max(0f, pulseMinScale);
        pulseMaxScale = Mathf.Max(pulseMinScale, pulseMaxScale);
        entranceDuration = Mathf.Max(0f, entranceDuration);
        exitDuration = Mathf.Max(0f, exitDuration);
        entranceStartScale = Mathf.Max(0f, entranceStartScale);
        entranceOvershootScale = Mathf.Max(pulseMaxScale, entranceOvershootScale);
    }
}
