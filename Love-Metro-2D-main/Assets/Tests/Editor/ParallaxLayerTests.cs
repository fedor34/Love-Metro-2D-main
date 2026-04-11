using NUnit.Framework;
using UnityEngine;

public class ParallaxLayerTests
{
    [Test]
    public void CalculateEffectiveSpeed_UsesGammaAndNameMultiplier()
    {
        float speed = InvokePrivateStatic<float>(
            typeof(ParallaxLayer),
            "CalculateEffectiveSpeed",
            4f,
            2f,
            1.5f,
            2f);

        Assert.AreEqual(48f, speed);
    }

    [Test]
    public void CalculateClampedTransformStep_LimitsToLayerWidthPortion()
    {
        float step = InvokePrivateStatic<float>(
            typeof(ParallaxLayer),
            "CalculateClampedTransformStep",
            100f,
            10f,
            5f,
            1f);

        Assert.AreEqual(4.5f, step);
    }

    [Test]
    public void ResolveNameMultiplier_ReturnsBoostOnlyWhenKeyMatches()
    {
        float boosted = InvokePrivateStatic<float>(
            typeof(ParallaxLayer),
            "ResolveNameMultiplier",
            "city_background_layer",
            new[] { "city" },
            2f);

        float normal = InvokePrivateStatic<float>(
            typeof(ParallaxLayer),
            "ResolveNameMultiplier",
            "foreground_layer",
            new[] { "city" },
            2f);

        Assert.AreEqual(2f, boosted);
        Assert.AreEqual(1f, normal);
    }

    private static T InvokePrivateStatic<T>(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (T)method.Invoke(null, args);
    }
}
