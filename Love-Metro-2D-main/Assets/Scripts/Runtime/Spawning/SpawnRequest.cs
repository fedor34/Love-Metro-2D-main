using System;

namespace LoveMetro.Spawning
{
    public readonly struct SpawnRequest
    {
        public SpawnRequest(
            int availableLocationsCount,
            int currentPassengerCount,
            int maxPassengersInScene,
            int minPassengersPerWave,
            int maxPassengersPerWave)
        {
            AvailableLocationsCount = Math.Max(0, availableLocationsCount);
            CurrentPassengerCount = Math.Max(0, currentPassengerCount);
            MaxPassengersInScene = Math.Max(0, maxPassengersInScene);
            MinPassengersPerWave = Math.Max(0, minPassengersPerWave);
            MaxPassengersPerWave = Math.Max(0, maxPassengersPerWave);
        }

        public int AvailableLocationsCount { get; }
        public int CurrentPassengerCount { get; }
        public int MaxPassengersInScene { get; }
        public int MinPassengersPerWave { get; }
        public int MaxPassengersPerWave { get; }
        public int RemainingSlots => Math.Max(0, MaxPassengersInScene - CurrentPassengerCount);
    }
}
