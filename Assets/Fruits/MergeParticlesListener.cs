using UnityEngine;
using System.Collections;
public class MergeParticlesListener : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private MergeVFXSet vfxPrefab;
    [SerializeField] private int poolSize = 5;

    [Header("Scaling")]
    [SerializeField] private bool scaleWithLevel = true;
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float scalePerLevel = 0.06f;
    [SerializeField] private float maxScale = 1.8f;

    [Header("Combo Extras")]
    [SerializeField] private bool extraWithCombo = true;
    [SerializeField] private int extraBurstPerCombo = 2;
    [SerializeField] private int extraSparklesPerCombo = 1;

    private MergeVFXSet[] pool;

    private void Awake()
    {
        pool = new MergeVFXSet[Mathf.Max(1, poolSize)];

        for (int i = 0; i < pool.Length; i++)
        {
            var set = Instantiate(vfxPrefab, transform);
            set.gameObject.SetActive(false);
            pool[i] = set;
        }
    }

    private void OnEnable() => GameSignals.FruitMerged += OnFruitMerged;
    private void OnDisable() => GameSignals.FruitMerged -= OnFruitMerged;

    private void OnFruitMerged(Vector2 pos, int newLevel, int combo)
    {
        var set = GetAvailableSet();
        if (set == null) return; // pool exhausted this frame, just skip

        float s = baseScale;
        if (scaleWithLevel)
            s = Mathf.Min(maxScale, baseScale + newLevel * scalePerLevel);

        int extraBurst = extraWithCombo ? Mathf.Max(0, combo - 1) * extraBurstPerCombo : 0;
        int extraSpark = extraWithCombo ? Mathf.Max(0, combo - 1) * extraSparklesPerCombo : 0;

        set.Play(pos, s, extraBurst, extraSpark);

        // return to pool when done
        StartCoroutine(ReturnWhenDone(set));
    }

    private MergeVFXSet GetAvailableSet()
    {
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null && !pool[i].IsBusy)
                return pool[i];
        }
        return null;
    }

    private IEnumerator ReturnWhenDone(MergeVFXSet set)
    {
        // wait until particles finish
        while (set != null && set.IsBusy)
            yield return null;

        if (set != null)
            set.gameObject.SetActive(false);
    }
}