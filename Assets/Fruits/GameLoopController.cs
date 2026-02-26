using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopController : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] private GameObject gameOverPanel;

    private bool isGameOver;

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
        isGameOver = false;
        Time.timeScale = 1f;
    }

    private void OnGameOver()
    {
        isGameOver = true;
        if (gameOverPanel) gameOverPanel.SetActive(true);

        Debug.Log("Game over");
        Restart();

        // Optional: pause physics when game over
        // Time.timeScale = 0f;
    }

    // Hook this to a UI button
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Editor convenience
    private void Update()
    {
        //if (!isGameOver) return;
        //if (Input.GetKeyDown(KeyCode.R)) Restart();



        // so point and click over the "shoot" area and then i take in to account everythihng
    }
}
