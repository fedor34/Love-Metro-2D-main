using System.Collections.Generic;
using LoveMetro.Core;
using LoveMetro.Input;
using UnityEngine;

namespace LoveMetro.Passengers
{
    internal sealed class PassengerInteractionRuntime
    {
        private readonly IPassengerInteractionHost _host;

        public PassengerInteractionRuntime(IPassengerInteractionHost host)
        {
            _host = host ?? throw new System.ArgumentNullException(nameof(host));
        }

        public Vector2 GetVelocity(global::Passenger passenger)
        {
            IPassengerInteractionHost host = ResolveHost(passenger);
            return host != null ? host.PhysicsRuntime.CurrentVelocity : Vector2.zero;
        }

        public void ApplyReflectedVelocity(global::Passenger passenger, Vector2 velocity, Vector2 normal, float boostMultiplier)
        {
            ResolveHost(passenger)?.PhysicsRuntime.ApplyReflectedVelocity(velocity, normal, boostMultiplier);
        }

        public float GetWallBounceBoost(global::Passenger passenger)
        {
            IPassengerInteractionHost host = ResolveHost(passenger);
            return host != null ? host.Tuning.WallBounceBoost : _host.Tuning.WallBounceBoost;
        }

        public Vector2 GetImpulseTargetWorld(Vector2 position)
        {
            PointerIntent intent = ResolvePointerIntent();
            return intent.HasReleasePoint
                ? intent.LastReleaseWorld
                : position + intent.ResolvedDirection * 5f;
        }

        public float GetNormalizedTargetDelta(Vector2 position, Vector2 targetWorld, bool vertical)
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

        public Vector2 GetCollisionNormal(Collision2D collision, Vector2 fallback)
        {
            return collision != null && collision.contacts.Length > 0
                ? collision.contacts[0].normal
                : fallback;
        }

        public bool TryResolvePassengerImpact(global::Passenger other)
        {
            if (other == null)
                return false;

            return _host.MatchRuntime.TryResolvePassengerImpact(other);
        }

        public global::Passenger FindClosestOpposite(float radius)
        {
            return ResolvePassengerRegistry()?.FindClosestOpposite(_host.Passenger, radius);
        }

        public void CollectSameGenderPassengers(List<global::Passenger> results)
        {
            if (results == null)
                return;

            IPassengerRegistry registry = ResolvePassengerRegistry();
            if (registry != null)
            {
                registry.GetSameGenderInRadius(_host.Passenger, _host.Tuning.RepelRadius, results);
                return;
            }

            results.Clear();
        }

        private PointerIntent ResolvePointerIntent()
        {
            IInputIntentProvider provider = _host.Services?.InputIntentProvider;
            return provider != null
                ? provider.CurrentIntent
                : new PointerIntent(
                    ClickDirectionManager.CurrentClickDirection,
                    ClickDirectionManager.HasClickDirection,
                    ClickDirectionManager.IsMouseHeld,
                    ClickDirectionManager.HorizontalAxis,
                    ClickDirectionManager.HorizontalVelocity,
                    ClickDirectionManager.VerticalAxis,
                    ClickDirectionManager.VerticalVelocity,
                    ClickDirectionManager.CurrentPointerWorld,
                    ClickDirectionManager.LastReleaseWorld,
                    ClickDirectionManager.HasReleasePoint,
                    ClickDirectionManager.LastReleaseTime);
        }

        private IPassengerRegistry ResolvePassengerRegistry()
        {
            return _host.Services?.PassengerRegistry ?? PassengerRegistry.Instance;
        }

        private static IPassengerInteractionHost ResolveHost(global::Passenger passenger)
        {
            return passenger as IPassengerInteractionHost;
        }
    }
}
