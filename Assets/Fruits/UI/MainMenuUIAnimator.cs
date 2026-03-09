using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIAnimator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform playButton;
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private CanvasGroup bestScoreGroup;
    [SerializeField] private CanvasGroup rootGroup;

    [Header("Timing")]
    [SerializeField] private float introPopDuration = 0.35f;
    [SerializeField] private float buttonPopDuration = 0.25f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float outroDuration = 0.25f;

    private Tween idleTween;
    private bool transitioning;

    private void Awake()
    {
        ApplyInitialState();
    }

    private void Start()
    {
        //PlayIntro();
    }

    private void OnDisable()
    {
        idleTween?.Kill();
        DOTween.Kill(this);
    }

    private void ApplyInitialState()
    {
        if (title)
            title.localScale = Vector3.zero;

        if (playButton)
            playButton.localScale = Vector3.zero;

        if (settingsButton)
            settingsButton.localScale = Vector3.zero;

        if (bestScoreGroup)
            bestScoreGroup.alpha = 0f;

        if (rootGroup)
            rootGroup.alpha = 1f;
    }

    public void PlayIntro()
    {
        idleTween?.Kill();
        ApplyInitialState();

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (title)
            seq.Append(title.DOScale(1f, introPopDuration).SetEase(Ease.OutBack).SetUpdate(true));

        if (playButton)
            seq.Append(playButton.DOScale(1f, buttonPopDuration).SetEase(Ease.OutBack).SetUpdate(true));

        if (settingsButton)
        {
            seq.Append(settingsButton.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true));
        }

        if (bestScoreGroup)
            seq.Append(bestScoreGroup.DOFade(1f, fadeDuration).SetUpdate(true));

        seq.AppendCallback(StartPlayIdle);
    }

    private void StartPlayIdle()
    {
        if (!playButton) return;

        idleTween?.Kill();
        idleTween = playButton
            .DOScale(1.05f, 0.9f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    public void PlayStartGame(string sceneName)
    {
        if (transitioning) return;

        transitioning = true;
        StartCoroutine(StartGameRoutine(sceneName));
    }

    private IEnumerator StartGameRoutine(string sceneName)
    {
        idleTween?.Kill();

        Sequence outro = DOTween.Sequence().SetUpdate(true);

        if (playButton)
        {
            outro.Append(playButton.DOScale(0.9f, 0.08f).SetUpdate(true));
            outro.Append(playButton.DOScale(1.1f, 0.12f).SetEase(Ease.OutBack).SetUpdate(true));
        }

        if (rootGroup)
            outro.Join(rootGroup.DOFade(0f, outroDuration).SetUpdate(true));

        yield return outro.WaitForCompletion();

        SceneManager.LoadScene(sceneName);
    }
}