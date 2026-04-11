using NUnit.Framework;
using UnityEngine;

public class ParallaxRendererClassifierTests
{
    private GameObject _rendererObject;

    [TearDown]
    public void TearDown()
    {
        if (_rendererObject != null)
            Object.DestroyImmediate(_rendererObject);
    }

    [Test]
    public void LooksLikeParallaxMaterial_DetectsShaderOrMaterialName()
    {
        Assert.IsTrue(ParallaxRendererClassifier.LooksLikeParallaxMaterial("city_parallax_mat", "sprites/default"));
        Assert.IsTrue(ParallaxRendererClassifier.LooksLikeParallaxMaterial("city", "custom/parallax"));
        Assert.IsFalse(ParallaxRendererClassifier.LooksLikeParallaxMaterial("city", "sprites/default"));
    }

    [Test]
    public void MatchesAny_IgnoresCaseAndEmptyKeys()
    {
        bool result = ParallaxRendererClassifier.MatchesAny(
            "BackgroundCityLayer",
            new[] { "", null, "city" });

        Assert.IsTrue(result);
    }

    [Test]
    public void IsLikelyBackgroundRenderer_UsesNameHints()
    {
        _rendererObject = new GameObject("City_Background");
        SpriteRenderer renderer = _rendererObject.AddComponent<SpriteRenderer>();

        bool result = ParallaxRendererClassifier.IsLikelyBackgroundRenderer(
            renderer,
            new[] { "background" },
            new[] { "\u0433\u043e\u0440\u043e\u0434" },
            new[] { "back" });

        Assert.IsTrue(result);
    }
}
