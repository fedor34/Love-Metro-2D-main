using UnityEngine;

namespace LoveMetro.Scoring
{
    public enum ScoreChangeKind
    {
        MatchAward,
        Penalty,
        Reset
    }

    public readonly struct ScoreChange
    {
        public ScoreChange(ScoreChangeKind kind, int delta, int scoreAfter, Vector3 worldPosition)
        {
            Kind = kind;
            Delta = delta;
            ScoreAfter = scoreAfter;
            WorldPosition = worldPosition;
        }

        public ScoreChangeKind Kind { get; }
        public int Delta { get; }
        public int ScoreAfter { get; }
        public Vector3 WorldPosition { get; }
    }
}
