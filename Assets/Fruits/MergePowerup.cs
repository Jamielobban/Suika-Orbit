using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Rewarded powerup: performs up to N animated merges by finding valid same-level pairs.
/// One fruit stays anchored, the other gets pulled into it, then the normal merge is triggered.
/// Uses unscaled time so it works while game is paused.
/// </summary>
public class MergePowerup : MonoBehaviour
{
    [Header("How many merges to attempt")]
    [SerializeField] private int mergesPerReward = 3;

    [Header("Pair picking")]
    [Tooltip("If true, prefers the closest pair in the chosen level group.")]
    [SerializeField] private bool preferClosestPair = true;

    [Tooltip("If true, picks a random level group first; if false, picks the highest available level first.")]
    [SerializeField] private bool pickRandomLevelGroup = true;

    [Header("Safety")]
    [Tooltip("Freeze fruit physics while performing reward merges.")]
    [SerializeField] private bool freezeBodiesDuringSequence = true;

    [Header("Gravity Center Source (optional)")]
    [Tooltip("If empty, we auto-find PointGravity2D at runtime when needed.")]
    [SerializeField] private PointGravity2D gravity;

    [Header("Reward Animation")]
    [SerializeField] private float preDelay = 0.08f;
    [SerializeField] private float highlightDuration = 0.12f;
    [SerializeField] private float flyDuration = 0.22f;
    [SerializeField] private float hitPause = 0.05f;
    [SerializeField] private float betweenMergesDelay = 0.08f;

    [Tooltip("How much both fruits scale up during the highlight.")]
    [SerializeField] private float highlightScale = 1.12f;

    [Tooltip("How much the anchor fruit punches on impact.")]
    [SerializeField] private float anchorPunchScale = 0.18f;

    [Tooltip("Optional ease for the flying fruit.")]
    [SerializeField] private Ease flyEase = Ease.InQuad;

    [Header("Optional FX")]
    [Tooltip("Optional particle effect spawned at the anchor when the merge hits.")]
    [SerializeField] private ParticleSystem impactBurstPrefab;

    [Tooltip("Optional trail effect that gets parented to the moving fruit during flight.")]
    [SerializeField] private ParticleSystem moverTrailPrefab;

    [Tooltip("Optional ring/highlight object to spawn under both fruits before flight.")]
    [SerializeField] private GameObject highlightRingPrefab;

    [Header("Optional Visual Tint")]
    [SerializeField] private bool tintSpritesDuringHighlight = true;
    [SerializeField] private Color highlightColor = Color.white;

    private static readonly List<Fruit> _all = new(256);

    private struct MergePair
    {
        public Fruit anchor;
        public Fruit mover;

        public MergePair(Fruit anchor, Fruit mover)
        {
            this.anchor = anchor;
            this.mover = mover;
        }
    }

    public void TryApplyRandomMerges()
    {
        TryApplyRandomMerges(mergesPerReward);
    }

    public void TryApplyRandomMerges(int mergeCount)
    {
        StartCoroutine(DoInstantMerges(mergeCount));
    }

    /// <summary>
    /// New method: use this for rewarded continue while the game is paused.
    /// </summary>
    public IEnumerator PlayRewardMergeSequence(int mergeCount)
    {
        List<MergePair> pairs = PlanMergePairs(mergeCount);
        if (pairs.Count == 0)
            yield break;

        Rigidbody2D[] frozenBodies = null;

        if (freezeBodiesDuringSequence)
            frozenBodies = FreezeAllFruitBodies(true);

        yield return new WaitForSecondsRealtime(preDelay);

        Vector2 gravityCenter = GetGravityCenter();

        for (int i = 0; i < pairs.Count; i++)
        {
            MergePair pair = pairs[i];

            if (!pair.anchor || !pair.mover)
                continue;

            yield return AnimateAndMergePair(pair.anchor, pair.mover, gravityCenter);

            if (betweenMergesDelay > 0f)
                yield return new WaitForSecondsRealtime(betweenMergesDelay);
        }

        if (freezeBodiesDuringSequence)
            FreezeAllFruitBodies(false, frozenBodies);
    }

    private IEnumerator DoInstantMerges(int mergeCount)
    {
        var bodies = FreezeAllFruitBodies(true);

        for (int i = 0; i < mergeCount; i++)
        {
            GetAllFruits(_all);
            var groups = BuildMergeGroups(_all);
            if (groups.Count == 0)
                break;

            int chosenLevel = ChooseLevel(groups);

            if (!TryPickPair(groups[chosenLevel], out Fruit a, out Fruit b))
                break;

            Vector2 center = GetGravityCenter();
            FruitMergeSystem.TryMerge(a, b, null, center);

            // only safe when game isn't paused
            yield return new WaitForFixedUpdate();
        }

        FreezeAllFruitBodies(false, bodies);
    }

