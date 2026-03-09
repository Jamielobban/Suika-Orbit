using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "OrbitScene";

    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Animation")]
    [SerializeField] private MainMenuUIAnimator menuAnimator;

    private void Start()
    {
        if (settingsPanel)
            settingsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        if (menuAnimator != null)
            menuAnimator.PlayStartGame(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}