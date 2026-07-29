#if UNITY_EDITOR
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

public class InputRaycastCameraResolverTests
{
    [Test]
    public void ResolveFallback_UsesLiveCinemachineCameraInsteadOfCameraMain()
    {
        GameObject brainObject = new GameObject("Brain");
        GameObject liveCameraObject = new GameObject("Live Stage Camera");
        GameObject otherCameraObject = new GameObject("Other Stage Camera");

        try
        {
            brainObject.AddComponent<CinemachineBrain>();
            Camera liveCamera = liveCameraObject.AddComponent<Camera>();
            CinemachineCamera liveCinemachineCamera = liveCameraObject.AddComponent<CinemachineCamera>();
            otherCameraObject.tag = "MainCamera";
            otherCameraObject.AddComponent<Camera>();
            otherCameraObject.AddComponent<CinemachineCamera>();
            CinemachineCore.SoloCamera = liveCinemachineCamera;

            Camera resolved = InputRaycastCameraResolver.ResolveFallback(null);

            Assert.That(resolved, Is.SameAs(liveCamera));
        }
        finally
        {
            CinemachineCore.SoloCamera = null;
            Object.DestroyImmediate(brainObject);
            Object.DestroyImmediate(liveCameraObject);
            Object.DestroyImmediate(otherCameraObject);
        }
    }
}
#endif
