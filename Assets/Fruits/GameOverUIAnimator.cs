using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using MoreMountains.Feedbacks;

public class GameOverUIAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RunStats runStats;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private RectTransform title;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private CanvasGroup continueButtonGroup;
    [SerializeField] private CanvasGroup retryButtonGroup;
    [SerializeField] private CanvasGroup homeButtonGroup;

    [Header("New Best")]
    [SerializeField] private TMP_Text newBestText;
    [SerializeField] private CanvasGroup newBestGroup;
    [SerializeField] private float newBestYoyoScale = 1.08f;
    [SerializeField] private float newBestYoyoDuration = 0.45f;
    [SerializeField] private Color newBestBaseColor = Color.white;
    [SerializeField] private Color newBestPulseColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private float newBestColorPulseDuration = 0.6f;

    [Header("Timings")]
    [SerializeField] private float introDuration = 0.35f;
    [SerializeField] private float outroDuration = 0.2f;
    [SerializeField] private float scoreDuration = 0.8f;

    [SerializeField] private MMF_Player feedbacks;

    private Sequence currentSequence;
    private Tween newBestLoopTween;
    private Tween newBestColorTween;

    private void OnEnable()
    {
        PlayIntro();
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
        newBestLoopTween?.Kill();
        newBestColorTween?.Kill();
        DOTween.Kill(scoreText);

        if (newBestText != null)
        {
            newBestText.transform.localScale = Vector3.one;
            newBestText.color = newBestBaseColor;
        }
    }

    public void PlayIntro()
    {
        currentSequence?.Kill();
        newBestLoopTween?.Kill();
        newBestColorTween?.Kill();

        int finalScore = runStats != null ? runStats.Score : 0;
        bool isNewBest = runStats != null && finalScore > 0 && finalScore >= runStats.BestScore;

        if (overlay != null) overlay.alpha = 0f;
        if (panel != null) panel.localScale = Vector3.zero;
        if (title != null) title.localScale = Vector3.zero;

        SetGroupState(continueButtonGroup, 0f, false);
        SetGroupState(retryButtonGroup, 0f, false);
        SetGroupState(homeButtonGroup, 0f, false);
        SetGroupState(newBestGroup, 0f, false);

        if (newBestText != null)
        {
            newBestText.gameObject.SetActive(isNewBest);
            newBestText.transform.localScale = Vector3.one;
            newBestText.color = newBestBaseColor;
        }

        if (scoreText != null)
            scoreText.text = "0";

        currentSequence = DOTween.Sequence().SetUpdate(true);

        if (overlay != null)
            currentSequence.Append(overlay.DOFade(1f, 0.15f).SetUpdate(true));

        if (panel != null)
            currentSequence.Append(panel.DOScale(1f, introDuration).SetEase(Ease.OutBack).SetUpdate(true));

        if (title != null)
            currentSequence.Append(title.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true));

        currentSequence.AppendCallback(() => AnimateScore(finalScore));
        currentSequence.AppendInterval(scoreDuration);

        if (scoreText != null)
            currentSequence.Append(scoreText.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f).SetUpdate(true));

        if (isNewBest && newBestGroup != null)
        {
            currentSequence.AppendCallback(() => SetGroupState(newBestGroup, 0f, false));
            currentSequence.Append(newBestGroup.DOFade(1f, 0.2f).SetUpdate(true));

            if (newBestText != null)
            {
                currentSequence.Append(newBestText.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f).SetUpdate(true));
                currentSequence.AppendCallback(StartNewBestLoop);
            }
        }

        if (continueButtonGroup != null)
        {
            currentSequence.AppendCallback(() => SetGroupState(continueButtonGroup, 0f, true));
            currentSequence.Append(continueButtonGroup.DOFade(1f, 0.2f).SetUpdate(true));
        }

        if (retryButtonGroup != null)
        {
            currentSequence.AppendCallback(() => SetGroupState(retryButtonGroup, 0f, true));
            currentSequence.Append(retryButtonGroup.DOFade(1f, 0.2f).SetUpdate(true));
        }

        if (homeButtonGroup != null)
        {
            currentSequence.AppendCallback(() => SetGroupState(homeButtonGroup, 0f, true));
            currentSequence.Append(homeButtonGroup.DOFade(1f, 0.2f).SetUpdate(true));
        }

        UISfx.I?.PlayPopup();
    }

    public IEnumerator PlayOutroAndDisable()
    {
        currentSequence?.Kill();
        newBestLoopTween?.Kill();
        newBestColorTween?.Kill();
        DOTween.Kill(scoreText);

        Sequence outro = DOTween.Sequence().SetUpdate(true);

        if (continueButtonGroup != null)
            outro.Join(continueButtonGroup.DOFade(0f, outroDuration).SetUpdate(true));

        if (retryButtonGroup != null)
            outro.Join(retryButtonGroup.DOFade(0f, outroDuration).SetUpdate(true));

        if (homeButtonGroup != null)
            outro.Join(homeButtonGroup.DOFade(0f, outroDuration).SetUpdate(true));

        if (newBestGroup != null)
            outro.Join(newBestGroup.DOFade(0f, outroDuration).SetUpdate(true));

        if (title != null)
            outro.Join(title.DOScale(0.9f, outroDuration).SetUpdate(true));

        if (panel != null)
            outro.Join(panel.DOScale(0.85f, outroDuration).SetEase(Ease.InBack).SetUpdate(true));

        if (overlay != null)
            outro.Join(overlay.DOFade(0f, outroDuration).SetUpdate(true));

        yield return outro.WaitForCompletion();

        if (newBestText != null)
        {
            newBestText.transform.localScale = Vector3.one;
            newBestText.color = newBestBaseColor;
        }

        gameObject.SetActive(false);
    }

    private void AnimateScore(int finalScore)
    {
        if (scoreText == null)
            return;

        int displayed = 0;

        DOTween.To(
            () => displayed,
            x =>
            {
                displayed = x;
                scoreText.text = x.ToString();
            },
            finalScore,
            scoreDuration
        )
        .SetEase(Ease.OutCubic)
        .SetUpdate(true)
        .SetTarget(scoreText);

        feedbacks?.PlayFeedbacks();
    }

    private void StartNewBestLoop()
    {
        if (newBestText == null)
            return;

        newBestLoopTween?.Kill();
        newBestColorTween?.Kill();

        newBestText.transform.localScale = Vector3.one;
        newBestText.color = newBestBaseColor;

        newBestLoopTween = newBestText.transform
            .DOScale(newBestYoyoScale, newBestYoyoDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        newBestColorTween = newBestText
            .DOColor(newBestPulseColor, newBestColorPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void SetGroupState(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }
}