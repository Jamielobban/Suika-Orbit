using System.Collections.Generic;
using UnityEngine;

public class DottedPreviewRenderer : MonoBehaviour
{
    [Header("Dot")]
    [SerializeField] private SpriteRenderer dotPrefab;
    [SerializeField] private int poolSize = 64;

    [Header("Counts")]
    [Tooltip("How many dots to show along the full trajectory (constant).")]
    [SerializeField] private int dotsCount = 18;

    [Header("Base Look")]
    [SerializeField] private float dotScale = 0.12f;
    [SerializeField] private Gradient alphaOverPath;

    [Header("Scale Along Path")]
    [Tooltip("Multiplier at the start (near muzzle).")]
    [SerializeField] private float startScaleMultiplier = 2.0f;

    [Tooltip("Multiplier at the end (far/impact).")]
    [SerializeField] private float endScaleMultiplier = 0.6f;

    [Tooltip("Higher = keeps start big for longer then drops faster near end.")]
    [SerializeField] private float scalePower = 1.8f;

    [Tooltip("Extra 'big head' region length (0..1 of path).")]
    [SerializeField] private float startBoostRange = 0.25f;

    [Tooltip("Additional boost applied inside the start range (1 = none).")]
    [SerializeField] private float startBoostMultiplier = 1.25f;

    private readonly List<SpriteRenderer> pool = new();
    private int used;

    private void Awake()
    {
        pool.Clear();
        for (int i = 0; i < poolSize; i++)
        {
            var d = Instantiate(dotPrefab, transform);
            d.gameObject.SetActive(false);
            pool.Add(d);
        }
    }

    public void RenderFullPath(Vector3[] points, float power01)
    {
        if (points == null || points.Length < 2)
        {
            HideAll();
            return;
        }

        power01 = Mathf.Clamp01(power01);

        // Total length
        float totalLen = 0f;
        for (int i = 1; i < points.Length; i++)
            totalLen += Vector3.Distance(points[i - 1], points[i]);

        if (totalLen <= 1e-5f)
        {
            HideAll();
            return;
        }

        // Build cumulative distances for sampling
        int n = points.Length;
        float[] cum = new float[n];
        cum[0] = 0f;
        for (int i = 1; i < n; i++)
            cum[i] = cum[i - 1] + Vector3.Distance(points[i - 1], points[i]);

        int count = Mathf.Clamp(dotsCount, 2, pool.Count);
        used = 0;

        // Power affects “strength” only (not count)
        float overallAlpha = Mathf.Lerp(0.35f, 1.0f, power01);

        for (int k = 0; k < count; k++)
        {
            float t = (count == 1) ? 0f : (float)k / (count - 1); // 0..1 along path
            Vector3 pos = SampleByDistance(points, cum, t * totalLen);

            PlaceDot(pos, t, overallAlpha);
        }

        DisableUnused();
    }

    private static Vector3 SampleByDistance(Vector3[] pts, float[] cum, float dist)
    {
        int last = pts.Length - 1;

        for (int i = 1; i <= last; i++)
        {
            if (cum[i] >= dist)
            {
                float segLen = Mathf.Max(cum[i] - cum[i - 1], 1e-5f);
                float u = (dist - cum[i - 1]) / segLen;
                return Vector3.Lerp(pts[i - 1], pts[i], u);
            }
        }

        return pts[last];
    }

    private void PlaceDot(Vector3 pos, float pathT, float overallAlpha)
    {
        if (used >= pool.Count) return;

        var d = pool[used++];

        // --- Scale along path (tweakable) ---
        float shapedT = Mathf.Pow(Mathf.Clamp01(pathT), Mathf.Max(0.01f, scalePower));

        float mult = Mathf.Lerp(startScaleMultiplier, endScaleMultiplier, shapedT);

        // extra boost near the start (also tweakable)
        float head01 = 1f - Mathf.InverseLerp(0f, Mathf.Max(0.001f, startBoostRange), pathT);
        head01 = Mathf.SmoothStep(0f, 1f, head01);
        mult *= Mathf.Lerp(1.0f, startBoostMultiplier, head01);

        float scale = dotScale * mult;

        d.transform.position = pos;
        d.transform.localScale = Vector3.one * scale;

        // --- Alpha ---
        float baseA = alphaOverPath.Evaluate(pathT).a;
        float a = Mathf.Clamp01(baseA * overallAlpha);

        var c = d.color;
        c.a = a;
        d.color = c;

        if (!d.gameObject.activeSelf)
            d.gameObject.SetActive(true);
    }

    private void DisableUnused()
    {
        for (int i = used; i < pool.Count; i++)
            if (pool[i].gameObject.activeSelf)
                pool[i].gameObject.SetActive(false);
    }

    public void HideAll()
    {
        used = 0;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i].gameObject.activeSelf)
                pool[i].gameObject.SetActive(false);
    }
}