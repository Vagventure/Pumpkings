using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite grabSprite;
    [SerializeField] private Vector2 screenOffset;

    [Header("World Raycast")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private LayerMask trashLayerMask;
    [SerializeField, Min(0.01f)] private float raycastDistance = 100f;
    [SerializeField, Min(0f)] private float sphereCastRadius = 0.25f;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private PointerEventData pointerEventData;
    private EventSystem cachedEventSystem;

    private void Awake()
    {
        if (trashLayerMask.value == 0)
        {
            trashLayerMask = LayerMask.GetMask("Trash");
        }

        if (cursorImage != null)
        {
            cursorImage.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        if (cursorImage != null)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
            cursorImage.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        Cursor.visible = true;

        if (cursorImage != null)
        {
            cursorImage.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || cursorImage == null)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        cursorImage.rectTransform.position = screenPosition + screenOffset;

        bool useGrabSprite = IsPointerOverClickableUi(screenPosition)
            || IsPointerOverCollectableTrash(screenPosition);

        cursorImage.sprite = useGrabSprite ? grabSprite : normalSprite;
    }

    private bool IsPointerOverClickableUi(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        if (pointerEventData == null || cachedEventSystem != eventSystem)
        {
            cachedEventSystem = eventSystem;
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.position = screenPosition;
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            if (CursorTargetResolver.IsClickableUi(uiRaycastResults[i].gameObject))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPointerOverCollectableTrash(Vector2 screenPosition)
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager != null && !gameManager.IsGameplayActive)
        {
            return false;
        }

        if (!InputRaycastCameraResolver.TryRaycast(
                inputCamera,
                screenPosition,
                raycastDistance,
                trashLayerMask,
                sphereCastRadius,
                out RaycastHit hit))
        {
            return false;
        }

        Trash trash = hit.collider.GetComponentInParent<Trash>();
        return CursorTargetResolver.IsCollectableTrash(trash);
    }

    private void OnValidate()
    {
        raycastDistance = Mathf.Max(0.01f, raycastDistance);
        sphereCastRadius = Mathf.Max(0f, sphereCastRadius);
    }
}
