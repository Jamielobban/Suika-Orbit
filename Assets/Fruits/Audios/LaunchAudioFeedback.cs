using UnityEngine;
using MoreMountains.Feedbacks;
public class LaunchAudioFeedback : MonoBehaviour
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
        GameSignals.FruitDropped += OnFruitDropped;
    }

    private void OnDisable()
    {
        GameSignals.FruitDropped -= OnFruitDropped;
    }

    private void OnFruitDropped(Fruit fruit)
    {
        if (!mmfPlayer || fruit == null) return;

        float intensity = 1f;

        if (scaleByLevel)
        {
            intensity = Mathf.Min(
                maxIntensity,
                baseIntensity + fruit.level * intensityPerLevel
            );
        }

        Vector3 pos = fruit.transform.position;
        mmfPlayer.PlayFeedbacks(pos, intensity);
    }
}
