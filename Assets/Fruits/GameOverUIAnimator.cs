using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

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

    [Header("Timings")]
    [SerializeField] private float introDuration = 0.35f;
    [SerializeField] private float outroDuration = 0.2f;
    [SerializeField] private float scoreDuration = 0.8f;

    private Sequence currentSequence;

    private void OnEnable()
    {
        PlayIntro();
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
        DOTween.Kill(scoreText);
    }

    public void PlayIntro()
    {
        currentSequence?.Kill();

        int finalScore = runStats != null ? runStats.Score : 0;

        if (overlay != null) overlay.alpha = 0f;
        if (panel != null) panel.localScale = Vector3.zero;
        if (title != null) title.localScale = Vector3.zero;

        SetGroupState(continueButtonGroup, 0f, false);
        SetGroupState(retryButtonGroup, 0f, false);

        if (scoreText != null) scoreText.text = "0";

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

        UISfx.I?.PlayPopup();
    }

    public IEnumerator PlayOutroAndDisable()
    {
        currentSequence?.Kill();
        DOTween.Kill(scoreText);

        Sequence outro = DOTween.Sequence().SetUpdate(true);

        if (continueButtonGroup != null)
            outro.Join(continueButtonGroup.DOFade(0f, outroDuration).SetUpdate(true));

        if (retryButtonGroup != null)
            outro.Join(retryButtonGroup.DOFade(0f, outroDuration).SetUpdate(true));

        if (title != null)
            outro.Join(title.DOScale(0.9f, outroDuration).SetUpdate(true));

        if (panel != null)
            outro.Join(panel.DOScale(0.85f, outroDuration).SetEase(Ease.InBack).SetUpdate(true));

        if (overlay != null)
            outro.Join(overlay.DOFade(0f, outroDuration).SetUpdate(true));

        yield return outro.WaitForCompletion();

        gameObject.SetActive(false);
    }

    private void AnimateScore(int finalScore)
    {
        if (scoreText == null) return;

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
    }

    private void SetGroupState(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }
}