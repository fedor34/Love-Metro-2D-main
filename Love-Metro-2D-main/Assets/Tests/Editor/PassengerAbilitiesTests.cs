using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for the passenger ability pipeline.
/// </summary>
public class PassengerAbilitiesTests
{
    private GameObject _passengerObject;
    private Passenger _passenger;
    private PassengerAbilities _abilities;

    [SetUp]
    public void Setup()
    {
        _passengerObject = new GameObject("TestPassenger");
        _passenger = _passengerObject.AddComponent<Passenger>();
        _abilities = _passengerObject.AddComponent<PassengerAbilities>();
    }

    [TearDown]
    public void Teardown()
    {
        if (_passengerObject != null)
            Object.DestroyImmediate(_passengerObject);
    }

    [Test]
    public void AddAbility_AddsAbilityToList()
    {
        var vipAbility = ScriptableObject.CreateInstance<VipAbility>();

        _abilities.AddAbility(vipAbility);

        Assert.IsTrue(_abilities.HasAbility<VipAbility>());
        Object.DestroyImmediate(vipAbility);
    }

    [Test]
    public void AddAbility_IgnoresNullAbility()
    {
        _abilities.AddAbility(null);

        Assert.IsFalse(_abilities.HasAbility<VipAbility>());
    }

    [Test]
    public void InvokeMatched_AddsConfiguredVipBonus_WhenPartnerAlsoHasVip()
    {
        var selfVip = ScriptableObject.CreateInstance<VipAbility>();
        selfVip.pairBonus = 150;
        _abilities.AddAbility(selfVip);

        var partnerObject = new GameObject("VipPartner");
        var partner = partnerObject.AddComponent<Passenger>();
        var partnerAbilities = partnerObject.AddComponent<PassengerAbilities>();
        var partnerVip = ScriptableObject.CreateInstance<VipAbility>();
        partnerAbilities.AddAbility(partnerVip);

        int points = 100;
        _abilities.InvokeMatched(partner, ref points);

        Assert.AreEqual(250, points);

        Object.DestroyImmediate(selfVip);
        Object.DestroyImmediate(partnerVip);
        Object.DestroyImmediate(partnerObject);
    }

    [Test]
    public void InvokeMatched_DoesNotChangePoints_WhenPartnerHasNoVip()
    {
        var selfVip = ScriptableObject.CreateInstance<VipAbility>();
        selfVip.pairBonus = 150;
        _abilities.AddAbility(selfVip);

        var partner = CreateMockPassenger();
        int points = 100;
        _abilities.InvokeMatched(partner, ref points);

        Assert.AreEqual(100, points);

        Object.DestroyImmediate(selfVip);
        CleanupPassenger(partner);
    }

    [Test]
    public void InvokeMatched_MultipleVipAbilities_StackTheirBonuses()
    {
        var vipAbility1 = ScriptableObject.CreateInstance<VipAbility>();
        var vipAbility2 = ScriptableObject.CreateInstance<VipAbility>();
        vipAbility1.pairBonus = 100;
        vipAbility2.pairBonus = 250;
        _abilities.AddAbility(vipAbility1);
        _abilities.AddAbility(vipAbility2);

        var partnerObject = new GameObject("VipPartner");
        var partner = partnerObject.AddComponent<Passenger>();
        var partnerAbilities = partnerObject.AddComponent<PassengerAbilities>();
        var partnerVip = ScriptableObject.CreateInstance<VipAbility>();
        partnerAbilities.AddAbility(partnerVip);

        int points = 100;
        _abilities.InvokeMatched(partner, ref points);

        Assert.AreEqual(450, points);

        Object.DestroyImmediate(vipAbility1);
        Object.DestroyImmediate(vipAbility2);
        Object.DestroyImmediate(partnerVip);
        Object.DestroyImmediate(partnerObject);
    }

    [Test]
    public void AttachAll_ForwardsOwnerToAbilities()
    {
        var trackingAbility = ScriptableObject.CreateInstance<TrackingAbility>();
        _abilities.AddAbility(trackingAbility);

        _abilities.AttachAll();

        Assert.AreEqual(1, trackingAbility.AttachCalls);
        Assert.AreSame(_passenger, trackingAbility.LastOwner);

        Object.DestroyImmediate(trackingAbility);
    }

    [Test]
    public void InvokePairBroken_ForwardsHitterToAbilities()
    {
        var trackingAbility = ScriptableObject.CreateInstance<TrackingAbility>();
        _abilities.AddAbility(trackingAbility);
        var hitter = CreateMockPassenger();

        _abilities.InvokePairBroken(hitter);

        Assert.AreEqual(1, trackingAbility.PairBrokenCalls);
        Assert.AreSame(_passenger, trackingAbility.LastOwner);
        Assert.AreSame(hitter, trackingAbility.LastHitter);

        Object.DestroyImmediate(trackingAbility);
        CleanupPassenger(hitter);
    }

    private static Passenger CreateMockPassenger()
    {
        var go = new GameObject("MockPartner");
        return go.AddComponent<Passenger>();
    }

    private static void CleanupPassenger(Passenger passenger)
    {
        if (passenger != null && passenger.gameObject != null)
            Object.DestroyImmediate(passenger.gameObject);
    }

    private class TrackingAbility : PassengerAbility
    {
        public int AttachCalls { get; private set; }
        public int PairBrokenCalls { get; private set; }
        public Passenger LastOwner { get; private set; }
        public Passenger LastHitter { get; private set; }

        public override void OnAttach(Passenger self)
        {
            AttachCalls++;
            LastOwner = self;
        }

        public override void OnPairBroken(Passenger self, Passenger hitter)
        {
            PairBrokenCalls++;
            LastOwner = self;
            LastHitter = hitter;
        }
    }
}