    private IEnumerator AnimateAndMergePair(Fruit anchor, Fruit mover, Vector2 gravityCenter)
    {
        if (!anchor || !mover)
            yield break;

        Transform anchorT = anchor.transform;
        Transform moverT = mover.transform;

        Vector3 anchorStartScale = anchorT.localScale;
        Vector3 moverStartScale = moverT.localScale;

        SpriteRenderer anchorSr = anchor.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer moverSr = mover.GetComponentInChildren<SpriteRenderer>();

        Color? anchorOriginalColor = null;
        Color? moverOriginalColor = null;

        if (tintSpritesDuringHighlight)
        {
            if (anchorSr) anchorOriginalColor = anchorSr.color;
            if (moverSr) moverOriginalColor = moverSr.color;
        }

        GameObject ringA = null;
        GameObject ringB = null;
        ParticleSystem moverTrail = null;

        if (highlightRingPrefab)
        {
            ringA = Instantiate(highlightRingPrefab, anchorT.position, Quaternion.identity);
            ringB = Instantiate(highlightRingPrefab, moverT.position, Quaternion.identity);
        }

        if (moverTrailPrefab)
        {
            moverTrail = Instantiate(moverTrailPrefab, moverT.position, Quaternion.identity, moverT);
        }

        if (ringA) ringA.transform.localScale = Vector3.zero;
        if (ringB) ringB.transform.localScale = Vector3.zero;

        Sequence intro = DOTween.Sequence().SetUpdate(true);

        intro.Join(anchorT.DOScale(anchorStartScale * highlightScale, highlightDuration).SetEase(Ease.OutBack));
        intro.Join(moverT.DOScale(moverStartScale * highlightScale, highlightDuration).SetEase(Ease.OutBack));

        if (ringA) intro.Join(ringA.transform.DOScale(1f, highlightDuration).SetEase(Ease.OutBack));
        if (ringB) intro.Join(ringB.transform.DOScale(1f, highlightDuration).SetEase(Ease.OutBack));

        if (tintSpritesDuringHighlight)
        {
            if (anchorSr) intro.Join(anchorSr.DOColor(highlightColor, highlightDuration));
            if (moverSr) intro.Join(moverSr.DOColor(highlightColor, highlightDuration));
        }

        yield return intro.WaitForCompletion();

        Vector3 anchorPos = anchorT.position;

        // fire event so audio / feedback systems can react
        GameSignals.RaiseRewardMergeTargetSelected(anchorPos);

        Sequence pull = DOTween.Sequence().SetUpdate(true);

        pull.Join(
            moverT.DOMove(anchorPos, flyDuration)
                .SetEase(flyEase)
        );

        pull.Join(
            anchorT.DOPunchScale(Vector3.one * anchorPunchScale, flyDuration, 1, 0.2f)
        );

        if (ringB)
        {
            pull.Join(ringB.transform.DOScale(0.5f, flyDuration));
        }

        yield return pull.WaitForCompletion();

        if (mover)
            mover.transform.position = anchorPos;

        if (impactBurstPrefab)
            Instantiate(impactBurstPrefab, anchorPos, Quaternion.identity);

        if (ringA) Destroy(ringA);
        if (ringB) Destroy(ringB);
        if (moverTrail) Destroy(moverTrail.gameObject);

        if (anchorT)
            anchorT.localScale = anchorStartScale;
        if (moverT)
            moverT.localScale = moverStartScale;

        if (tintSpritesDuringHighlight)
        {
            if (anchorSr && anchorOriginalColor.HasValue) anchorSr.color = anchorOriginalColor.Value;
            if (moverSr && moverOriginalColor.HasValue) moverSr.color = moverOriginalColor.Value;
        }

        yield return new WaitForSecondsRealtime(hitPause);

        if (anchor && mover)
            FruitMergeSystem.TryMerge(anchor, mover, null, gravityCenter);
    }

