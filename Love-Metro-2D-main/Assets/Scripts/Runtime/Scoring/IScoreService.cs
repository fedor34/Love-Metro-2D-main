using System;
using UnityEngine;

namespace LoveMetro.Scoring
{
    public interface IScoreService
    {
        int CurrentScore { get; }
        int BasePointsPerCouple { get; }
        event Action<ScoreChange> ScoreChanged;
        ScoreChange AwardMatchPoints(Vector3 worldPosition, int basePoints);
        ScoreChange ApplyPenalty(int amount, Vector3 worldPosition);
        void Reset(int score = 0);
    }
}
