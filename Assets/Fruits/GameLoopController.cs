using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameLoopController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Optional animator on the Game Over panel.")]
    [SerializeField] private GameOverUIAnimator gameOverAnimator;

    [Tooltip("Button root object (optional, for showing/hiding).")]
    [SerializeField] private GameObject watchAdButtonObject;

    [Tooltip("The actual Button component for enabling/disabling clicks.")]
    [SerializeField] private Button watchAdButton;

    [Tooltip("Text on the Watch Ad / Continue button.")]
    [SerializeField] private TMP_Text watchAdLabel;

    [Tooltip("Optional: a 'Loading…' text or spinner shown while ad isn't ready.")]
    [SerializeField] private GameObject adLoadingObject;

    [Tooltip("Optional: 'Ad not available' message.")]
    [SerializeField] private GameObject noAdText;

    [Header("Continue Countdown")]
    [SerializeField] private bool useCountdown = true;

    [Tooltip("Seconds before the Continue button can be pressed.")]
    [SerializeField] private int continueCountdownSeconds = 5;

    [Header("Revive Reward")]
    [SerializeField] private MergePowerup mergePowerup;
    [SerializeField] private int mergesOnContinue = 3;

    [Header("Rules")]
    [SerializeField] private bool allowOneContinuePerRun = true;

    [Header("Pause")]
    [SerializeField] private bool pauseOnGameOver = true;

    [Header("Gameplay")]
    [SerializeField] private FruitLauncherBase launcher;

    private bool isGameOver;
    private bool usedContinue;
    private bool countdownDone;

    private Coroutine adUiRoutine;
    private Coroutine transitionRoutine;

    private void OnEnable()
    {
        GameSignals.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameSignals.GameOver -= OnGameOver;

        if (adUiRoutine != null)
        {
            StopCoroutine(adUiRoutine);
            adUiRoutine = null;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }

    private void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (noAdText) noAdText.SetActive(false);
        if (adLoadingObject) adLoadingObject.SetActive(false);

        isGameOver = false;
        usedContinue = false;
        countdownDone = false;

        Time.timeScale = 1f;
        GameInput.Unlock();

        if (watchAdButtonObject) watchAdButtonObject.SetActive(true);
        if (watchAdButton) watchAdButton.interactable = false;
        if (watchAdLabel) watchAdLabel.text = "Continue?";
    }

    private void OnGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (pauseOnGameOver)
            Time.timeScale = 0f;

        GameInput.Lock();

        GameSignals.RaiseGameplayMusicDuckRequested();

        if (gameOverPanel)
            gameOverPanel.SetActive(true);

        StartAdUiFlow();
    }

    private void StartAdUiFlow()
    {
        if (adUiRoutine != null)
            StopCoroutine(adUiRoutine);

        adUiRoutine = StartCoroutine(AdUiFlow());
    }

    private IEnumerator AdUiFlow()
    {
        bool canContinue = !allowOneContinuePerRun || !usedContinue;

        if (!canContinue)
        {
            if (watchAdButtonObject) watchAdButtonObject.SetActive(false);
            if (watchAdButton) watchAdButton.interactable = false;
            if (noAdText) noAdText.SetActive(false);
            if (adLoadingObject) adLoadingObject.SetActive(false);
            adUiRoutine = null;
            yield break;
        }

        if (watchAdButtonObject) watchAdButtonObject.SetActive(true);
        if (watchAdButton) watchAdButton.interactable = false;

        countdownDone = !useCountdown;

        if (useCountdown && watchAdLabel)
        {
            for (int t = Mathf.Max(1, continueCountdownSeconds); t > 0; t--)
            {
                watchAdLabel.text = $"Continue? {t}";

                if (adLoadingObject) adLoadingObject.SetActive(false);
                if (noAdText) noAdText.SetActive(false);

                yield return new WaitForSecondsRealtime(1f);
            }

            countdownDone = true;
        }

        while (true)
        {
            bool adReady = (AdsManager.I != null) && AdsManager.I.IsRewardedReady();

#if UNITY_EDITOR
            adReady = true;
#endif

            if (adReady)
            {
                if (watchAdLabel) watchAdLabel.text = "Continue";
                if (adLoadingObject) adLoadingObject.SetActive(false);
                if (noAdText) noAdText.SetActive(false);

                if (watchAdButton)
                    watchAdButton.interactable = countdownDone;

                break;
            }
            else
            {
                if (watchAdLabel) watchAdLabel.text = "Loading…";
                if (adLoadingObject) adLoadingObject.SetActive(true);
                if (noAdText) noAdText.SetActive(true);

                if (watchAdButton)
                    watchAdButton.interactable = false;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        adUiRoutine = null;
    }

    public void Restart()
    {
        if (transitionRoutine != null)
            return;

        transitionRoutine = StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        if (watchAdButton)
            watchAdButton.interactable = false;

        GameSignals.RaiseRetryStarted();
        GameSignals.RaiseScoreCountupFinished();
        GameSignals.RaiseGameplayMusicRestoreRequested();

        if (gameOverAnimator != null && gameOverPanel != null && gameOverPanel.activeSelf)
            yield return gameOverAnimator.PlayOutroAndDisable();
        else if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
        GameInput.Unlock();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void WatchAdToContinue()
    {
        if (transitionRoutine != null) return;
        if (allowOneContinuePerRun && usedContinue) return;
        if (!countdownDone) return;

        if (AdsManager.I == null)
        {
            Debug.LogWarning("No AdsManager in scene.");
            return;
        }

        if (!AdsManager.I.IsRewardedReady())
        {
            StartAdUiFlow();
            return;
        }

        if (watchAdButton) watchAdButton.interactable = false;
        if (watchAdLabel) watchAdLabel.text = "Playing ad…";
        if (adLoadingObject) adLoadingObject.SetActive(false);
        if (noAdText) noAdText.SetActive(false);

        AdsManager.I.ShowRewarded(() =>
        {
            if (transitionRoutine != null)
                return;

            transitionRoutine = StartCoroutine(ContinueAfterOutro());
        });

        StartAdUiFlow();
    }

    private IEnumerator ContinueAfterOutro()
    {
        usedContinue = true;

        if (adUiRoutine != null)
        {
            StopCoroutine(adUiRoutine);
            adUiRoutine = null;
        }

        GameSignals.RaiseScoreCountupFinished();

        if (gameOverAnimator != null && gameOverPanel != null && gameOverPanel.activeSelf)
            yield return gameOverAnimator.PlayOutroAndDisable();
        else if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        GameInput.Lock();
        isGameOver = true;
        countdownDone = false;

        GameSignals.RaiseContinueStarted();

        if (mergePowerup)
            yield return mergePowerup.PlayRewardMergeSequence(mergesOnContinue);

        if (pauseOnGameOver)
            Time.timeScale = 1f;

        GameInput.Unlock();
        isGameOver = false;

        if (launcher)
            launcher.Revive();

        GameSignals.RaiseGameplayMusicRestoreRequested();
        GameSignals.RaiseContinueFinished();

        transitionRoutine = null;
    }

    [ContextMenu("Raise Game Over")]
    public void RaiseGameOver()
    {
        GameSignals.RaiseGameOver();
    }
}