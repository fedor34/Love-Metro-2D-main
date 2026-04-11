using NUnit.Framework;
using UnityEngine;

public class SimpleBackgroundScrollerTests
{
    [Test]
    public void BuildScrollDelta_UsesNormalizedDirectionAndSpeed()
    {
        Vector3 delta = InvokePrivateStatic<Vector3>(
            typeof(SimpleBackgroundScroller),
            "BuildScrollDelta",
            10f,
            new Vector2(3f, 4f),
            2f,
            0.5f);

        Assert.That(delta.x, Is.EqualTo(3f).Within(0.0001f));
        Assert.That(delta.y, Is.EqualTo(4f).Within(0.0001f));
    }

    [Test]
    public void BuildScrollDelta_ReturnsZero_WhenDirectionOrTimeIsZero()
    {
        Vector3 zeroDirection = InvokePrivateStatic<Vector3>(
            typeof(SimpleBackgroundScroller),
            "BuildScrollDelta",
            10f,
            Vector2.zero,
            2f,
            0.5f);

        Vector3 zeroDeltaTime = InvokePrivateStatic<Vector3>(
            typeof(SimpleBackgroundScroller),
            "BuildScrollDelta",
            10f,
            Vector2.left,
            2f,
            0f);

        Assert.AreEqual(Vector3.zero, zeroDirection);
        Assert.AreEqual(Vector3.zero, zeroDeltaTime);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method.Invoke(null, args);
    }
}
