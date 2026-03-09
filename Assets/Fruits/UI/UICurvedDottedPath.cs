using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UICurvedDottedPath : MonoBehaviour
{
    [Header("Curve Points In Order")]
    [SerializeField] private List<RectTransform> controlPoints = new();

    [Header("Dots")]
    [SerializeField] private RectTransform dotsParent;
    [SerializeField] private Image dotPrefab;

    [Header("Layout")]
    [SerializeField] private float spacing = 26f;
    [SerializeField] private Vector2 dotSize = new Vector2(12f, 12f);
    [SerializeField] private int samplesPerSegment = 30;

    [Header("Style")]
    [SerializeField] private bool fadeEnds = false;

    private readonly List<Image> spawnedDots = new();

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Refresh();
#endif
    }

    [ContextMenu("Refresh Curved Dots")]
    public void Refresh()
    {
        if (dotsParent == null || dotPrefab == null) return;
        if (controlPoints == null || controlPoints.Count < 2) return;

        ClearDots();

        List<Vector2> sampled = BuildSampledCurve();
        if (sampled.Count < 2) return;

        float totalLength = 0f;
        for (int i = 1; i < sampled.Count; i++)
            totalLength += Vector2.Distance(sampled[i - 1], sampled[i]);

        if (totalLength <= spacing) return;

        float targetDistance = spacing;
        float traveled = 0f;

        int segIndex = 1;
        Vector2 prev = sampled[0];

        while (segIndex < sampled.Count)
        {
            Vector2 curr = sampled[segIndex];
            float segLen = Vector2.Distance(prev, curr);

            if (traveled + segLen >= targetDistance)
            {
                float remain = targetDistance - traveled;
                float t = segLen > 0.0001f ? remain / segLen : 0f;
                Vector2 pos = Vector2.Lerp(prev, curr, t);

                Image dot = CreateDot();
                RectTransform rt = dot.rectTransform;
                rt.SetParent(dotsParent, false);
                rt.anchoredPosition = pos;
                rt.sizeDelta = dotSize;
                rt.localScale = Vector3.one;

                if (fadeEnds)
                {
                    float progress01 = Mathf.Clamp01(targetDistance / totalLength);
                    Color c = dot.color;
                    c.a = Mathf.Lerp(0.35f, 1f, Mathf.Sin(progress01 * Mathf.PI));
                    dot.color = c;
                }

                spawnedDots.Add(dot);

                prev = pos;
                traveled = targetDistance;
                targetDistance += spacing;
            }
            else
            {
                traveled += segLen;
                prev = curr;
                segIndex++;
            }
        }
    }

    private List<Vector2> BuildSampledCurve()
    {
        List<Vector2> result = new();

        if (controlPoints.Count == 2)
        {
            result.Add(controlPoints[0].anchoredPosition);
            result.Add(controlPoints[1].anchoredPosition);
            return result;
        }

        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector2 p0 = controlPoints[Mathf.Max(i - 1, 0)].anchoredPosition;
            Vector2 p1 = controlPoints[i].anchoredPosition;
            Vector2 p2 = controlPoints[i + 1].anchoredPosition;
            Vector2 p3 = controlPoints[Mathf.Min(i + 2, controlPoints.Count - 1)].anchoredPosition;

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        result.Add(controlPoints[controlPoints.Count - 1].anchoredPosition);
        return result;
    }

    private Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Image CreateDot()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return (Image)UnityEditor.PrefabUtility.InstantiatePrefab(dotPrefab, dotsParent);
#endif
        return Instantiate(dotPrefab, dotsParent);
    }

    private void ClearDots()
    {
        for (int i = spawnedDots.Count - 1; i >= 0; i--)
        {
            if (spawnedDots[i] == null) continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(spawnedDots[i].gameObject);
            else
#endif
                Destroy(spawnedDots[i].gameObject);
        }

        spawnedDots.Clear();

#if UNITY_EDITOR
        if (!Application.isPlaying && dotsParent != null)
        {
            for (int i = dotsParent.childCount - 1; i >= 0; i--)
            {
                Transform child = dotsParent.GetChild(i);
                if (child.name.StartsWith(dotPrefab.name))
                    DestroyImmediate(child.gameObject);
            }
        }
#endif
    }
}