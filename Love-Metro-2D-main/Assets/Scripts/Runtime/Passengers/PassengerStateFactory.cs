using System;

namespace LoveMetro.Passengers
{
    public sealed class PassengerStateFactory
    {
        private readonly PassengerStateContext _context;

        public PassengerStateFactory(PassengerStateContext context)
        {
            _context = context;
        }

        public IPassengerState Create(PassengerStateId id)
        {
            switch (id)
            {
                case PassengerStateId.Wandering:
                    return new States.WanderingPassengerState(_context);
                case PassengerStateId.Falling:
                    return new States.FallingPassengerState(_context);
                case PassengerStateId.Flying:
                    return new States.FlyingPassengerState(_context);
                case PassengerStateId.Matching:
                case PassengerStateId.StayingOnHandrail:
                case PassengerStateId.BeingAbsorbed:
                    return _context.CreateLegacyState(id)
                        ?? throw new InvalidOperationException($"Legacy passenger state is not configured: {id}");
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }
    }
}
