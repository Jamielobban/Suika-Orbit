using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;
using DG.Tweening;

public class GameAudioController : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private MMF_Player backgroundMusicFeedback;
    [SerializeField] private AudioSource musicSource;

    [Header("Game Over")]
    [SerializeField] private MMF_Player gameOverFeedback;
    [SerializeField] private float musicVolumeDuringGameOver = 0.2f;
    [SerializeField] private float musicFadeDuration = 0.35f;

    [Header("State SFX")]
    [SerializeField] private MMF_Player continueStartedFeedback;
    [SerializeField] private MMF_Player continueFinishedFeedback;
    [SerializeField] private MMF_Player retryFeedback;

    [Header("Score Countup")]
    [SerializeField] private MMF_Player scoreCountupStartFeedback;
    [SerializeField] private MMF_Player scoreCountupLoopFeedback;
    [SerializeField] private MMF_Player scoreCountupFinishedFeedback;

    private float originalMusicVolume;
    private bool scoreLoopPlaying;
    private Tween musicTween;

    private void Awake()
    {
        if (musicSource != null)
            originalMusicVolume = 0.15f;
    }

    private void OnEnable()
    {
        GameSignals.RunStarted += HandleRunStarted;
        GameSignals.GameOver += HandleGameOver;
        GameSignals.ContinueStarted += HandleContinueStarted;
        GameSignals.ContinueFinished += HandleContinueFinished;
        GameSignals.RetryStarted += HandleRetryStarted;
        GameSignals.ScoreCountupStarted += HandleScoreCountupStarted;
        GameSignals.ScoreCountupFinished += HandleScoreCountupFinished;
    }

    private void OnDisable()
    {
        GameSignals.RunStarted -= HandleRunStarted;
        GameSignals.GameOver -= HandleGameOver;
        GameSignals.ContinueStarted -= HandleContinueStarted;
        GameSignals.ContinueFinished -= HandleContinueFinished;
        GameSignals.RetryStarted -= HandleRetryStarted;
        GameSignals.ScoreCountupStarted -= HandleScoreCountupStarted;
        GameSignals.ScoreCountupFinished -= HandleScoreCountupFinished;

        musicTween?.Kill();
    }

    private void HandleRunStarted()
    {
        if (backgroundMusicFeedback != null)
            backgroundMusicFeedback.PlayFeedbacks();
    }

    private void HandleGameOver()
    {
        if (gameOverFeedback != null)
            gameOverFeedback.PlayFeedbacks();

        FadeMusic(musicVolumeDuringGameOver);
    }

    private void HandleContinueStarted()
    {
        if (continueStartedFeedback != null)
            continueStartedFeedback.PlayFeedbacks();
    }

    private void HandleContinueFinished()
    {
        if (continueFinishedFeedback != null)
            continueFinishedFeedback.PlayFeedbacks();

        FadeMusic(originalMusicVolume);
    }

    private void HandleRetryStarted()
    {
        if (retryFeedback != null)
            retryFeedback.PlayFeedbacks();
    }

    private void HandleScoreCountupStarted()
    {
        if (scoreCountupStartFeedback != null)
            scoreCountupStartFeedback.PlayFeedbacks();

        if (!scoreLoopPlaying && scoreCountupLoopFeedback != null)
        {
            scoreCountupLoopFeedback.PlayFeedbacks();
            scoreLoopPlaying = true;
        }
    }

    private void HandleScoreCountupFinished()
    {
        if (scoreLoopPlaying && scoreCountupLoopFeedback != null)
            scoreCountupLoopFeedback.StopFeedbacks();

        if (scoreCountupFinishedFeedback != null)
            scoreCountupFinishedFeedback.PlayFeedbacks();

        scoreLoopPlaying = false;
    }

    private void FadeMusic(float targetVolume)
    {
        if (musicSource == null)
            return;

        musicTween?.Kill();

        musicTween = musicSource
            .DOFade(targetVolume, musicFadeDuration)
            .SetUpdate(true); // important since game is paused
    }
}