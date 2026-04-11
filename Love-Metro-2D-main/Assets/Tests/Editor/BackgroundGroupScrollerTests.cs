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

        Assert.That(delta.x, Is.EqualTo(-10f).Within(0.0001f));
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
