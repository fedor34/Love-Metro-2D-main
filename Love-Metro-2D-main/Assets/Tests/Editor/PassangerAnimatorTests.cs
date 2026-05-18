using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PassangerAnimatorTests
{
    private GameObject _passengerObject;
    private Animator _animator;
    private PassangerAnimator _passangerAnimator;

    [SetUp]
    public void Setup()
    {
        _passengerObject = new GameObject("PassangerAnimatorTest");
        _animator = _passengerObject.AddComponent<Animator>();
        _animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Male 1/Male1.controller");
        _passengerObject.AddComponent<SpriteRenderer>();
        _passengerObject.AddComponent<Rigidbody2D>();
        _passangerAnimator = _passengerObject.AddComponent<PassangerAnimator>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_passengerObject != null)
            Object.DestroyImmediate(_passengerObject);
    }

    [Test]
    public void AutomaticWalking_EnablesWalkOnSmallObservedMovement()
    {
        Assert.IsNotNull(_animator.runtimeAnimatorController);

        _passangerAnimator.EnableAutomaticWalkingAnimation();
        InvokePrivate(_passangerAnimator, "UpdateAutomaticWalking", 0.03f);

        Assert.IsTrue(_animator.GetBool("IsWalking"));
    }

    [Test]
    public void CalculateTransformSpeed_TracksNonRigidbodyMovement()
    {
        SetPrivateField(_passangerAnimator, "_hasObservedPosition", true);
        SetPrivateField(_passangerAnimator, "_lastObservedPosition", Vector3.zero);
        Vector3 currentPosition = new Vector3(0.05f, 0f, 0f);

        float speed = InvokePrivate<float>(_passangerAnimator, "CalculateTransformSpeed", currentPosition, 0.5f);

        Assert.AreEqual(0.1f, speed, 0.0001f);
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        instance.GetType()
            .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(instance, args);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        return (T)instance.GetType()
            .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(instance, args);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        instance.GetType()
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(instance, value);
    }
}
