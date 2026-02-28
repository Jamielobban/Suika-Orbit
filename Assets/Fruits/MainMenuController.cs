using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "OrbitScene";

    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;

    // -----------------------------
    // PLAY
    // -----------------------------
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // -----------------------------
    // SETTINGS
    // -----------------------------
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

    // -----------------------------
    // QUIT
    // -----------------------------
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}