using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameSignals
{
    // --- Gameplay ---
    public static event Action<Fruit> FruitDropped;

    // pos, newLevel, combo
    public static event Action<Vector2, int, int> FruitMerged;

    public static event Action GameOver;

    // --- Score / UI ---
    public static event Action<int> ScoreChanged;

    // Preview queue (e.g., 4 upcoming fruit levels)
    public static event Action<IReadOnlyList<int>> NextFruitQueueChanged;

    // Hold slot (level in hold slot, -1 if empty)
    public static event Action<int> HoldChanged;

    // --- Raise helpers ---
    public static void RaiseFruitDropped(Fruit fruit)
    {
        FruitDropped?.Invoke(fruit);
    }

    public static void RaiseFruitMerged(Vector2 pos, int newLevel, int combo)
    {
        FruitMerged?.Invoke(pos, newLevel, combo);
    }

    public static void RaiseGameOver()
    {
        GameOver?.Invoke();
    }

    public static void RaiseScoreChanged(int score)
    {
        ScoreChanged?.Invoke(score);
    }

    public static void RaiseNextFruitQueueChanged(IEnumerable<int> queue)
    {
        // Copy so listeners can't mutate the internal queue and so it's safe to enumerate later
        NextFruitQueueChanged?.Invoke(new List<int>(queue));
    }
    public static event System.Action<int> BestScoreChanged;
    public static void RaiseBestScoreChanged(int best) => BestScoreChanged?.Invoke(best);
}
