using UnityEngine;

public static class CameraFacingRotationUtility
{
    public static Quaternion GetScreenAlignedBillboardRotation(Transform cameraTransform)
    {
        return cameraTransform != null ? cameraTransform.rotation : Quaternion.identity;
    }

    public static Quaternion GetSemiBillboardRotation(
        Vector3 objectPosition,
        Vector3 desiredForward,
        Transform cameraTransform,
        float maxCameraFacingAngle)
    {
        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude < 0.0001f)
        {
            return Quaternion.identity;
        }

        if (cameraTransform == null)
        {
            return Quaternion.LookRotation(desiredForward.normalized);
        }

        Vector3 cameraFacingDirection = cameraTransform.position - objectPosition;
        cameraFacingDirection.y = 0f;

        if (cameraFacingDirection.sqrMagnitude < 0.0001f)
        {
            cameraFacingDirection = -cameraTransform.forward;
            cameraFacingDirection.y = 0f;
        }

        if (cameraFacingDirection.sqrMagnitude < 0.0001f)
        {
            return Quaternion.LookRotation(desiredForward.normalized);
        }

        float cameraFacingYaw = GetYaw(cameraFacingDirection);
        float desiredYaw = GetYaw(desiredForward);
        float maxAngle = Mathf.Clamp(maxCameraFacingAngle, 0f, 180f);
        float clampedOffset = Mathf.Clamp(
            Mathf.DeltaAngle(cameraFacingYaw, desiredYaw),
            -maxAngle,
            maxAngle);

        return Quaternion.Euler(0f, cameraFacingYaw + clampedOffset, 0f);
    }

    private static float GetYaw(Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
}
