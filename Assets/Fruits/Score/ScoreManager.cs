using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private FruitDatabase database;

    [Tooltip("Extra % per combo step. 0.25 = +25% per combo.")]
    [SerializeField] private float comboStepBonus = 0.25f;

    public int Score { get; private set; }

    private void OnEnable()
    {
        GameSignals.FruitMerged += OnFruitMerged;
    }

    private void OnDisable()
    {
        GameSignals.FruitMerged -= OnFruitMerged;
    }

    private void OnFruitMerged(Vector2 pos, int newLevel, int combo)
    {
        if (!database) return;

        int basePts = database.GetBasePoints(newLevel);
        float mult = 1f + comboStepBonus * Mathf.Max(0, combo - 1);

        int gained = Mathf.RoundToInt(basePts * mult);
        Score += gained;

        GameSignals.RaiseScoreChanged(Score);
    }
}
