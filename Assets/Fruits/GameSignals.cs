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
    public static event Action RunStarted;

    public static event Action ContinueStarted;
    public static event Action ContinueFinished;

    public static event Action RetryStarted;

    // Music / audio state hooks
    public static event Action GameplayMusicDuckRequested;
    public static event Action GameplayMusicRestoreRequested;

    // --- Score / UI ---
    public static event Action<int> ScoreChanged;

    // gainedPoints, combo, newTotal
    public static event Action<int, int, int> ScoreFeedbackRequested;

    public static event Action ScoreCountupStarted;
    public static event Action ScoreCountupFinished;

    public static event Action<IReadOnlyList<int>> NextFruitQueueChanged;
    public static event Action<int> HoldChanged;
    public static event Action<int> BestScoreChanged;

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

    public static void RaiseRunStarted()
    {
        RunStarted?.Invoke();
    }

    public static void RaiseContinueStarted()
    {
        ContinueStarted?.Invoke();
    }

    public static void RaiseContinueFinished()
    {
        ContinueFinished?.Invoke();
    }

    public static void RaiseRetryStarted()
    {
        RetryStarted?.Invoke();
    }

    public static void RaiseGameplayMusicDuckRequested()
    {
        GameplayMusicDuckRequested?.Invoke();
    }

    public static void RaiseGameplayMusicRestoreRequested()
    {
        GameplayMusicRestoreRequested?.Invoke();
    }

    public static void RaiseScoreChanged(int score)
    {
        ScoreChanged?.Invoke(score);
    }

    public static void RaiseScoreFeedbackRequested(int gainedPoints, int combo, int newTotal)
    {
        ScoreFeedbackRequested?.Invoke(gainedPoints, combo, newTotal);
    }

    public static void RaiseScoreCountupStarted()
    {
        ScoreCountupStarted?.Invoke();
    }

    public static void RaiseScoreCountupFinished()
    {
        ScoreCountupFinished?.Invoke();
    }

    public static void RaiseNextFruitQueueChanged(IEnumerable<int> queue)
    {
        NextFruitQueueChanged?.Invoke(new List<int>(queue));
    }

    public static void RaiseHoldChanged(int heldLevel)
    {
        HoldChanged?.Invoke(heldLevel);
    }

    public static void RaiseBestScoreChanged(int best)
    {
        BestScoreChanged?.Invoke(best);
    }
}