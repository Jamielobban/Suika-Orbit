using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameLoopController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Button root object (optional, for showing/hiding).")]
    [SerializeField] private GameObject watchAdButtonObject;

    [Tooltip("The actual Button component for enabling/disabling clicks.")]
    [SerializeField] private Button watchAdButton;

    [Tooltip("Text on the Watch Ad / Continue button.")]
    [SerializeField] private TMP_Text watchAdLabel;

    [Tooltip("Optional: a 'Loading…' text or spinner shown while ad isn't ready.")]
    [SerializeField] private GameObject adLoadingObject;

    [Tooltip("Optional: 'Ad not available' message (use if you want).")]
    [SerializeField] private GameObject noAdText;

    [Header("Continue Countdown")]
    [SerializeField] private bool useCountdown = true;

    [Tooltip("Seconds before the Continue button can be pressed (UX/urgency).")]
    [SerializeField] private int continueCountdownSeconds = 5;

    [Header("Revive Reward")]
    [SerializeField] private MergePowerup mergePowerup;
    [SerializeField] private int mergesOnContinue = 3;

    [Header("Rules")]
    [SerializeField] private bool allowOneContinuePerRun = true;

    [Header("Pause")]
    [SerializeField] private bool pauseOnGameOver = true;

    [SerializeField] private FruitLauncherBase launcher;

    private bool isGameOver;
    private bool usedContinue;

    private Coroutine adUiRoutine;
    private bool countdownDone;

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
    }

    private void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (noAdText) noAdText.SetActive(false);
        if (adLoadingObject) adLoadingObject.SetActive(false);

        isGameOver = false;
        usedContinue = false;

        Time.timeScale = 1f;
        GameInput.Unlock();

        // Optional: start the ad UI button hidden/disabled
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

        if (gameOverPanel) gameOverPanel.SetActive(true);

        StartAdUiFlow();
    }

    private void StartAdUiFlow()
    {
        if (adUiRoutine != null) StopCoroutine(adUiRoutine);
        adUiRoutine = StartCoroutine(AdUiFlow());
    }

    private IEnumerator AdUiFlow()
    {
        // If player already used continue (and you only allow once), hide the ad option.
        bool canContinue = !allowOneContinuePerRun || !usedContinue;

        if (!canContinue)
        {
            if (watchAdButtonObject) watchAdButtonObject.SetActive(false);
            if (noAdText) noAdText.SetActive(false);
            if (adLoadingObject) adLoadingObject.SetActive(false);
            yield break;
        }

        // Show the button, but disable it until countdown + ad ready
        if (watchAdButtonObject) watchAdButtonObject.SetActive(true);
        if (watchAdButton) watchAdButton.interactable = false;

        countdownDone = !useCountdown;

        // Countdown phase (UX/urgency)
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

        // Now wait for ad readiness (loading phase)
        while (true)
        {
            bool adReady = (AdsManager.I != null) && AdsManager.I.IsRewardedReady();

#if UNITY_EDITOR
            // In editor, treat as ready so you can test flow fast
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
                // Ad not ready yet: show loading / no-ad message
                if (watchAdLabel) watchAdLabel.text = "Loading…";
                if (adLoadingObject) adLoadingObject.SetActive(true);

                // If you prefer "No ad available" instead of "Loading…", use noAdText and disable loading
                if (noAdText) noAdText.SetActive(true);

                if (watchAdButton)
                    watchAdButton.interactable = false;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        adUiRoutine = null;
    }

    // Hook to Restart button
    public void Restart()
    {
        Time.timeScale = 1f;
        GameInput.Unlock();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Hook to Watch Ad / Continue button
    public void WatchAdToContinue()
    {
        if (allowOneContinuePerRun && usedContinue) return;
        if (!countdownDone) return;

        if (AdsManager.I == null)
        {
            Debug.LogWarning("No AdsManager in scene.");
            return;
        }

        if (!AdsManager.I.IsRewardedReady())
        {
            // If player taps while not ready (should be disabled anyway), just refresh UI.
            StartAdUiFlow();
            return;
        }

        if (watchAdButton) watchAdButton.interactable = false;
        if (watchAdLabel) watchAdLabel.text = "Playing ad…";
        if (adLoadingObject) adLoadingObject.SetActive(false);
        if (noAdText) noAdText.SetActive(false);

        AdsManager.I.ShowRewarded(() =>
        {
            usedContinue = true;

            if (pauseOnGameOver)
                Time.timeScale = 1f;

            GameInput.Unlock();
            isGameOver = false;

            if (gameOverPanel) gameOverPanel.SetActive(false);

            // Reward effect
            if (mergePowerup) mergePowerup.TryApplyRandomMerges(mergesOnContinue);
            if (launcher) launcher.Revive();
        });

        // Optional: refresh UI after requesting the ad (it’ll likely be not-ready until reloaded)
        StartAdUiFlow();
    }

    [ContextMenu("Raise Game Over")]
    public void CheckGameOver()
    {
        GameSignals.RaiseGameOver();
    }
}