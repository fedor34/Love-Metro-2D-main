namespace LoveMetro.FieldEffects
{
    public readonly struct FieldEffectRuntimeData
    {
        public FieldEffectRuntimeData(global::IFieldEffect effect, float startTime, int priority)
        {
            Effect = effect;
            StartTime = startTime;
            Priority = priority;
        }

        public global::IFieldEffect Effect { get; }
        public float StartTime { get; }
        public int Priority { get; }
    }
}
