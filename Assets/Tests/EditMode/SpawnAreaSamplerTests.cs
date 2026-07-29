#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class SpawnAreaSamplerTests
{
    [TestCase(WindDirection.PositiveX, -0.5f, -0.4f)]
    [TestCase(WindDirection.NegativeX, 0.4f, 0.5f)]
    [TestCase(WindDirection.PositiveZ, -0.5f, -0.4f)]
    [TestCase(WindDirection.NegativeZ, 0.4f, 0.5f)]
    public void GetDirectionalEdgePoint_UsesUpwindEdgeBand(
        WindDirection direction,
        float expectedMinimum,
        float expectedMaximum)
    {
        GameObject spawnArea = new GameObject("Spawn Area");

        try
        {
            BoxCollider boxCollider = spawnArea.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;

            Random.InitState(9876);

            for (int i = 0; i < 64; i++)
            {
                Vector3 point = SpawnAreaSampler.GetDirectionalEdgePoint(
                    spawnArea.transform,
                    direction,
                    new Vector2(0f, 0.1f));
                Vector3 localPoint = spawnArea.transform.InverseTransformPoint(point);
                float edgeCoordinate = direction == WindDirection.PositiveX
                    || direction == WindDirection.NegativeX
                    ? localPoint.x
                    : localPoint.z;

                Assert.That(edgeCoordinate, Is.InRange(expectedMinimum, expectedMaximum));
            }
        }
        finally
        {
            Object.DestroyImmediate(spawnArea);
        }
    }

    [Test]
    public void ClampPoint_RestrictsLocalXZAndPreservesLocalY()
    {
        GameObject spawnArea = new GameObject("Spawn Area");

        try
        {
            spawnArea.transform.position = new Vector3(4f, 2f, -3f);
            spawnArea.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            BoxCollider boxCollider = spawnArea.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(1f, 0f, -1f);
            boxCollider.size = new Vector3(4f, 2f, 6f);

            Vector3 worldPoint = spawnArea.transform.TransformPoint(new Vector3(10f, 0.25f, -10f));
            Vector3 clamped = SpawnAreaSampler.ClampPointXZ(spawnArea.transform, worldPoint);
            Vector3 localPoint = spawnArea.transform.InverseTransformPoint(clamped);

            Assert.That(localPoint.x, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(localPoint.y, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(localPoint.z, Is.EqualTo(-4f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(spawnArea);
        }
    }

    [Test]
    public void GetRandomPoint_UsesRotatedBoxColliderSpace()
    {
        GameObject spawnArea = new GameObject("Spawn Area");

        try
        {
            spawnArea.transform.position = new Vector3(3f, 2f, -4f);
            spawnArea.transform.rotation = Quaternion.Euler(0f, -70f, 0f);
            spawnArea.transform.localScale = new Vector3(26.821604f, -3.0871315f, 2.8058794f);

            BoxCollider boxCollider = spawnArea.AddComponent<BoxCollider>();
            boxCollider.enabled = false;
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;

            Random.InitState(12345);

            for (int i = 0; i < 128; i++)
            {
                Vector3 worldPoint = SpawnAreaSampler.GetRandomPoint(spawnArea.transform);
                Vector3 localPoint = spawnArea.transform.InverseTransformPoint(worldPoint);

                Assert.That(localPoint.x, Is.InRange(-0.5f, 0.5f));
                Assert.That(localPoint.y, Is.InRange(-0.5f, 0.5f));
                Assert.That(localPoint.z, Is.InRange(-0.5f, 0.5f));
            }
        }
        finally
        {
            Object.DestroyImmediate(spawnArea);
        }
    }
}
#endif
