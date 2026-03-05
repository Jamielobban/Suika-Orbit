using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rewarded powerup: performs up to N merges by finding valid same-level pairs.
/// Picks closest pairs to reduce chaos.
/// Uses FruitMergeSystem.TryMerge to keep all existing VFX/score/combo logic.
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
    [Tooltip("Freeze fruit physics briefly while performing merges to avoid chain explosions.")]
    [SerializeField] private float freezeSeconds = 0.05f;

    [Header("Gravity Center Source (optional)")]
    [Tooltip("If empty, we auto-find PointGravity2D at runtime when needed.")]
    [SerializeField] private PointGravity2D gravity;

    // cache for speed
    private static readonly List<Fruit> _all = new(256);

    public void TryApplyRandomMerges()
    {
        TryApplyRandomMerges(mergesPerReward);
    }

    public void TryApplyRandomMerges(int mergeCount)
    {
        StartCoroutine(DoMerges(mergeCount));
    }

    private IEnumerator DoMerges(int mergeCount)
    {
        // Optional small freeze so things don't explode while we delete/spawn
        var bodies = FreezeAllFruitBodies(true);
        if (freezeSeconds > 0f)
            yield return new WaitForSecondsRealtime(freezeSeconds);

        for (int i = 0; i < mergeCount; i++)
        {
            // Rebuild list each iteration because merges destroy/spawn fruits
            GetAllFruits(_all);

            // Build groups by level where count >= 2
            var groups = BuildMergeGroups(_all);
            if (groups.Count == 0)
                break;

            int chosenLevel = ChooseLevel(groups);

            // pick two fruits from that level
            if (!TryPickPair(groups[chosenLevel], out Fruit a, out Fruit b))
                break;

            Vector2 center = GetGravityCenter();
            // Use an outward normal from gravity center so the spawned fruit is nudged away from the center
            Vector2 origin = ((Vector2)a.transform.position + (Vector2)b.transform.position) * 0.5f;
            Vector2 normal = (origin - center);
            if (normal.sqrMagnitude < 1e-6f) normal = Vector2.up;
            normal.Normalize();

            // Call your existing merge system.
            // collision=null is fine; it will use midpoint, but we want the "normal".
            // Your FruitMergeSystem currently uses normal only when collision exists,
            // so we pass a fake-ish normal by temporarily nudging via gravity center approach:
            // -> easiest: just call TryMerge with collision=null and accept Vector2.up normal.
            // However we can improve by adding an overload in FruitMergeSystem.
            // For now: use collision=null and rely on safe placement.
            FruitMergeSystem.TryMerge(a, b, null, center);

            // wait a FixedUpdate so spawned fruit gets enabled safely (your EnableSpawnedNextFixed runs next FixedUpdate)
            yield return new WaitForFixedUpdate();
        }

        FreezeAllFruitBodies(false, bodies);
    }

    // ---------- Helpers ----------

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
            // ✅ Only merge fruits actually in the field (dynamic bodies)
            if (!rb || rb.bodyType != RigidbodyType2D.Dynamic) continue;

            outList.Add(f);
        }
    }

    // groups[level] = list of fruits with that level
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

        // remove any levels with <2
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
                if (idx-- == 0) return kv.Key;
            }
        }

        // pick highest level available
        int best = int.MinValue;
        foreach (var kv in groups)
            if (kv.Key > best) best = kv.Key;
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
        Fruit bestA = null, bestB = null;

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