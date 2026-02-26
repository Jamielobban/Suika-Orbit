using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CometSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private CometMover cometPrefab;

    [Header("Pool")]
    [SerializeField] private int poolSize = 6;

    [Header("Spawn")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(12f, 25f);
    [SerializeField] private float offscreenMargin = 2.0f;

    [Header("Plane")]
    [Tooltip("World Z plane where comets live (usually 0 in 2D).")]
    [SerializeField] private float cometsZ = 0f;

    [Header("Edge Weights")]
    [Tooltip("Higher = spawns more often from that edge.")]
    [SerializeField] private float weightLeft = 0.7f;
    [SerializeField] private float weightRight = 0.7f;
    [SerializeField] private float weightTop = 1.6f;
    [SerializeField] private float weightBottom = 1.6f;

    [Header("Corner Avoidance")]
    [Tooltip("0.0 = allow corners, 0.2 = avoid outer 20% near corners.")]
    [Range(0f, 0.45f)]
    [SerializeField] private float edgeInsetPercent = 0.18f;

    [Header("Motion")]
    [SerializeField] private Vector2 speedRange = new Vector2(12f, 20f);
    [SerializeField] private Vector2 turnDegPerSecRange = new Vector2(15f, 50f);
    [SerializeField] private Vector2 lifeRange = new Vector2(3f, 6f);

    [Header("Aim")]
    [SerializeField] private Vector2 inwardAngleRange = new Vector2(45f, 80f);
    [SerializeField] private float minInwardDot = 0.25f;
    [SerializeField] private int directionTries = 10;

    private readonly Queue<CometMover> pool = new();
    private Coroutine loopRoutine;

    private enum Edge { Left, Right, Top, Bottom }

    private void OnEnable()
    {
        if (!cam) cam = Camera.main;

        if (pool.Count == 0)
            Prewarm();

        loopRoutine = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
    }

    private void Prewarm()
    {
        pool.Clear();
        for (int i = 0; i < poolSize; i++)
        {
            var comet = Instantiate(cometPrefab, transform);
            comet.gameObject.SetActive(false);
            pool.Enqueue(comet);
        }
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSecondsRealtime(wait);
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (pool.Count == 0) return;
        offscreenMargin = Mathf.Max(0.01f, offscreenMargin);

        GetCameraBounds(out float left, out float right, out float bottom, out float top);

        Edge edge = PickWeightedEdge();

        // inset range so we avoid corners (and the “always on vertical margins” feeling)
        float insetX = (right - left) * edgeInsetPercent;
        float insetY = (top - bottom) * edgeInsetPercent;

        float xMin = left + insetX;
        float xMax = right - insetX;
        float yMin = bottom + insetY;
        float yMax = top - insetY;

        Vector2 spawnPos;
        Vector2 inwardNormal;

        switch (edge)
        {
            case Edge.Left:
                spawnPos = new Vector2(left - offscreenMargin, Random.Range(yMin, yMax));
                inwardNormal = Vector2.right;
                break;

            case Edge.Right:
                spawnPos = new Vector2(right + offscreenMargin, Random.Range(yMin, yMax));
                inwardNormal = Vector2.left;
                break;

            case Edge.Top:
                spawnPos = new Vector2(Random.Range(xMin, xMax), top + offscreenMargin);
                inwardNormal = Vector2.down;
                break;

            default: // Bottom
                spawnPos = new Vector2(Random.Range(xMin, xMax), bottom - offscreenMargin);
                inwardNormal = Vector2.up;
                break;
        }

        // Pick a direction that enters view
        Vector2 dir = inwardNormal;
        bool found = false;

        for (int t = 0; t < Mathf.Max(1, directionTries); t++)
        {
            float angle = Random.Range(inwardAngleRange.x, inwardAngleRange.y);
            if (Random.value < 0.5f) angle = -angle;

            Vector2 candidate = Rotate(inwardNormal, angle).normalized;

            if (Vector2.Dot(candidate, inwardNormal) < minInwardDot)
                continue;

            if (!WillEnterView(spawnPos, candidate, left, right, bottom, top))
                continue;

            dir = candidate;
            found = true;
            break;
        }

        if (!found) dir = inwardNormal;

        float speed = Random.Range(speedRange.x, speedRange.y);

        float turn = Random.Range(turnDegPerSecRange.x, turnDegPerSecRange.y);
        if (Random.value < 0.5f) turn = -turn;

        float life = Random.Range(lifeRange.x, lifeRange.y);

        var comet = pool.Dequeue();
        comet.transform.position = new Vector3(spawnPos.x, spawnPos.y, cometsZ);
        comet.Spawn(spawnPos, dir, speed, turn, life, ReturnToPool);
    }

    private Edge PickWeightedEdge()
    {
        float wl = Mathf.Max(0f, weightLeft);
        float wr = Mathf.Max(0f, weightRight);
        float wt = Mathf.Max(0f, weightTop);
        float wb = Mathf.Max(0f, weightBottom);

        float sum = wl + wr + wt + wb;
        if (sum <= 0.0001f) return (Edge)Random.Range(0, 4);

        float r = Random.value * sum;

        if (r < wl) return Edge.Left;
        r -= wl;

        if (r < wr) return Edge.Right;
        r -= wr;

        if (r < wt) return Edge.Top;
        return Edge.Bottom;
    }

    private void ReturnToPool(CometMover c)
    {
        pool.Enqueue(c);
    }

    private void GetCameraBounds(out float left, out float right, out float bottom, out float top)
    {
        if (cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector3 cp = cam.transform.position;

            left = cp.x - halfW;
            right = cp.x + halfW;
            bottom = cp.y - halfH;
            top = cp.y + halfH;
            return;
        }

        float depth = Mathf.Abs(cam.transform.position.z - cometsZ);
        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, depth));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, depth));

        left = bl.x;
        bottom = bl.y;
        right = tr.x;
        top = tr.y;
    }

    private static bool WillEnterView(Vector2 pos, Vector2 dir, float left, float right, float bottom, float top)
    {
        Vector2 p = pos;
        const int steps = 90;
        const float stepDist = 0.30f;

        for (int i = 0; i < steps; i++)
        {
            p += dir * stepDist;
            if (p.x >= left && p.x <= right && p.y >= bottom && p.y <= top)
                return true;
        }

        return false;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(a);
        float c = Mathf.Cos(a);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }
}
