using System.Collections.Generic;
using UnityEngine;

public partial class Passenger
{
    private readonly List<Passenger> _sameGenderRepelBuffer = new List<Passenger>(8);

    private void EnsureStateMachineInitialized()
    {
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
        if (_currentState != null)
        {
            _currentState.Exit();
            UnsubscribeCurrentStateFromTrainInertia();
        }

        _currentState = newState;

        if (_currentState != null)
        {
            _currentState.Enter();
            SubscribeCurrentStateToTrainInertia();
        }
    }

    private void SubscribeCurrentStateToTrainInertia()
    {
        if (_train == null || _currentState == null)
            return;

        _train.startInertia -= _currentState.OnTrainSpeedChange;
        _train.startInertia += _currentState.OnTrainSpeedChange;
    }

    private void UnsubscribeCurrentStateFromTrainInertia()
    {
        if (_train != null && _currentState != null)
            _train.startInertia -= _currentState.OnTrainSpeedChange;
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

        if (PassengerRegistry.Instance != null)
        {
            PassengerRegistry.Instance.GetSameGenderInRadius(this, _repelRadius, results);
            return;
        }

        results.Clear();
        foreach (Passenger other in Object.FindObjectsOfType<Passenger>())
        {
            if (other == this || other == null || other.IsFemale != IsFemale)
                continue;

            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance <= _repelRadius)
                results.Add(other);
        }
    }

    private static Passenger FindClosestOpposite(Passenger self, float radius)
    {
        if (PassengerRegistry.Instance != null)
            return PassengerRegistry.Instance.FindClosestOpposite(self, radius);

        Passenger best = null;
        float bestDistance = radius;
        foreach (Passenger passenger in Object.FindObjectsOfType<Passenger>())
        {
            if (passenger == self || passenger == null || passenger.IsFemale == self.IsFemale)
                continue;

            float distance = Vector2.Distance(self.transform.position, passenger.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = passenger;
            }
        }

        return best;
    }
}
