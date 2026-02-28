using UnityEngine;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(Rigidbody2D))]
public class FruitImpactFeedback : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private MMF_Player mmfPlayer;

    [Header("Impact Threshold")]
    [Tooltip("Minimum collision speed to trigger sound")]
    [SerializeField] private float minImpactSpeed = 1.2f;

    [Tooltip("Speed that counts as max intensity")]
    [SerializeField] private float maxImpactSpeed = 8f;

    [Header("Spam Protection")]
    [SerializeField] private float cooldown = 0.06f;

    private float _nextAllowedTime;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryPlayImpact(collision);
    }

    private void TryPlayImpact(Collision2D collision)
    {
        if (!mmfPlayer) return;
        if (Time.time < _nextAllowedTime) return;

        // ✅ best general-purpose measure
        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minImpactSpeed) return;

        // Normalize to 0–1 for MMF intensity
        float intensity = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );

        mmfPlayer.PlayFeedbacks(transform.position, intensity);

        _nextAllowedTime = Time.time + cooldown;
    }
}