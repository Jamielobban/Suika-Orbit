using UnityEngine;

public class RunStats : MonoBehaviour
{
    private const string BestKey = "BEST_SCORE";

    public int Score { get; private set; }
    public int BestScore { get; private set; }

    private void Awake()
    {
        BestScore = PlayerPrefs.GetInt(BestKey, 0);
        GameSignals.RaiseBestScoreChanged(BestScore);
    }

    private void OnEnable()
    {
        GameSignals.ScoreChanged += OnScoreChanged;
        GameSignals.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameSignals.ScoreChanged -= OnScoreChanged;
        GameSignals.GameOver -= OnGameOver;
    }

    private void OnScoreChanged(int score)
    {
        Score = score;
    }

    private void OnGameOver()
    {
        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestKey, BestScore);
            PlayerPrefs.Save();
            GameSignals.RaiseBestScoreChanged(BestScore);
        }
    }

    // Call on restart if you want UI to reset instantly
    public void ResetRun()
    {
        Score = 0;
        GameSignals.RaiseScoreChanged(0);
    }
}
