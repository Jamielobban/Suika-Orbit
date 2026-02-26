using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable()
    {
        GameSignals.ScoreChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        GameSignals.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int score)
    {
        if (!scoreText) return;
        scoreText.text = score.ToString();
    }
}
