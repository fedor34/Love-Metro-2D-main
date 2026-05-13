namespace LoveMetro.Passengers
{
    public enum PassengerLookupKind
    {
        SameGenderInRadius,
        ClosestOppositeInRadius
    }

    public readonly struct PassengerLookupQuery
    {
        public PassengerLookupQuery(global::Passenger passenger, float radius, PassengerLookupKind kind)
        {
            Passenger = passenger;
            Radius = radius;
            Kind = kind;
        }

        public global::Passenger Passenger { get; }
        public float Radius { get; }
        public PassengerLookupKind Kind { get; }
    }
}
