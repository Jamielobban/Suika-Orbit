using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private float countDuration = 0.22f;
    [SerializeField] private float punchScale = 0.18f;
    [SerializeField] private float punchDuration = 0.18f;

    private int displayedScore;
    private Tween scoreTween;
    private Tween punchTween;

    private void Awake()
    {
        if (scoreText)
            scoreText.text = "0";
    }

    private void OnEnable()
    {
        GameSignals.ScoreChanged += OnScoreChanged;
        GameSignals.RunStarted += OnRunStarted;
    }

    private void OnDisable()
    {
        GameSignals.ScoreChanged -= OnScoreChanged;
        GameSignals.RunStarted -= OnRunStarted;

        scoreTween?.Kill();
        punchTween?.Kill();
    }

    private void OnRunStarted()
    {
        scoreTween?.Kill();
        punchTween?.Kill();

        displayedScore = 0;

        if (scoreText)
        {
            scoreText.text = "0";
            scoreText.transform.localScale = Vector3.one;
        }
    }

    private void OnScoreChanged(int newScore)
    {
        if (!scoreText) return;

        scoreTween?.Kill();
        punchTween?.Kill();

        scoreTween = DOTween.To(
            () => displayedScore,
            x =>
            {
                displayedScore = x;
                scoreText.text = x.ToString();
            },
            newScore,
            countDuration
        ).SetEase(Ease.OutQuad);

        punchTween = scoreText.transform.DOPunchScale(
            Vector3.one * punchScale,
            punchDuration,
            6,
            0.8f
        );
    }
}