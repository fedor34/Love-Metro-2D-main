using NUnit.Framework;
using UnityEngine;

public class PassengerStateMachineTests
{
    private GameObject _passengerObject;
    private Passenger _passenger;

    [SetUp]
    public void Setup()
    {
        _passengerObject = new GameObject("PassengerStateTest");
        _passenger = _passengerObject.AddComponent<Passenger>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_passengerObject != null)
            Object.DestroyImmediate(_passengerObject);
    }

    [Test]
    public void Launch_InitializesStateMachine_AndEntersFalling()
    {
        _passenger.Launch(new Vector2(2f, 0f));

        Assert.AreEqual("Falling", _passenger.GetCurrentStateName());
        Assert.IsNotNull(_passenger.GetRigidbody());
    }

    [Test]
    public void ForceToMatchingState_WithNullPartner_InitializesMatchingState()
    {
        _passenger.ForceToMatchingState(null);

        Assert.AreEqual("Matching", _passenger.GetCurrentStateName());
    }
}
