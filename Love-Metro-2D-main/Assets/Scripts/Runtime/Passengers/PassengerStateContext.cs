namespace LoveMetro.Passengers
{
    public readonly struct PassengerStateContext
    {
        public PassengerStateContext(global::Passenger passenger)
        {
            Passenger = passenger;
        }

        public global::Passenger Passenger { get; }
    }
}
