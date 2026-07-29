using Unity.Cinemachine;
using UnityEngine;

public static class InputRaycastCameraResolver
{
    public static bool TryRaycast(
        Camera preferredCamera,
        Vector2 screenPosition,
        float maxDistance,
        LayerMask layerMask,
        out RaycastHit hit)
    {
        return TryRaycast(preferredCamera, screenPosition, maxDistance, layerMask, 0f, out hit);
    }

    public static bool TryRaycast(
        Camera preferredCamera,
        Vector2 screenPosition,
        float maxDistance,
        LayerMask layerMask,
        float sphereCastRadius,
        out RaycastHit hit)
    {
        Camera camera = ResolveFallback(preferredCamera);

        if (TryRaycastWithCamera(camera, screenPosition, maxDistance, layerMask, out hit))
        {
            return true;
        }

        if (TrySphereCastWithCamera(camera, screenPosition, maxDistance, layerMask, sphereCastRadius, out hit))
        {
            return true;
        }

        hit = default;
        return false;
    }

    public static Camera ResolveFallback(Camera preferredCamera)
    {
        if (IsUsable(preferredCamera))
        {
            return preferredCamera;
        }

        Camera cinemachineCamera = ResolveCinemachineCamera();
        if (IsUsable(cinemachineCamera))
        {
            return cinemachineCamera;
        }

        return Camera.main;
    }

    private static Camera ResolveCinemachineCamera()
    {
        for (int i = 0; i < CinemachineBrain.ActiveBrainCount; i++)
        {
            CinemachineBrain brain = CinemachineBrain.GetActiveBrain(i);

            if (brain == null || !brain.isActiveAndEnabled)
            {
                continue;
            }

            Camera outputCamera = brain.OutputCamera;
            if (IsUsable(outputCamera))
            {
                return outputCamera;
            }

            if (brain.ActiveVirtualCamera is Component activeCameraComponent
                && activeCameraComponent.TryGetComponent(out Camera stageCamera)
                && IsUsable(stageCamera))
            {
                return stageCamera;
            }
        }

        return null;
    }

    private static bool TryRaycastWithCamera(
        Camera camera,
        Vector2 screenPosition,
        float maxDistance,
        LayerMask layerMask,
        out RaycastHit hit)
    {
        if (!IsUsable(camera) || !camera.pixelRect.Contains(screenPosition))
        {
            hit = default;
            return false;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hit, maxDistance, layerMask);
    }

    private static bool TrySphereCastWithCamera(
        Camera camera,
        Vector2 screenPosition,
        float maxDistance,
        LayerMask layerMask,
        float sphereCastRadius,
        out RaycastHit hit)
    {
        if (sphereCastRadius <= 0f || !IsUsable(camera) || !camera.pixelRect.Contains(screenPosition))
        {
            hit = default;
            return false;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        return Physics.SphereCast(ray, sphereCastRadius, out hit, maxDistance, layerMask);
    }

    private static bool IsUsable(Camera camera)
    {
        return camera != null && camera.isActiveAndEnabled;
    }

}
