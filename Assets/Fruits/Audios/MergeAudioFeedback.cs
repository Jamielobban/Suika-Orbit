using UnityEngine;
using MoreMountains.Feedbacks;

public class MergeAudioFeedback : MonoBehaviour
{
    [Header("Feedback Player")]
    [SerializeField] private MMF_Player mmfPlayer;

    [Header("Optional scaling by level")]
    [SerializeField] private bool scaleByLevel = false;
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField] private float intensityPerLevel = 0.05f;
    [SerializeField] private float maxIntensity = 2f;

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
        if (!mmfPlayer) return;

        float intensity = 1f;

        if (scaleByLevel)
        {
            intensity = Mathf.Min(
                maxIntensity,
                baseIntensity + newLevel * intensityPerLevel
            );
        }

        // Play at merge position (useful for spatial audio / particles)
        mmfPlayer.PlayFeedbacks(new Vector3(pos.x, pos.y, 0f), intensity);
    }
}