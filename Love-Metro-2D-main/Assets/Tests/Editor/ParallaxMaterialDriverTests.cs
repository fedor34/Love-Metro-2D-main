using NUnit.Framework;
using UnityEngine;

public class ParallaxMaterialDriverTests
{
    [Test]
    public void CalculateNormalizedTrainSpeed_ReturnsZero_WhenPointerIsNotHeld()
    {
        float speed = InvokePrivateStatic<float>(
            typeof(ParallaxMaterialDriver),
            "CalculateNormalizedTrainSpeed",
            240f,
            480f,
            0.8f,
            false);

        Assert.AreEqual(0f, speed);
    }

    [Test]
    public void CalculateNormalizedTrainSpeed_ClampsAndScalesHeldSpeed()
    {
        float speed = InvokePrivateStatic<float>(
            typeof(ParallaxMaterialDriver),
            "CalculateNormalizedTrainSpeed",
            960f,
            480f,
            0.8f,
            true);

        Assert.AreEqual(0.8f, speed);
    }

    [Test]
    public void AdvanceParallaxTime_IgnoresNegativeScaleAndDeltaTime()
    {
        float time = InvokePrivateStatic<float>(
            typeof(ParallaxMaterialDriver),
            "AdvanceParallaxTime",
            1.5f,
            -3f,
            -2f);

        Assert.AreEqual(1.5f, time);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method.Invoke(null, args);
    }
}
