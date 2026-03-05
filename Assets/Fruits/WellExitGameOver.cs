using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    [SerializeField] private TMP_Text dangerTimerText;   // drag a TextMeshProUGUI here
    [SerializeField] private bool showDecimals = false;  // false = 3,2,1 ; true = 2.7,2.6...
    [SerializeField] private int decimalPlaces = 1;      // only used if showDecimals = true

    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

    private readonly HashSet<Fruit> tracked = new();
    private readonly HashSet<Fruit> centerEnteredOnce = new();

    private Collider2D well;
    private float deadline = -1f;
    private bool fired;

    private Material runtimeMat;
    private Color currentRim;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void Awake()
    {
        well = GetComponent<Collider2D>();
        well.isTrigger = true;

        if (!wellRenderer) wellRenderer = GetComponentInChildren<Renderer>();

        if (sourceMaterial)
        {
            runtimeMat = Instantiate(sourceMaterial);
            if (wellRenderer) wellRenderer.material = runtimeMat;
        }
        else if (wellRenderer)
        {
            runtimeMat = Instantiate(wellRenderer.material);
            wellRenderer.material = runtimeMat;
        }

        currentRim = safeRimColor;
        SetRimColor(currentRim, instant: true);

        SetTimerUIVisible(false);
    }

    private void OnDestroy()
    {
        if (runtimeMat) Destroy(runtimeMat);
    }

    private void Update()
    {
        if (fired) return;

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

        // Rim color
        SetRimColor(anyOutside ? dangerRimColor : safeRimColor);

        // Timer / game over + UI
        if (anyOutside)
        {
            if (deadline < 0f)
            {
                deadline = Time.time + timeOutsideToLose;
                SetTimerUIVisible(true);
            }

            float remaining = Mathf.Max(0f, deadline - Time.time);
            UpdateTimerUI(remaining);

            if (Time.time >= deadline)
            {
                fired = true;
                SetTimerUIVisible(false);
                GameSignals.RaiseGameOver();
            }
        }
        else
        {
            deadline = -1f;
            SetTimerUIVisible(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & fruitMask) == 0) return;

        var fruit = other.GetComponent<Fruit>();
        if (!fruit) return;

        tracked.Add(fruit);
    }

    private Vector2 GetFruitCenter(Fruit fruit)
    {
        if (!fruit) return Vector2.zero;

        if (useRigidbodyPosition)
        {
            var rb = fruit.GetComponent<Rigidbody2D>();
            if (rb) return rb.position;
        }

        return fruit.transform.position;
    }

    private void SetRimColor(Color target, bool instant = false)
    {
        if (!runtimeMat) return;

        if (instant)
            currentRim = target;
        else
            currentRim = Color.Lerp(currentRim, target, 1f - Mathf.Exp(-colorLerpSpeed * Time.deltaTime));

        runtimeMat.SetColor(RimColorId, currentRim);
    }

    private void SetTimerUIVisible(bool visible)
    {
        if (!dangerTimerText) return;
        dangerTimerText.gameObject.SetActive(visible);
    }

    private void UpdateTimerUI(float remaining)
    {
        if (!dangerTimerText) return;

        if (!showDecimals)
        {
            // 2.1 -> 3, 2.0 -> 2
            int secs = Mathf.CeilToInt(remaining);
            dangerTimerText.text = secs.ToString();
        }
        else
        {
            dangerTimerText.text = remaining.ToString($"F{Mathf.Clamp(decimalPlaces, 0, 3)}");
        }
    }
}