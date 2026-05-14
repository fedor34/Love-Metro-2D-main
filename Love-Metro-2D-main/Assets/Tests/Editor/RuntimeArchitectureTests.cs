using System.Collections.Generic;
using System.IO;
using LoveMetro.Core;
using LoveMetro.Input;
using LoveMetro.Pairing;
using LoveMetro.Passengers;
using LoveMetro.Scoring;
using LoveMetro.Train;
using NUnit.Framework;
using UnityEngine;

public class RuntimeArchitectureTests
{
    [SetUp]
    public void Setup()
    {
        RuntimeServices.Instance.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        RuntimeServices.Instance.ResetForTests();
    }

    [Test]
    public void RuntimeServices_ProvidesDefaultPureServices()
    {
        Assert.IsNotNull(RuntimeServices.Instance.PairingService);
        Assert.IsNotNull(RuntimeServices.Instance.ScoreService);
    }

    [Test]
    public void PairingService_EvaluateRejectsInvalidPairs()
    {
        PairingService service = new PairingService();
        Passenger male = CreatePassenger(false, Vector3.zero);
        Passenger female = CreatePassenger(true, new Vector3(10f, 0f, 0f));

        try
        {
            PairingResult result = service.Evaluate(new PairingRequest(male, female, maxDistance: 3f, source: "test"));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(PairingFailureReason.TooFar, result.FailureReason);
        }
        finally
        {
            Object.DestroyImmediate(male.gameObject);
            Object.DestroyImmediate(female.gameObject);
        }
    }

    [Test]
    public void PairingService_EvaluateAcceptsCompatiblePassengers()
    {
        PairingService service = new PairingService();
        Passenger male = CreatePassenger(false, Vector3.zero);
        Passenger female = CreatePassenger(true, new Vector3(1f, 0f, 0f));

        try
        {
            PairingResult result = service.Evaluate(new PairingRequest(male, female, maxDistance: 3f, source: "test"));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(PairingFailureReason.None, result.FailureReason);
        }
        finally
        {
            Object.DestroyImmediate(male.gameObject);
            Object.DestroyImmediate(female.gameObject);
        }
    }

    [Test]
    public void ScoreService_AwardsAndPenalizesThroughTypedChanges()
    {
        ScoreService service = new ScoreService(scoreMultiplier: 1.5f);
        ScoreChange award = service.AwardMatchPoints(Vector3.right, 100);
        ScoreChange penalty = service.ApplyPenalty(20, Vector3.left);

        Assert.AreEqual(150, award.Delta);
        Assert.AreEqual(150, award.ScoreAfter);
        Assert.AreEqual(-20, penalty.Delta);
        Assert.AreEqual(130, service.CurrentScore);
    }

    [Test]
    public void PointerIntent_ResolvedDirectionFallsBackToRight()
    {
        PointerIntent intent = PointerIntent.Empty;

        Assert.AreEqual(Vector2.right, intent.ResolvedDirection);
    }

    [Test]
    public void TrainMotionController_ClampsSpeed()
    {
        TrainMotionController controller = new TrainMotionController(1f, 10f);

        Assert.AreEqual(10f, controller.ApplyAcceleration(9f, 10f, 1f));
        Assert.AreEqual(0f, controller.ApplyAcceleration(1f, -10f, 1f));
    }

    [Test]
    public void PassengerStateMachine_EntersExitsAndForwardsTrainInertiaOnlyToActiveState()
    {
        GameObject trainObject = new GameObject("Train");
        TrainManager train = trainObject.AddComponent<TrainManager>();
        var first = new TestPassengerState();
        var second = new TestPassengerState();
        var stateMachine = new PassengerStateMachine(new PassengerStateContext(null));

        try
        {
            stateMachine.ConfigureTrain(train);
            stateMachine.ChangeState(first);
            train.startInertia?.Invoke(Vector2.right);
            stateMachine.ChangeState(second);
            train.startInertia?.Invoke(Vector2.left);

            Assert.AreEqual(1, first.EnterCount);
            Assert.AreEqual(1, first.ExitCount);
            Assert.AreEqual(1, first.TrainImpulseCount);
            Assert.AreEqual(1, second.EnterCount);
            Assert.AreEqual(0, second.ExitCount);
            Assert.AreEqual(1, second.TrainImpulseCount);
        }
        finally
        {
            Object.DestroyImmediate(trainObject);
        }
    }

    [Test]
    public void PassengerStateFactory_CreatesEveryStateId()
    {
        var context = new PassengerStateContext(null);
        var factory = new PassengerStateFactory(context);

        foreach (PassengerStateId id in System.Enum.GetValues(typeof(PassengerStateId)))
        {
            IPassengerState state = factory.Create(id);

            Assert.IsNotNull(state, $"{id} state was not created.");
            Assert.AreEqual(id, state.Id);
        }
    }

