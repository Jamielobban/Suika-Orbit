using TMPro;
using UnityEngine;

public class BestScoreUI : MonoBehaviour
{
    private const string BestKey = "BEST_SCORE";

    [SerializeField] private TMP_Text bestText;

    private void Start()
    {
        int best = PlayerPrefs.GetInt(BestKey, 0);
        if (bestText)
            bestText.text = best.ToString();
    }
}