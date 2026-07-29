using UnityEngine;
using UnityEngine.UI;

public class TrashPickupProgressView : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Camera worldCamera;

    private Transform anchor;
    private Vector3 anchorLocalPosition;
    private Quaternion anchorLocalRotation;
    private Vector3 anchorLocalScale;
    private bool isDetached;

    public void Show()
    {
        EnsureAnchor();
        DetachFromScaledTrash();
        SetProgress(0f);
        gameObject.SetActive(true);
        AlignToCamera();
    }

    public void SetProgress(float progress01)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(progress01);
        }
    }

    public void Hide()
    {
        SetProgress(0f);
        gameObject.SetActive(false);
        ReattachToTrash();
    }

    private void LateUpdate()
    {
        AlignToCamera();
    }

    private void AlignToCamera()
    {
        if (anchor != null && isDetached)
        {
            transform.position = anchor.TransformPoint(anchorLocalPosition);
        }

        Camera camera = InputRaycastCameraResolver.ResolveFallback(worldCamera);

        if (camera == null)
        {
            return;
        }

        transform.rotation = CameraFacingRotationUtility.GetScreenAlignedBillboardRotation(
            camera.transform);
    }

    private void EnsureAnchor()
    {
        if (anchor != null)
        {
            return;
        }

        anchor = transform.parent;

        if (anchor == null)
        {
            return;
        }

        anchorLocalPosition = transform.localPosition;
        anchorLocalRotation = transform.localRotation;
        anchorLocalScale = transform.localScale;
    }

    private void DetachFromScaledTrash()
    {
        if (anchor == null || isDetached)
        {
            return;
        }

        transform.SetParent(null, true);
        isDetached = true;
    }

    private void ReattachToTrash()
    {
        if (!isDetached || anchor == null)
        {
            return;
        }

        transform.SetParent(anchor, false);
        transform.localPosition = anchorLocalPosition;
        transform.localRotation = anchorLocalRotation;
        transform.localScale = anchorLocalScale;
        isDetached = false;
    }
}
