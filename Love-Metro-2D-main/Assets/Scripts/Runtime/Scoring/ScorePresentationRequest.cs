using UnityEngine;

namespace LoveMetro.Scoring
{
    public readonly struct ScorePresentationRequest
    {
        public ScorePresentationRequest(ScoreChange change, Color color, bool waitForEndOfFrame = false)
        {
            Change = change;
            Color = color;
            WaitForEndOfFrame = waitForEndOfFrame;
        }

        public ScoreChange Change { get; }
        public Color Color { get; }
        public bool WaitForEndOfFrame { get; }
    }
}
