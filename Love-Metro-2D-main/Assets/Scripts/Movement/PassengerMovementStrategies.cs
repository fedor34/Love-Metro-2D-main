using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Movement strategies for passengers to make motion more controllable.
/// Strategies return desired velocity based on current and natural velocities.
/// </summary>
public interface IPassengerMovementStrategy
{
    Vector2 ComputeDesiredVelocity(Passenger passenger,
                                   Vector2 naturalVelocity,
                                   Vector2 currentVelocity,
                                   float deltaTime);
}

/// <summary>
/// Legacy behavior: go exactly with natural velocity.
/// </summary>
public class LegacyMovementStrategy : IPassengerMovementStrategy
{
    public Vector2 ComputeDesiredVelocity(Passenger passenger, Vector2 naturalVelocity, Vector2 currentVelocity, float deltaTime)
    {
        return naturalVelocity;
    }
}

/// <summary>
/// Smooth steering with acceleration, drag and speed clamp.
/// Adds subtle wall avoidance to reduce sticking and jitter.
/// </summary>
public class SteeringSmoothStrategy : IPassengerMovementStrategy
{
    private const float MaxSpeedMultiplier = 1.1f;     // relative to natural speed magnitude
    private const float MaxAcceleration = 8f;          // units per second^2
    private const float Drag = 3.5f;                   // velocity damping
    private const float AvoidDistance = 0.75f;         // wall probe distance
    private const float AvoidStrength = 4f;            // how strongly we steer away

    public Vector2 ComputeDesiredVelocity(Passenger passenger, Vector2 naturalVelocity, Vector2 currentVelocity, float deltaTime)
    {
        // Desired by intent
        Vector2 desired = naturalVelocity;

        // Wall avoidance via short raycasts in facing direction
        Vector2 forward = passenger.transform.right * Mathf.Sign(Vector2.Dot(naturalVelocity, Vector2.right));
        if (forward.sqrMagnitude < 0.0001f) forward = Vector2.right;
        RaycastHit2D hit = Physics2D.Raycast(passenger.transform.position, forward, AvoidDistance, ~0);
        if (hit.collider != null && hit.collider.attachedRigidbody != passenger.GetRigidbody())
        {
            Vector2 away = (Vector2)hit.normal * AvoidStrength;
            desired += away;
        }

        // Acceleration towards desired with drag
        Vector2 acceleration = (desired - currentVelocity) * 1.0f;
        float maxAcc = MaxAcceleration;
        if (acceleration.magnitude > maxAcc)
            acceleration = acceleration.normalized * maxAcc;

        Vector2 newVelocity = currentVelocity + acceleration * deltaTime;
        newVelocity -= newVelocity * Mathf.Clamp01(Drag * deltaTime);

        // Clamp max speed
        float maxSpeed = naturalVelocity.magnitude * MaxSpeedMultiplier;
        if (maxSpeed > 0.01f && newVelocity.magnitude > maxSpeed)
            newVelocity = newVelocity.normalized * maxSpeed;

        return newVelocity;
    }
}

/// <summary>
/// Lane-based motion: prefer moving along X while gently snapping Y to nearest lane.
/// Lanes are spaced by LaneSpacing world units.
/// </summary>
public class LaneBasedStrategy : IPassengerMovementStrategy
{
    private readonly float _laneSpacing;
    private readonly float _laneSnapStrength;

    public LaneBasedStrategy(float laneSpacing = 1.0f, float laneSnapStrength = 3.0f)
    {
        _laneSpacing = Mathf.Max(0.25f, laneSpacing);
        _laneSnapStrength = Mathf.Max(0.5f, laneSnapStrength);
    }

    public Vector2 ComputeDesiredVelocity(Passenger passenger, Vector2 naturalVelocity, Vector2 currentVelocity, float deltaTime)
    {
        Vector2 pos = passenger.transform.position;
        float targetLaneY = Mathf.Round(pos.y / _laneSpacing) * _laneSpacing;
        float yError = targetLaneY - pos.y;

        // Move primarily along intended X; correct Y smoothly toward lane
        Vector2 desired = new Vector2(
            naturalVelocity.x,
            Mathf.Lerp(currentVelocity.y, yError * _laneSnapStrength, Mathf.Clamp01(deltaTime * _laneSnapStrength))
        );

        // Ease X speed toward natural
        desired.x = Mathf.Lerp(currentVelocity.x, naturalVelocity.x, Mathf.Clamp01(deltaTime * 4f));

        // Clamp magnitude to keep it controllable
        float maxSpeed = Mathf.Max(0.01f, naturalVelocity.magnitude);
        if (desired.magnitude > maxSpeed)
            desired = desired.normalized * maxSpeed;

        return desired;
    }
}

/// <summary>
/// Simple boids-like: separation from neighbors, slight alignment and goal-forward bias.
/// </summary>
public class BoidsLiteStrategy : IPassengerMovementStrategy
{
    private const float NeighborRadius = 2.0f;
    private const float SeparationRadius = 1.0f;
    private const float SeparationStrength = 2.5f;
    private const float AlignmentStrength = 0.35f;
    private const float ForwardBias = 0.8f;
    private const float CenteringForce = 1.5f; // Сила возврата к центру Y

    public Vector2 ComputeDesiredVelocity(Passenger passenger, Vector2 naturalVelocity, Vector2 currentVelocity, float deltaTime)
    {
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        int alignmentCount = 0;

        // Возврат к центру Y (y=0 в локальных координатах или мира, если вагон центрирован)
        // Предполагаем что центр вагона по Y ~ 0. Если нет, нужно передавать центр.
        float yOffset = passenger.transform.position.y;
        Vector2 centering = new Vector2(0, -yOffset * CenteringForce);

        foreach (var other in Object.FindObjectsOfType<Passenger>())
        {
            if (other == passenger) continue;
            Vector2 toOther = (Vector2)(other.transform.position - passenger.transform.position);
            float dist = toOther.magnitude;
            if (dist < 0.001f || dist > NeighborRadius) continue;

            // Separation
            if (dist < SeparationRadius)
            {
                separation -= toOther.normalized * (SeparationRadius - dist);
            }

            // Alignment
            var rb = other.GetRigidbody();
            if (rb != null)
            {
                alignment += rb.velocity;
                alignmentCount++;
            }
        }

        if (alignmentCount > 0)
            alignment /= alignmentCount;

        Vector2 desired = naturalVelocity * ForwardBias
                          + separation * SeparationStrength
                          + alignment * AlignmentStrength
                          + centering; // Добавляем центрирующую силу

        // Smooth towards desired to reduce jitter
        desired = Vector2.Lerp(currentVelocity, desired, Mathf.Clamp01(deltaTime * 5f));

        // Clamp speed to natural magnitude
        float maxSpeed = Mathf.Max(0.01f, naturalVelocity.magnitude);
        if (desired.magnitude > maxSpeed)
            desired = desired.normalized * maxSpeed;

        return desired;
    }
}

