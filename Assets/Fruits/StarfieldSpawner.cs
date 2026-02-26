using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarfieldSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject starPrefab;

    [Header("Pooling")]
    [SerializeField] private int poolSize = 30;

    [Header("Spawn Timing")]
    [Tooltip("Time between star pulses. If too small, you'll feel constant regrowing.")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.6f, 1.8f);

    [Header("Scale")]
    [SerializeField] private Vector2 targetScaleRange = new Vector2(0.18f, 0.55f);

    [Header("Timing (Pulse)")]
    [SerializeField] private Vector2 growDurationRange = new Vector2(0.25f, 0.6f);
    [SerializeField] private Vector2 shrinkDurationRange = new Vector2(0.25f, 0.55f);

    [Header("Alpha")]
    [SerializeField] private bool animateAlpha = true;
    [SerializeField] private Vector2 peakAlphaRange = new Vector2(0.10f, 0.28f);

    [Header("World Plane")]
    [SerializeField] private float starsZ = 0f;

    private readonly Queue<GameObject> pool = new();

    private void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    private void Start()
    {
        PrewarmPool();
        StartCoroutine(SpawnRoutine());
    }

    private void OnDestroy()
    {
        foreach (var go in pool)
        {
            if (!go) continue;
            go.transform.DOKill();
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.DOKill();
        }
    }

    private void PrewarmPool()
    {
        pool.Clear();
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(starPrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnStarPulse();
            yield return new WaitForSeconds(Random.Range(spawnIntervalRange.x, spawnIntervalRange.y));
        }
    }

    private void SpawnStarPulse()
    {
        if (pool.Count == 0) return;

        GameObject star = pool.Dequeue();
        star.SetActive(true);

        Vector2 pos = RandomPointInCameraView(cam, starsZ);
        star.transform.position = new Vector3(pos.x, pos.y, starsZ);

        // reset transform tween + scale
        star.transform.DOKill();
        star.transform.localScale = Vector3.zero;

        var sr = star.GetComponent<SpriteRenderer>();
        if (sr) sr.DOKill();

        float targetScale = Random.Range(targetScaleRange.x, targetScaleRange.y);
        float growDur = Random.Range(growDurationRange.x, growDurationRange.y);
        float shrinkDur = Random.Range(shrinkDurationRange.x, shrinkDurationRange.y);

        // If you want to guarantee "pulse once, then gone" feel:
        // ensure the next spawn isn't faster than the pulse duration (optional)
        // (You can also just set spawnIntervalRange higher in inspector.)
        // float pulseDur = growDur + shrinkDur;

        float peakA = animateAlpha ? Random.Range(peakAlphaRange.x, peakAlphaRange.y) : 1f;

        if (sr)
        {
            var c = sr.color;

            if (animateAlpha)
                c.a = 0f;     // start invisible; we'll fade in/out with the pulse
            else
                c.a = 1f;     // keep visible if we are NOT animating alpha

            sr.color = c;
        }

        Sequence seq = DOTween.Sequence();

        // Grow
        seq.Append(star.transform.DOScale(targetScale, growDur).SetEase(Ease.OutSine));

        if (sr && animateAlpha)
            seq.Join(sr.DOFade(peakA, growDur).SetEase(Ease.OutSine));

        // Shrink
        seq.Append(star.transform.DOScale(0f, shrinkDur).SetEase(Ease.InSine));

        if (sr && animateAlpha)
            seq.Join(sr.DOFade(0f, shrinkDur).SetEase(Ease.InSine));
        else if (sr)
        {
            // If alpha isn't animated, hide ONLY at the end (when scale hits 0)
            seq.OnComplete(() =>
            {
                if (!sr) return;
                var c = sr.color;
                c.a = 0f;
                sr.color = c;
            });
        }

        seq.OnComplete(() =>
        {
            if (!star) return;

            // Return to pool
            star.SetActive(false);
            pool.Enqueue(star);
        });
    }

    private static Vector2 RandomPointInCameraView(Camera cam, float z)
    {
        float depth = Mathf.Abs(cam.transform.position.z - z);
        Vector3 w = cam.ViewportToWorldPoint(new Vector3(Random.value, Random.value, depth));
        return new Vector2(w.x, w.y);
    }
}
