using NUnit.Framework;
using UnityEngine;

public class BackgroundGroupScrollerTests
{
    [Test]
    public void BuildScrollDelta_UsesLinearAndQuadraticBoost()
    {
        Vector3 delta = InvokePrivateStatic<Vector3>(
            typeof(BackgroundGroupScroller),
            "BuildScrollDelta",
            2f,
            3f,
            1f,
            4f,
            Vector2.left,
            0.5f);

        // BuildScrollDelta(speed=2, linear=3, quad=1, extra=4, left, dt=0.5)
        // boost = (linear*speed + quad*speed^2) * extra = (6 + 4) * 4 = 40
        // delta = left * boost * dt = (-1, 0) * 40 * 0.5 = (-20, 0)
        Assert.That(delta.x, Is.EqualTo(-20f).Within(0.0001f));
        Assert.That(delta.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void BuildScrollDelta_ReturnsZero_WhenSpeedOrDirectionIsInvalid()
    {
        Vector3 zeroSpeed = InvokePrivateStatic<Vector3>(
            typeof(BackgroundGroupScroller),
            "BuildScrollDelta",
            0f,
            3f,
            1f,
            4f,
            Vector2.left,
            0.5f);

        Vector3 zeroDirection = InvokePrivateStatic<Vector3>(
            typeof(BackgroundGroupScroller),
            "BuildScrollDelta",
            2f,
            3f,
            1f,
            4f,
            Vector2.zero,
            0.5f);

        Assert.AreEqual(Vector3.zero, zeroSpeed);
        Assert.AreEqual(Vector3.zero, zeroDirection);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method.Invoke(null, args);
    }
}