    private List<MergePair> PlanMergePairs(int mergeCount)
    {
        var result = new List<MergePair>(mergeCount);

        GetAllFruits(_all);

        // local working list so we can remove chosen fruits and not reuse them
        List<Fruit> working = new List<Fruit>(_all);

        for (int i = 0; i < mergeCount; i++)
        {
            var groups = BuildMergeGroups(working);
            if (groups.Count == 0)
                break;

            int chosenLevel = ChooseLevel(groups);

            if (!TryPickPair(groups[chosenLevel], out Fruit a, out Fruit b))
                break;

            ChooseAnchorAndMover(a, b, out Fruit anchor, out Fruit mover);

            result.Add(new MergePair(anchor, mover));

            working.Remove(a);
            working.Remove(b);
        }

        return result;
    }

    private void ChooseAnchorAndMover(Fruit a, Fruit b, out Fruit anchor, out Fruit mover)
    {
        Vector2 center = GetGravityCenter();

        float da = ((Vector2)a.transform.position - center).sqrMagnitude;
        float db = ((Vector2)b.transform.position - center).sqrMagnitude;

        // Keep the one closer to center as anchor so the result feels more stable
        if (da <= db)
        {
            anchor = a;
            mover = b;
        }
        else
        {
            anchor = b;
            mover = a;
        }
    }

    private Vector2 GetGravityCenter()
    {
        if (!gravity)
            gravity = FindFirstObjectByType<PointGravity2D>();

        if (gravity && gravity.Center)
            return gravity.Center.position;

        return Vector2.zero;
    }

    private static void GetAllFruits(List<Fruit> outList)
    {
        outList.Clear();
        var fruits = FindObjectsOfType<Fruit>();

        for (int i = 0; i < fruits.Length; i++)
        {
            var f = fruits[i];
            if (!f) continue;
            if (f.isMerging) continue;

            var rb = f.GetComponent<Rigidbody2D>();
            if (!rb || rb.bodyType != RigidbodyType2D.Dynamic) continue;

            outList.Add(f);
        }
    }

    private static Dictionary<int, List<Fruit>> BuildMergeGroups(List<Fruit> fruits)
    {
        var groups = new Dictionary<int, List<Fruit>>(16);

        for (int i = 0; i < fruits.Count; i++)
        {
            var f = fruits[i];
            if (!f) continue;

            if (!groups.TryGetValue(f.level, out var list))
            {
                list = new List<Fruit>(8);
                groups.Add(f.level, list);
            }

            list.Add(f);
        }

        var toRemove = new List<int>();

        foreach (var kv in groups)
        {
            if (kv.Value.Count < 2)
                toRemove.Add(kv.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
            groups.Remove(toRemove[i]);

        return groups;
    }

    private int ChooseLevel(Dictionary<int, List<Fruit>> groups)
    {
        if (pickRandomLevelGroup)
        {
            int idx = Random.Range(0, groups.Count);
            foreach (var kv in groups)
            {
                if (idx-- == 0)
                    return kv.Key;
            }
        }

        int best = int.MinValue;
        foreach (var kv in groups)
        {
            if (kv.Key > best)
                best = kv.Key;
        }

        return best;
    }

    private bool TryPickPair(List<Fruit> list, out Fruit a, out Fruit b)
    {
        a = null;
        b = null;

        if (list == null || list.Count < 2)
            return false;

        if (!preferClosestPair)
        {
            a = list[Random.Range(0, list.Count)];
            do { b = list[Random.Range(0, list.Count)]; } while (b == a);
            return a && b;
        }

        float best = float.PositiveInfinity;
        Fruit bestA = null;
        Fruit bestB = null;

        for (int i = 0; i < list.Count; i++)
        {
            var fi = list[i];
            if (!fi) continue;

            Vector2 pi = fi.transform.position;

            for (int j = i + 1; j < list.Count; j++)
            {
                var fj = list[j];
                if (!fj) continue;

                float d = (pi - (Vector2)fj.transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestA = fi;
                    bestB = fj;
                }
            }
        }

        a = bestA;
        b = bestB;
        return a && b;
    }

    private Rigidbody2D[] FreezeAllFruitBodies(bool freeze, Rigidbody2D[] reuse = null)
    {
        if (freeze)
        {
            var fruits = FindObjectsOfType<Fruit>();
            var bodies = new List<Rigidbody2D>(fruits.Length);

            for (int i = 0; i < fruits.Length; i++)
            {
                var rb = fruits[i] ? fruits[i].GetComponent<Rigidbody2D>() : null;
                if (!rb) continue;

                bodies.Add(rb);
                rb.simulated = false;
            }

            return bodies.ToArray();
        }
        else
        {
            if (reuse == null) return null;

            for (int i = 0; i < reuse.Length; i++)
            {
                if (reuse[i])
                    reuse[i].simulated = true;
            }

            return null;
        }
    }
}