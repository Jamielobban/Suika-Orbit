using UnityEngine;
using TMPro;
using DG.Tweening;

public class StartButtonPulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Scale Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float duration = 0.6f;

    [Header("Animation")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 baseScale;
    private Tween pulseTween;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        baseScale = targetText.transform.localScale;
    }

    void OnEnable()
    {
        PlayPulse();
    }

    void OnDisable()
    {
        pulseTween?.Kill();
    }

    void PlayPulse()
    {
        pulseTween = targetText.transform
            .DOScale(baseScale * scaleMultiplier, duration)
            .SetEase(curve)
            .SetLoops(-1, LoopType.Yoyo);
    }
}