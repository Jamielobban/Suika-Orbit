using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class WellExitGameOver : MonoBehaviour
{
    [Header("Lose Rule")]
    [SerializeField] private LayerMask fruitMask;
    [SerializeField] private float timeOutsideToLose = 5f;

    [Tooltip("If true: a fruit only becomes 'eligible' after its CENTER has been inside at least once.")]
    [SerializeField] private bool requireCenterEnteredOnce = true;

    [Tooltip("Use Rigidbody2D.position when available (more accurate under physics).")]
    [SerializeField] private bool useRigidbodyPosition = true;

    [Header("Well Visual (Rim Color)")]
    [SerializeField] private Renderer wellRenderer;
    [SerializeField] private Material sourceMaterial;
    [SerializeField] private Color safeRimColor = new(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color dangerRimColor = new(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float colorLerpSpeed = 10f;

    [Header("Danger Timer UI")]
    [SerializeField] private TMP_Text dangerTimerText;
    [SerializeField] private bool showDecimals = false;
    [SerializeField] private int decimalPlaces = 1;

    [Header("Danger Timer Feel")]
    [SerializeField] private Color timerStartColor = new(1f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color timerEndColor = new(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color timerFlashColor = Color.white;

    [Tooltip("Scale punch on each whole-second tick.")]
    [SerializeField] private float tickPunchScale = 0.22f;

    [Tooltip("Duration of the scale punch.")]
    [SerializeField] private float tickPunchDuration = 0.22f;

    [Tooltip("Position shake strength on each tick.")]
    [SerializeField] private float tickShakePositionStrength = 10f;

    [Tooltip("Rotation shake strength on each tick.")]
    [SerializeField] private float tickShakeRotationStrength = 8f;

    [Tooltip("How jittery the shakes are.")]
    [SerializeField] private int tickShakeVibrato = 18;

    [Tooltip("2 and 1 seconds remaining hit harder.")]
    [SerializeField] private float lowTimeIntensityMultiplier = 1.35f;

    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

    private readonly HashSet<Fruit> tracked = new();
    private readonly HashSet<Fruit> centerEnteredOnce = new();

    private Collider2D well;
    private float deadline = -1f;
    private bool fired;

    private Material runtimeMat;
    private Color currentRim;

    private Vector3 timerBaseScale;
    private Vector2 timerBaseAnchoredPos;
    private int lastDisplayedSecond = -1;

    private Tween timerScaleTween;
    private Tween timerPosTween;
    private Tween timerRotTween;
    private Tween timerColorTween;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void Awake()
    {
        well = GetComponent<Collider2D>();
        well.isTrigger = true;

        if (!wellRenderer)
            wellRenderer = GetComponentInChildren<Renderer>();

        if (sourceMaterial)
        {
            runtimeMat = Instantiate(sourceMaterial);
            if (wellRenderer)
                wellRenderer.material = runtimeMat;
        }
        else if (wellRenderer)
        {
            runtimeMat = Instantiate(wellRenderer.material);
            wellRenderer.material = runtimeMat;
        }

        currentRim = safeRimColor;
        SetRimColor(currentRim, true);

        if (dangerTimerText)
        {
            timerBaseScale = dangerTimerText.rectTransform.localScale;
            timerBaseAnchoredPos = dangerTimerText.rectTransform.anchoredPosition;
            dangerTimerText.color = timerStartColor;
        }

        SetTimerUIVisible(false);
    }

    private void OnDestroy()
    {
        if (runtimeMat)
            Destroy(runtimeMat);

        timerScaleTween?.Kill();
        timerPosTween?.Kill();
        timerRotTween?.Kill();
        timerColorTween?.Kill();
    }

    private void Update()
    {
        if (fired)
            return;

        tracked.RemoveWhere(f => f == null);
        centerEnteredOnce.RemoveWhere(f => f == null);

        bool anyOutside = false;

        foreach (var fruit in tracked)
        {
            Vector2 center = GetFruitCenter(fruit);
            bool centerInside = well.OverlapPoint(center);

            if (requireCenterEnteredOnce && !centerEnteredOnce.Contains(fruit))
            {
                if (centerInside)
                    centerEnteredOnce.Add(fruit);

                continue;
            }

            if (!centerInside)
            {
                anyOutside = true;
                break;
            }
        }

        SetRimColor(anyOutside ? dangerRimColor : safeRimColor);

        if (anyOutside)
        {
            if (deadline < 0f)
            {
                deadline = Time.time + timeOutsideToLose;
                SetTimerUIVisible(true);
                lastDisplayedSecond = -1;
                ResetTimerTransformOnly();
            }

            float remaining = Mathf.Max(0f, deadline - Time.time);

            UpdateTimerUI(remaining);
            UpdateTimerVisuals(remaining);

            if (Time.time >= deadline)
            {
                fired = true;
                SetTimerUIVisible(false);
                ResetTimerVisuals();
                GameSignals.RaiseGameOver();
            }
        }
        else
        {
            deadline = -1f;
            SetTimerUIVisible(false);
            ResetTimerVisuals();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & fruitMask) == 0)
            return;

        var fruit = other.GetComponent<Fruit>();
        if (!fruit)
            return;

        tracked.Add(fruit);
    }

    private Vector2 GetFruitCenter(Fruit fruit)
    {
        if (!fruit)
            return Vector2.zero;

        if (useRigidbodyPosition)
        {
            var rb = fruit.GetComponent<Rigidbody2D>();
            if (rb)
                return rb.position;
        }

        return fruit.transform.position;
    }

    private void SetRimColor(Color target, bool instant = false)
    {
        if (!runtimeMat)
            return;

        if (instant)
        {
            currentRim = target;
        }
        else
        {
            currentRim = Color.Lerp(
                currentRim,
                target,
                1f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime)
            );
        }

        runtimeMat.SetColor(RimColorId, currentRim);
    }

    private void SetTimerUIVisible(bool visible)
    {
        if (!dangerTimerText)
            return;

        dangerTimerText.gameObject.SetActive(visible);
    }

    private void UpdateTimerUI(float remaining)
    {
        if (!dangerTimerText)
            return;

        int displayedSecond = Mathf.CeilToInt(remaining);

        if (!showDecimals)
        {
            dangerTimerText.text = displayedSecond.ToString();
        }
        else
        {
            dangerTimerText.text = remaining.ToString($"F{Mathf.Clamp(decimalPlaces, 0, 3)}");
        }

        if (displayedSecond != lastDisplayedSecond)
        {
            if (displayedSecond > 0)
                PlayTickFeedback(displayedSecond);

            lastDisplayedSecond = displayedSecond;
        }
    }

    private void UpdateTimerVisuals(float remaining)
    {
        if (!dangerTimerText)
            return;

        float t = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.001f, timeOutsideToLose));
        dangerTimerText.color = Color.Lerp(timerStartColor, timerEndColor, t);
    }

    private void PlayTickFeedback(int displayedSecond)
    {
        if (!dangerTimerText)
            return;

        RectTransform rt = dangerTimerText.rectTransform;

        float intensity = displayedSecond <= 2 ? lowTimeIntensityMultiplier : 1f;

        timerScaleTween?.Kill();
        timerPosTween?.Kill();
        timerRotTween?.Kill();
        timerColorTween?.Kill();

        rt.localScale = timerBaseScale;
        rt.anchoredPosition = timerBaseAnchoredPos;
        rt.localRotation = Quaternion.identity;

        Color baseColor = Color.Lerp(
            timerStartColor,
            timerEndColor,
            1f - Mathf.Clamp01((float)displayedSecond / Mathf.Max(1f, timeOutsideToLose))
        );

        dangerTimerText.color = timerFlashColor;

        timerColorTween = dangerTimerText
            .DOColor(baseColor, 0.18f)
            .SetUpdate(true);

        timerScaleTween = rt
            .DOPunchScale(Vector3.one * (tickPunchScale * intensity), tickPunchDuration, 1, 0.2f)
            .SetUpdate(true);

        timerPosTween = rt
            .DOShakeAnchorPos(
                0.20f,
                tickShakePositionStrength * intensity,
                tickShakeVibrato,
                90f,
                false,
                true
            )
            .SetUpdate(true)
            .OnComplete(() => rt.anchoredPosition = timerBaseAnchoredPos);

        timerRotTween = rt
            .DOShakeRotation(
                0.20f,
                new Vector3(0f, 0f, tickShakeRotationStrength * intensity),
                tickShakeVibrato,
                90f,
                true
            )
            .SetUpdate(true)
            .OnComplete(() => rt.localRotation = Quaternion.identity);
    }

    private void ResetTimerTransformOnly()
    {
        timerScaleTween?.Kill();
        timerPosTween?.Kill();
        timerRotTween?.Kill();

        if (!dangerTimerText)
            return;

        dangerTimerText.rectTransform.localScale = timerBaseScale;
        dangerTimerText.rectTransform.anchoredPosition = timerBaseAnchoredPos;
        dangerTimerText.rectTransform.localRotation = Quaternion.identity;
    }

    private void ResetTimerVisuals()
    {
        lastDisplayedSecond = -1;

        timerScaleTween?.Kill();
        timerPosTween?.Kill();
        timerRotTween?.Kill();
        timerColorTween?.Kill();

        if (!dangerTimerText)
            return;

        dangerTimerText.color = timerStartColor;
        dangerTimerText.rectTransform.localScale = timerBaseScale;
        dangerTimerText.rectTransform.anchoredPosition = timerBaseAnchoredPos;
        dangerTimerText.rectTransform.localRotation = Quaternion.identity;
    }
}