    [Test]
    public void PassengerStateRuntime_CreatesEveryStateIdAndChangesCurrentState()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var runtime = new PassengerStateRuntime(passenger);

        try
        {
            foreach (PassengerStateId id in System.Enum.GetValues(typeof(PassengerStateId)))
            {
                runtime.ChangeState(id);
                Assert.AreEqual(id, runtime.CurrentStateId, $"{id} state was not activated.");
            }
        }
        finally
        {
            runtime.Clear();
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerStateRuntime_ParameterizedTransitionsSetCurrentState()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var runtime = new PassengerStateRuntime(passenger);

        try
        {
            runtime.EnterFalling(Vector2.right);
            Assert.AreEqual(PassengerStateId.Falling, runtime.CurrentStateId);

            runtime.EnterFlying(Vector2.up, 10f);
            runtime.UpdateFlyingWind(Vector2.left, 9f);
            Assert.AreEqual(PassengerStateId.Flying, runtime.CurrentStateId);

            runtime.EnterAbsorption(Vector3.right, 3f);
            Assert.AreEqual(PassengerStateId.BeingAbsorbed, runtime.CurrentStateId);
        }
        finally
        {
            runtime.Clear();
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void MatchingPassengerState_TogglesPhysicsAndMatchability()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var state = new LoveMetro.Passengers.States.MatchingPassengerState(new PassengerStateContext(passenger));

        try
        {
            Rigidbody2D rigidbody = passenger.GetComponent<Rigidbody2D>();
            Collider2D collider = passenger.GetComponent<Collider2D>();

            state.Enter();

            Assert.IsFalse(passenger.IsMatchable);
            Assert.AreEqual(RigidbodyType2D.Static, rigidbody.bodyType);
            Assert.IsFalse(collider.enabled);

            state.Exit();

            Assert.IsTrue(passenger.IsMatchable);
            Assert.AreEqual(RigidbodyType2D.Dynamic, rigidbody.bodyType);
            Assert.IsTrue(collider.enabled);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void StayingOnHandrailPassengerState_ReleasesAttachedHandrail()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        GameObject handrailObject = new GameObject("Handrail");
        HandRailPosition handrail = handrailObject.AddComponent<HandRailPosition>();
        var context = new PassengerStateContext(passenger);
        var state = new LoveMetro.Passengers.States.StayingOnHandrailPassengerState(context);

        try
        {
            state.Enter();
            context.AttachHandrail(handrail);
            Assert.IsTrue(handrail.IsOccupied);

            state.Exit();

            Assert.IsFalse(handrail.IsOccupied);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
            Object.DestroyImmediate(handrailObject);
        }
    }

    [Test]
    public void BeingAbsorbedPassengerState_UsesAbsorptionParametersWithoutDirectRigidbodyAccess()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var state = new LoveMetro.Passengers.States.BeingAbsorbedPassengerState(new PassengerStateContext(passenger));

        try
        {
            state.SetAbsorptionParameters(Vector3.right * 5f, 10f);

            Assert.DoesNotThrow(() =>
            {
                state.Enter();
                state.UpdateState();
                state.Exit();
            });
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerMotionController_ClampsAndReflectsVelocity()
    {
        GameObject gameObject = new GameObject("MotionPassenger");
        Rigidbody2D rigidbody = gameObject.AddComponent<Rigidbody2D>();
        PassengerMotionConfig config = new PassengerMotionConfig(5f, 0.1f, 3f, 1f, 2f, 1f, 0.3f);
        var controller = new PassengerMotionController(rigidbody, config, bounceElasticity: 0.5f);

        try
        {
            Vector2 clamped = controller.ClampFlightVelocity(new Vector2(10f, 0f));
            Vector2 reflected = controller.ReflectVelocity(Vector2.right, Vector2.left, boostMultiplier: 2f);
            Vector2 launched = controller.ScaleLaunchVelocity(Vector2.right, speedMultiplier: 2f, impulseScale: 3f);
            controller.SetVelocity(Vector2.up * 2f);

            Assert.AreEqual(5f, clamped.magnitude, 0.001f);
            Assert.AreEqual(Vector2.left, reflected);
            Assert.AreEqual(5f, launched.magnitude, 0.001f);
            Assert.AreEqual(Vector2.up * 2f, controller.CurrentVelocity);
            Assert.DoesNotThrow(() => controller.AddForce(Vector2.right, ForceMode2D.Impulse));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void PassengerPhysicsRuntime_CreatesRequiredComponentsAndSyncsAnimator()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var runtime = new PassengerPhysicsRuntime(passenger);

        try
        {
            runtime.EnsureRequiredComponents();
            passenger.PassangerAnimator = runtime.Animator;

            Assert.IsNotNull(runtime.Rigidbody);
            Assert.IsNotNull(runtime.Collider);
            Assert.IsNotNull(runtime.Animator);
            Assert.AreSame(runtime.Animator, passenger.PassangerAnimator);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerPhysicsRuntime_ResetsCollisionFilters()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var runtime = new PassengerPhysicsRuntime(passenger);

        try
        {
            runtime.EnsureRequiredComponents();
            runtime.Rigidbody.includeLayers = 0;
            runtime.Rigidbody.excludeLayers = 1;
            runtime.Collider.includeLayers = 0;
            runtime.Collider.excludeLayers = 1;

            runtime.ResetCollisionFilters();

            Assert.AreEqual(Physics2D.AllLayers, runtime.Rigidbody.includeLayers.value);
            Assert.AreEqual(0, runtime.Rigidbody.excludeLayers.value);
            Assert.AreEqual(Physics2D.AllLayers, runtime.Collider.includeLayers.value);
            Assert.AreEqual(0, runtime.Collider.excludeLayers.value);
            Assert.IsFalse(runtime.Collider.isTrigger);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerPhysicsRuntime_PreservesMotionOperations()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        var runtime = new PassengerPhysicsRuntime(passenger);
        PassengerMotionConfig config = new PassengerMotionConfig(5f, 0.1f, 3f, 1f, 2f, 1f, 0.3f);

        try
        {
            runtime.ConfigureMotion(config, bounceElasticity: 0.5f);
            Vector2 clamped = runtime.ClampFlightVelocity(new Vector2(10f, 0f));
            Vector2 reflected = runtime.ReflectVelocity(Vector2.right, Vector2.left, boostMultiplier: 2f);
            Vector2 launched = runtime.ScaleLaunchVelocity(Vector2.right, speedMultiplier: 2f, impulseScale: 3f);
            runtime.SetVelocity(Vector2.up * 2f);

            Assert.AreEqual(5f, clamped.magnitude, 0.001f);
            Assert.AreEqual(Vector2.left, reflected);
            Assert.AreEqual(5f, launched.magnitude, 0.001f);
            Assert.AreEqual(Vector2.up * 2f, runtime.CurrentVelocity);
            Assert.DoesNotThrow(() => runtime.AddForce(Vector2.right, ForceMode2D.Impulse));
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerStateTuning_UsesPassengerRuntimeValues()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);

        try
        {
            PassengerStateTuning tuning = ((IPassengerStateHost)passenger).Tuning;

            Assert.AreEqual(1.0f, tuning.LaunchSensitivity);
            Assert.AreEqual(18f, tuning.MaxFlightSpeed);
            Assert.AreEqual(0.7f, tuning.FlightSpeedMultiplier);
            Assert.AreEqual(1.0f, tuning.WallBounceBoost);
            Assert.AreEqual(3, tuning.MaxBounces);
            Assert.AreEqual(8f, tuning.MinWindStrengthForFlying);
            Assert.AreEqual(new Vector2(1f, 3f), tuning.HandrailStandingTimeInterval);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerInteractionRuntime_UsesRuntimeInputIntentProvider()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        PointerIntent intent = new PointerIntent(
            Vector2.up,
            true,
            false,
            0f,
            0f,
            0f,
            0f,
            Vector2.zero,
            new Vector2(3f, 4f),
            true,
            10f);
        RuntimeServices.Instance.RegisterInputIntentProvider(new TestInputIntentProvider(intent));

        try
        {
            PassengerInteractionRuntime runtime = ((IPassengerInteractionHost)passenger).InteractionRuntime;

            Assert.AreEqual(new Vector2(3f, 4f), runtime.GetImpulseTargetWorld(Vector2.zero));
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerInteractionRuntime_FallsBackToLegacyPointerBridge()
    {
        GameObject inputObject = new GameObject("ClickDirectionManager");
        inputObject.AddComponent<ClickDirectionManager>();
        Object.DestroyImmediate(inputObject);

        Passenger passenger = CreatePassenger(false, Vector3.zero);

        try
        {
            PassengerInteractionRuntime runtime = ((IPassengerInteractionHost)passenger).InteractionRuntime;

            Assert.AreEqual(Vector2.right * 5f, runtime.GetImpulseTargetWorld(Vector2.zero));
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerInteractionRuntime_UsesRegistryForLookups()
    {
        GameObject registryObject = new GameObject("PassengerRegistry");
        PassengerRegistry registry = registryObject.AddComponent<PassengerRegistry>();
        Passenger male = CreatePassenger(false, Vector3.zero);
        Passenger sameGender = CreatePassenger(false, Vector3.right);
        Passenger female = CreatePassenger(true, Vector3.up);

        try
        {
            RuntimeServices.Instance.RegisterPassengerRegistry(registry);
            registry.Register(male);
            registry.Register(sameGender);
            registry.Register(female);

            PassengerInteractionRuntime runtime = ((IPassengerInteractionHost)male).InteractionRuntime;
            var sameGenderResults = new List<Passenger>();

            Assert.AreSame(female, runtime.FindClosestOpposite(5f));
            runtime.CollectSameGenderPassengers(sameGenderResults);
            CollectionAssert.Contains(sameGenderResults, sameGender);
        }
        finally
        {
            Object.DestroyImmediate(male.gameObject);
            Object.DestroyImmediate(sameGender.gameObject);
            Object.DestroyImmediate(female.gameObject);
            Object.DestroyImmediate(registryObject);
        }
    }

    [Test]
    public void PassengerInteractionRuntime_ClearsSameGenderBufferWithoutRegistry()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        PassengerInteractionRuntime runtime = ((IPassengerInteractionHost)passenger).InteractionRuntime;
        var results = new List<Passenger> { passenger };

        try
        {
            runtime.CollectSameGenderPassengers(results);

            Assert.IsEmpty(results);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
        }
    }

    [Test]
    public void PassengerInteractionRuntime_PreservesCollisionFallbackAndPeerBounceHelpers()
    {
        Passenger passenger = CreatePassenger(false, Vector3.zero);
        Passenger other = CreatePassenger(true, Vector3.right);
        PassengerInteractionRuntime runtime = ((IPassengerInteractionHost)passenger).InteractionRuntime;
        PassengerPhysicsRuntime otherPhysics = ((IPassengerInteractionHost)other).PhysicsRuntime;

        try
        {
            otherPhysics.ConfigureMotion(new PassengerMotionConfig(5f, 0.1f, 3f, 1f, 2f, 1f, 0.3f), bounceElasticity: 1f);
            otherPhysics.SetVelocity(Vector2.right);

            Assert.AreEqual(Vector2.left, runtime.GetCollisionNormal(null, Vector2.left));
            Assert.AreEqual(Vector2.right, runtime.GetVelocity(other));
            Assert.AreEqual(1f, runtime.GetWallBounceBoost(other));

            runtime.ApplyReflectedVelocity(other, Vector2.right, Vector2.left, boostMultiplier: 1f);

            Assert.AreEqual(Vector2.left, otherPhysics.CurrentVelocity);
        }
        finally
        {
            Object.DestroyImmediate(passenger.gameObject);
            Object.DestroyImmediate(other.gameObject);
        }
    }

    [Test]
    public void GameInitializer_DoesNotUseRuntimeReflectionConfiguration()
    {
        string path = Path.Combine(Application.dataPath, "Scripts", "Core", "GameInitializer.cs");
        string source = File.ReadAllText(path);

        Assert.IsFalse(source.Contains("System.Reflection"));
        Assert.IsFalse(source.Contains("BindingFlags"));
        Assert.IsFalse(source.Contains("GetField("));
    }

    [Test]
    public void MenuManager_DoesNotSearchSceneObjectsAtRuntime()
    {
        string path = Path.Combine(Application.dataPath, "Scripts", "UI", "MenuManager.cs");
        string source = File.ReadAllText(path);

        Assert.IsFalse(source.Contains("GameObject.Find"));
        Assert.IsFalse(source.Contains("Resources.FindObjectsOfTypeAll"));
    }

    private static Passenger CreatePassenger(bool isFemale, Vector3 position)
    {
        GameObject gameObject = new GameObject("Passenger_" + (isFemale ? "F" : "M"));
        gameObject.transform.position = position;
        // Ensure a concrete Collider2D exists before Passenger is added — Passenger's
        // RequireComponent no longer pins it, and tests capture the collider reference upfront.
        gameObject.AddComponent<CircleCollider2D>();
        Passenger passenger = gameObject.AddComponent<Passenger>();
        passenger.IsFemale = isFemale;
        passenger.IsMatchable = true;
        passenger.IsInCouple = false;
        return passenger;
    }

    private sealed class TestPassengerState : IPassengerState
    {
        public TestPassengerState(PassengerStateId id = PassengerStateId.Wandering)
        {
            Id = id;
        }

        public PassengerStateId Id { get; }

        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public int TrainImpulseCount { get; private set; }

        public void UpdateState()
        {
        }

        public void Exit()
        {
            ExitCount++;
        }

        public void Enter()
        {
            EnterCount++;
        }

        public void OnCollision(Collision2D collision)
        {
        }

        public void OnTriggerEnter(Collider2D collision)
        {
        }

        public void OnTrainSpeedChange(Vector2 force)
        {
            TrainImpulseCount++;
        }
    }

    private sealed class TestInputIntentProvider : IInputIntentProvider
    {
        public TestInputIntentProvider(PointerIntent intent)
        {
            CurrentIntent = intent;
        }

        public PointerIntent CurrentIntent { get; }

        public event System.Action<PointerIntent> IntentChanged;
    }
}
