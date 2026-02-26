using TMPro;
using UnityEngine;

public class BestScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text bestText;

    private void OnEnable()
    {
        GameSignals.BestScoreChanged += OnBestChanged;
    }

    private void OnDisable()
    {
        GameSignals.BestScoreChanged -= OnBestChanged;
    }

    private void OnBestChanged(int best)
    {
        if (!bestText) return;
        bestText.text = best.ToString();
    }
}
