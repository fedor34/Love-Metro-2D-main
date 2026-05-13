namespace LoveMetro.Scoring
{
    public sealed class ScoreModel
    {
        public ScoreModel(int initialScore = 0)
        {
            CurrentScore = initialScore;
        }

        public int CurrentScore { get; private set; }

        public int Add(int amount)
        {
            int delta = UnityEngine.Mathf.Max(0, amount);
            CurrentScore += delta;
            return delta;
        }

        public int Subtract(int amount)
        {
            int delta = UnityEngine.Mathf.Max(0, amount);
            CurrentScore -= delta;
            return delta;
        }

        public void Reset(int score = 0)
        {
            CurrentScore = score;
        }
    }
}
