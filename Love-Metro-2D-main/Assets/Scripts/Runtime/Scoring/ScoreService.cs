using System;
using UnityEngine;

namespace LoveMetro.Scoring
{
    public class ScoreService : IScoreService
    {
        private readonly ScoreModel _model;
        private readonly int _basePointsPerCouple;
        private readonly float _scoreMultiplier;

        public ScoreService(int basePointsPerCouple = 0, float scoreMultiplier = 1f, ScoreModel model = null)
        {
            _basePointsPerCouple = Mathf.Max(0, basePointsPerCouple);
            _scoreMultiplier = Mathf.Max(0f, scoreMultiplier);
            _model = model ?? new ScoreModel();
        }

        public int CurrentScore => _model.CurrentScore;
        public int BasePointsPerCouple => _basePointsPerCouple;

        public event Action<ScoreChange> ScoreChanged;

        public ScoreChange AwardMatchPoints(Vector3 worldPosition, int basePoints)
        {
            int scaledPoints = Mathf.Max(0, Mathf.RoundToInt(basePoints * _scoreMultiplier));
            int delta = _model.Add(scaledPoints);
            ScoreChange change = new ScoreChange(ScoreChangeKind.MatchAward, delta, _model.CurrentScore, worldPosition);
            ScoreChanged?.Invoke(change);
            return change;
        }

        public ScoreChange ApplyPenalty(int amount, Vector3 worldPosition)
        {
            int penalty = _model.Subtract(amount);
            ScoreChange change = new ScoreChange(ScoreChangeKind.Penalty, -penalty, _model.CurrentScore, worldPosition);
            ScoreChanged?.Invoke(change);
            return change;
        }

        public void Reset(int score = 0)
        {
            _model.Reset(score);
            ScoreChanged?.Invoke(new ScoreChange(ScoreChangeKind.Reset, 0, _model.CurrentScore, Vector3.zero));
        }
    }
}
