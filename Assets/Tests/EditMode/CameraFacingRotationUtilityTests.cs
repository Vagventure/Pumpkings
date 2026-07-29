#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class CameraFacingRotationUtilityTests
{
    [TestCase(1f, 0f, 75f)]
    [TestCase(-1f, 0f, -75f)]
    public void GetSemiBillboardRotation_ClampsFacingAroundCamera(
        float desiredX,
        float desiredZ,
        float expectedYaw)
    {
        GameObject cameraObject = new GameObject("Camera");

        try
        {
            cameraObject.transform.position = new Vector3(0f, 5f, 10f);

            Quaternion rotation = CameraFacingRotationUtility.GetSemiBillboardRotation(
                Vector3.zero,
                new Vector3(desiredX, 0f, desiredZ),
                cameraObject.transform,
                75f);

            Assert.That(Mathf.DeltaAngle(0f, rotation.eulerAngles.y), Is.EqualTo(expectedYaw).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void GetScreenAlignedBillboardRotation_MatchesCameraRotation()
    {
        GameObject cameraObject = new GameObject("Camera");

        try
        {
            cameraObject.transform.rotation = Quaternion.Euler(25f, 40f, 3f);

            Quaternion rotation = CameraFacingRotationUtility.GetScreenAlignedBillboardRotation(
                cameraObject.transform);

            Assert.That(Quaternion.Angle(rotation, cameraObject.transform.rotation), Is.LessThan(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
#endif
