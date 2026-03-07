using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverUIAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunStats runStats;

    [Tooltip("Main panel that pops in. Usually your blue box/root card.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("Optional dark overlay behind the panel.")]
    [SerializeField] private CanvasGroup overlay;

    [Tooltip("The 'Game Over' title root or text transform.")]
    [SerializeField] private RectTransform title;

    [Tooltip("Small label like 'Final Score'. Optional.")]
    [SerializeField] private RectTransform finalScoreLabel;

    [Tooltip("The big changing score text.")]
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("Optional best score text.")]
    [SerializeField] private TMP_Text bestScoreText;

    [Tooltip("Continue button root.")]
    [SerializeField] private CanvasGroup continueButtonGroup;

    [Tooltip("Retry button root.")]
    [SerializeField] private CanvasGroup retryButtonGroup;

    [Header("Timing")]
    [SerializeField] private float panelPopDuration = 0.35f;
    [SerializeField] private float scoreCountDuration = 0.9f;
    [SerializeField] private float buttonFadeDuration = 0.25f;

    private Sequence sequence;

    private void OnEnable()
    {
        PlayIntro();
    }

    private void OnDisable()
    {
        sequence?.Kill();
        DOTween.Kill(scoreText);
    }

    private void PlayIntro()
    {
        sequence?.Kill();

        int finalScore = runStats != null ? runStats.Score : 0;
        int bestScore = runStats != null ? runStats.BestScore : 0;

        // Initial state
        if (panel != null)
            panel.localScale = Vector3.zero;

        if (overlay != null)
            overlay.alpha = 0f;

        if (title != null)
            title.localScale = Vector3.zero;

        if (finalScoreLabel != null)
            finalScoreLabel.localScale = Vector3.zero;

        if (scoreText != null)
        {
            scoreText.text = "0";
            scoreText.transform.localScale = Vector3.one;
        }

        SetGroupVisible(continueButtonGroup, false);
        SetGroupVisible(retryButtonGroup, false);

        if (bestScoreText != null)
            bestScoreText.text = $"Best {bestScore}";

        sequence = DOTween.Sequence().SetUpdate(true);

        // Optional overlay fade
        if (overlay != null)
            sequence.Append(overlay.DOFade(1f, 0.15f));

        // Panel pop
        if (panel != null)
            sequence.Append(panel.DOScale(1f, panelPopDuration).SetEase(Ease.OutBack));

        // Title + label pop
        if (title != null)
            sequence.Append(title.DOScale(1f, 0.2f).SetEase(Ease.OutBack));

        if (finalScoreLabel != null)
            sequence.Append(finalScoreLabel.DOScale(1f, 0.18f).SetEase(Ease.OutBack));

        // Score count
        sequence.AppendCallback(() => AnimateScore(finalScore));

        sequence.AppendInterval(scoreCountDuration);

        // Little punch when score lands
        if (scoreText != null)
        {
            sequence.Append(
                scoreText.transform.DOPunchScale(Vector3.one * 0.18f, 0.22f, 8, 0.8f)
                    .SetUpdate(true)
            );
        }

        // Buttons fade in
        if (continueButtonGroup != null)
        {
            sequence.AppendCallback(() => SetGroupVisible(continueButtonGroup, true));
            sequence.Append(continueButtonGroup.DOFade(1f, buttonFadeDuration).SetUpdate(true));
        }

        if (retryButtonGroup != null)
        {
            sequence.AppendCallback(() => SetGroupVisible(retryButtonGroup, true));
            sequence.Append(retryButtonGroup.DOFade(1f, buttonFadeDuration).SetUpdate(true));
        }
    }

    private void AnimateScore(int finalScore)
    {
        if (scoreText == null) return;

        int displayedValue = 0;

        DOTween.To(
                () => displayedValue,
                x =>
                {
                    displayedValue = x;
                    scoreText.text = x.ToString();
                },
                finalScore,
                scoreCountDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(scoreText);
    }

    private void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}