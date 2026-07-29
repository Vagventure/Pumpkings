#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class WindGustMathTests
{
    [TestCase(WindDirection.PositiveX, 0, WindDirection.NegativeX)]
    [TestCase(WindDirection.PositiveX, 1, WindDirection.PositiveZ)]
    [TestCase(WindDirection.PositiveX, 2, WindDirection.NegativeZ)]
    public void SelectDifferentDirection_NeverReturnsPrevious(
        WindDirection previous,
        int alternativeIndex,
        WindDirection expected)
    {
        Assert.That(WindGustMath.SelectDifferentDirection(previous, alternativeIndex), Is.EqualTo(expected));
    }

    [TestCase(WindDirection.PositiveX, 1f, 0f, 0f)]
    [TestCase(WindDirection.NegativeX, -1f, 0f, 0f)]
    [TestCase(WindDirection.PositiveZ, 0f, 0f, 1f)]
    [TestCase(WindDirection.NegativeZ, 0f, 0f, -1f)]
    public void GetLocalDirection_UsesLocalXZAxes(
        WindDirection direction,
        float expectedX,
        float expectedY,
        float expectedZ)
    {
        Vector3 actual = WindGustMath.GetLocalDirection(direction);

        Assert.That(actual.x, Is.EqualTo(expectedX));
        Assert.That(actual.y, Is.EqualTo(expectedY));
        Assert.That(actual.z, Is.EqualTo(expectedZ));
    }

    [Test]
    public void GetRemainingMovementFraction_LaterSpawnTravelsLess()
    {
        float existingTrashFraction = WindGustMath.GetRemainingMovementFraction(0f);
        float laterTrashFraction = WindGustMath.GetRemainingMovementFraction(0.75f);

        Assert.That(existingTrashFraction, Is.EqualTo(1f));
        Assert.That(laterTrashFraction, Is.EqualTo(0.15625f).Within(0.0001f));
    }

    [Test]
    public void GetLocalDisplacement_AppliesDistanceAndDeviationInXZPlane()
    {
        Vector3 displacement = WindGustMath.GetLocalDisplacement(
            WindDirection.PositiveX,
            20f,
            0.15f,
            1f,
            0f);

        Assert.That(displacement.x, Is.EqualTo(3f).Within(0.0001f));
        Assert.That(displacement.y, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(displacement.z, Is.EqualTo(0f).Within(0.0001f));
    }

    [TestCase(0.8f, 2.4f)]
    [TestCase(1.2f, 3.6f)]
    public void GetLocalDisplacement_RespectsConfiguredMultiplierRange(
        float multiplier,
        float expectedMagnitude)
    {
        Vector3 displacement = WindGustMath.GetLocalDisplacement(
            WindDirection.PositiveZ,
            20f,
            0.15f,
            multiplier,
            12f);

        Assert.That(displacement.magnitude, Is.EqualTo(expectedMagnitude).Within(0.0001f));
        Assert.That(Vector3.Angle(Vector3.forward, displacement), Is.EqualTo(12f).Within(0.0001f));
    }

    [TestCase(0, 3, 4f, 1f, 0f)]
    [TestCase(1, 3, 4f, 1f, 2.3f)]
    [TestCase(2, 3, 4f, -1f, 4f)]
    public void GetDirectionalSpawnTime_KeepsBurstInsideWindow(
        int spawnIndex,
        int spawnCount,
        float duration,
        float jitterNormalized,
        float expectedTime)
    {
        Assert.That(
            DirectionalBurstMath.GetSpawnTime(
                spawnIndex,
                spawnCount,
                duration,
                jitterNormalized),
            Is.EqualTo(expectedTime).Within(0.0001f));
    }
}
#endif
