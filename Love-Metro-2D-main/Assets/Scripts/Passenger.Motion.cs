using UnityEngine;

public partial class Passenger
{
    private void EnterFallingState(Vector2 initialVelocity)
    {
        EnsureRequiredComponents();
        ConfigureMotionController();
        EnsureStateRuntimeInitialized();
        _stateRuntime.EnterFalling(initialVelocity);
    }

    private void ConfigureMotionController()
    {
        EnsurePhysicsRuntime().ConfigureMotion(CreateMotionConfig(), Settings.bounceElasticity);
    }

    private LoveMetro.Passengers.PassengerMotionConfig CreateMotionConfig()
    {
        PassengerSettings settings = Settings;
        return new LoveMetro.Passengers.PassengerMotionConfig(
            settings.maxFlightSpeed,
            settings.minFallingSpeed,
            settings.magnetRadius,
            settings.magnetForce,
            settings.repelRadius,
            settings.repelForce,
            settings.rematchCooldown);
    }
}
