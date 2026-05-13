using System.Collections.Generic;
using UnityEngine;

public partial class Passenger
{
    private readonly List<Passenger> _sameGenderRepelBuffer = new List<Passenger>(8);

    private void EnsureStateMachineInitialized()
    {
        _stateMachine ??= new LoveMetro.Passengers.PassengerStateMachine(
            new LoveMetro.Passengers.PassengerStateContext(this));

        if (wanderingState != null)
            return;

        wanderingState = new Wandering(this);
        stayingOnHandrailState = new StayingOnHandrail(this);
        fallingState = new Falling(this);
        flyingState = new Flying(this);
        matchingState = new Matching(this);
        beingAbsorbedState = new BeingAbsorbed(this);
    }

    private void ChangeState(PassangerState newState)
    {
        EnsureStateMachineInitialized();
        _stateMachine.ConfigureTrain(_train);
        _stateMachine.ChangeState(newState);
        _currentState = _stateMachine.CurrentState;
    }

    private void SubscribeCurrentStateToTrainInertia()
    {
        _stateMachine?.ConfigureTrain(_train);
    }

    private void UnsubscribeCurrentStateFromTrainInertia()
    {
        _stateMachine?.Clear();
        _currentState = null;
    }

    private Vector2 GetImpulseTargetWorld(Vector2 position)
    {
        return ClickDirectionManager.HasReleasePoint
            ? ClickDirectionManager.LastReleaseWorld
            : position + ClickDirectionManager.GetCurrentDirection() * 5f;
    }

    private static float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            float worldDelta = vertical ? targetWorld.y - position.y : targetWorld.x - position.x;
            return Mathf.Clamp(worldDelta, -1f, 1f);
        }

        Vector3 positionScreen = mainCamera.WorldToScreenPoint(position);
        Vector3 targetScreen = mainCamera.WorldToScreenPoint(targetWorld);
        float delta = vertical ? targetScreen.y - positionScreen.y : targetScreen.x - positionScreen.x;
        float divisor = vertical ? Mathf.Max(1f, (float)Screen.height) : Mathf.Max(1f, (float)Screen.width);
        return Mathf.Clamp(delta / divisor, -1f, 1f);
    }

    private static Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback)
    {
        return collision != null && collision.contacts.Length > 0
            ? collision.contacts[0].normal
            : fallback;
    }

    private bool TryResolvePassengerImpact(Passenger other)
    {
        if (other == null)
            return false;

        BreakCoupleOnImpact(other);
        other.BreakCoupleOnImpact(this);
        return TryMatchWith(other);
    }

    private void CollectSameGenderPassengers(List<Passenger> results)
    {
        if (results == null)
            return;

        LoveMetro.Core.IPassengerRegistry registry = ResolvePassengerRegistry();
        if (registry != null)
        {
            registry.GetSameGenderInRadius(this, _repelRadius, results);
            return;
        }

        results.Clear();
    }

    private static Passenger FindClosestOpposite(Passenger self, float radius)
    {
        return ResolvePassengerRegistry()?.FindClosestOpposite(self, radius);
    }

    private static LoveMetro.Core.IPassengerRegistry ResolvePassengerRegistry()
    {
        return LoveMetro.Core.RuntimeServices.Instance.PassengerRegistry ?? PassengerRegistry.Instance;
    }
}
