using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScorePopupItemUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private CanvasGroup canvasGroup;

    private Sequence currentSequence;
    private System.Action<ScorePopupItemUI> onFinished;

    private void Awake()
    {
        if (!popupRoot)
            popupRoot = transform as RectTransform;

        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        HideInstant();
    }

    public void Play(
        int gainedPoints,
        int combo,
        Vector2 anchoredStartPos,
        float moveUp,
        float duration,
        float startScale,
        float peakScale,
        bool showComboOnlyIfAboveOne,
        System.Action<ScorePopupItemUI> finishedCallback)
    {
        onFinished = finishedCallback;

        currentSequence?.Kill();

        bool showCombo = combo > 1 || !showComboOnlyIfAboveOne;

        if (pointsText)
            pointsText.text = "+" + gainedPoints;

        if (comboText)
        {
            comboText.gameObject.SetActive(showCombo);

            if (showCombo)
                comboText.text = "Combo x" + combo;
        }

        popupRoot.anchoredPosition = anchoredStartPos;
        popupRoot.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);

        currentSequence = DOTween.Sequence();
        currentSequence.SetUpdate(true);

        currentSequence.Append(
            popupRoot.DOScale(peakScale, 0.16f).SetEase(Ease.OutBack)
        );

        currentSequence.Join(
            popupRoot.DOAnchorPosY(anchoredStartPos.y + moveUp, duration).SetEase(Ease.OutQuad)
        );

        currentSequence.Join(
            canvasGroup.DOFade(0f, duration).SetEase(Ease.OutQuad)
        );

        currentSequence.OnComplete(Finish);
    }

    public void StopAndHide()
    {
        currentSequence?.Kill();
        HideInstant();
        gameObject.SetActive(false);
    }

    private void Finish()
    {
        HideInstant();
        gameObject.SetActive(false);
        onFinished?.Invoke(this);
    }

    private void HideInstant()
    {
        if (popupRoot)
        {
            popupRoot.localScale = Vector3.one;
            popupRoot.anchoredPosition = Vector2.zero;
        }

        if (canvasGroup)
            canvasGroup.alpha = 0f;

        if (comboText)
            comboText.gameObject.SetActive(false);
    }
}