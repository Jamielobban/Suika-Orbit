using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject watchAdButton; // optional: the button object
    [SerializeField] private GameObject noAdText;      // optional: "Ad not available"

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

    private void OnEnable()
    {
        GameSignals.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameSignals.GameOver -= OnGameOver;
    }

    private void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (noAdText) noAdText.SetActive(false);

        isGameOver = false;
        usedContinue = false;

        Time.timeScale = 1f;
        GameInput.Unlock();
    }

    private void OnGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (pauseOnGameOver)
            Time.timeScale = 0f;

        GameInput.Lock();

        if (gameOverPanel) gameOverPanel.SetActive(true);

        RefreshAdUI();
    }

    private void RefreshAdUI()
    {
        if (!watchAdButton && !noAdText) return;

        bool canContinue = !allowOneContinuePerRun || !usedContinue;

        bool adReady = (AdsManager.I != null) && AdsManager.I.IsRewardedReady();

#if UNITY_EDITOR
        adReady = true; // editor instant reward path
#endif

        bool showAd = canContinue && adReady;

        if (watchAdButton) watchAdButton.SetActive(showAd);
        if (noAdText) noAdText.SetActive(canContinue && !adReady);
    }

    // Hook to Restart button
    public void Restart()
    {
        Time.timeScale = 1f;
        GameInput.Unlock();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Hook to Watch Ad button
    public void WatchAdToContinue()
    {
        //if (!isGameOver) return;
        if (allowOneContinuePerRun && usedContinue) return;

        if (AdsManager.I == null)
        {
            Debug.LogWarning("No AdsManager in scene.");
            return;
        }

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

        RefreshAdUI();
    }

    [ContextMenu("Raise Game Over")]
    public void CheckGameOver()
    {
        GameSignals.RaiseGameOver();
    }
}