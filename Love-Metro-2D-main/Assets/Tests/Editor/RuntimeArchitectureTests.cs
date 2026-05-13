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

            Assert.AreEqual(5f, clamped.magnitude, 0.001f);
            Assert.AreEqual(Vector2.left, reflected);
            Assert.AreEqual(5f, launched.magnitude, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
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
        Passenger passenger = gameObject.AddComponent<Passenger>();
        passenger.IsFemale = isFemale;
        passenger.IsMatchable = true;
        passenger.IsInCouple = false;
        return passenger;
    }

    private sealed class TestPassengerState : IPassengerState
    {
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
}
