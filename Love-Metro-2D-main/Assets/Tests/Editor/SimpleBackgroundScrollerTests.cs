using NUnit.Framework;
using UnityEngine;

public class SimpleBackgroundScrollerTests
{
    [Test]
    public void ComputeScrollDelta_UsesHorizontalDirectionAndSpeed()
    {
        float delta = InvokePrivateStatic<float>(
            typeof(SimpleBackgroundScroller),
            "ComputeScrollDelta",
            10f,
            new Vector2(3f, 4f),
            2f,
            0.5f);

        Assert.That(delta, Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    public void ComputeScrollDelta_ReturnsZero_WhenDirectionOrTimeIsZero()
    {
        float zeroDirection = InvokePrivateStatic<float>(
            typeof(SimpleBackgroundScroller),
            "ComputeScrollDelta",
            10f,
            Vector2.zero,
            2f,
            0.5f);

        float zeroDeltaTime = InvokePrivateStatic<float>(
            typeof(SimpleBackgroundScroller),
            "ComputeScrollDelta",
            10f,
            Vector2.left,
            2f,
            0f);

        Assert.AreEqual(0f, zeroDirection);
        Assert.AreEqual(0f, zeroDeltaTime);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method.Invoke(null, args);
    }
}
