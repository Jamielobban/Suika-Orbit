using UnityEngine;

public class RunStats : MonoBehaviour
{
    private const string BestKey = "BEST_SCORE";

    public int Score { get; private set; }
    private int bestScore;

    private void Awake()
    {
        bestScore = PlayerPrefs.GetInt(BestKey, 0);
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
        if (Score > bestScore)
        {
            bestScore = Score;

            PlayerPrefs.SetInt(BestKey, bestScore);
            PlayerPrefs.Save();

            GameSignals.RaiseBestScoreChanged(bestScore);
        }
    }

    [ContextMenu("Game Over")]
    private void RaiseGameOver() { GameSignals.RaiseGameOver(); }
}