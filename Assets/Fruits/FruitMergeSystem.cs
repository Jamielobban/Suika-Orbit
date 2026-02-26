using System.Collections;
using UnityEngine;

public static class FruitMergeSystem
{
    private const float MergeSpawnLockTime = 0.05f;
    private const float MergeDestroyDelay = 0.02f;

    // Push out from contact by radius + epsilon
    private const float SpawnEpsilon = 0.02f;

    // Keep at 0 for no pop
    private const float InitialUpVelocity = 0.00f;

    // Put all fruits on a dedicated layer named "Fruit"
    private const string FruitLayerName = "Fruit";

    // --- Shockwave when two MAX level fruits merge (subtle nudge)
    private const float MaxMergeShockRadius = 5.5f;  // tune
    private const float MaxMergeShockImpulse = 70.6f;  // tune (0.8–2.5)
    private const float MaxMergeShockUpBias = 0.18f; // tiny "wave" feel

    public static void TryMerge(Fruit a, Fruit b, Collision2D collision, Vector2 gravityCenter)
    {
        if (!a || !b) return;
        if (a.isMerging || b.isMerging) return;
        if (a.database == null) return;

        // Must be same level
        if (a.level != b.level) return;

        // Only one side initiates (prevents double-trigger)
        if (a.GetInstanceID() > b.GetInstanceID()) return;

        var db = a.database;

        int nextLevel = a.level + 1;
        Fruit nextPrefab = db.GetPrefab(nextLevel);

        a.isMerging = true;
        b.isMerging = true;

        // ---- pick merge origin (midpoint or contact)
        Vector2 origin = ((Vector2)a.transform.position + (Vector2)b.transform.position) * 0.5f;
        Vector2 normal = Vector2.up;

        if (collision != null && collision.contactCount > 0)
        {
            var c = collision.GetContact(0);
            origin = c.point;
            normal = c.normal;
        }

        // --- IMPORTANT: remove A/B from physics immediately (prevents overlap "explosions")
        DisablePhysics(a);
        DisablePhysics(b);

        // ----------------------------
        // MAX LEVEL CASE:
        // If there is no next prefab, DO NOT spawn anything.
        // Just shockwave + delete both.
        // ----------------------------
        if (nextPrefab == null)
        {
            int fruitMask = LayerMask.GetMask(FruitLayerName);
            if (fruitMask == 0) fruitMask = ~0;

            ApplyShockwaveOrbitStyle(origin, gravityCenter, MaxMergeShockRadius, MaxMergeShockImpulse, fruitMask);

            // optional: signal for VFX/SFX
            GameSignals.RaiseFruitMerged(origin, a.level, 1);

            Object.Destroy(a.gameObject, MergeDestroyDelay);
            Object.Destroy(b.gameObject, MergeDestroyDelay);
            Debug.Log("Shockwave");
            return;
        }

        // ----------------------------
        // NORMAL MERGE CASE:
        // Spawn next level as usual
        // ----------------------------
        Fruit merged = Object.Instantiate(nextPrefab, origin, Quaternion.identity);
        merged.database = db;
        merged.level = nextLevel;

        Vector2 mergedPos = PlaceSpawnedSafely(merged, origin, normal);

        var juice = merged.GetComponent<FruitJuiceMMF>();
        if (juice != null)
        {
            juice.PlayMergeSquash();
        }

        int combo = 1;
        if (ComboManager.I != null)
            combo = ComboManager.I.RegisterMerge(mergedPos, nextLevel);

        GameSignals.RaiseFruitMerged(mergedPos, nextLevel, combo);

        Object.Destroy(a.gameObject, MergeDestroyDelay);
        Object.Destroy(b.gameObject, MergeDestroyDelay);
    }

    // Spawns with physics off, pushes out by radius, finds non-overlap, enables next FixedUpdate.
    private static Vector2 PlaceSpawnedSafely(Fruit spawned, Vector2 spawnPos, Vector2 normal)
    {
        var rb = spawned.GetComponent<Rigidbody2D>();
        var circle = spawned.GetComponent<CircleCollider2D>();

        if (rb != null) rb.simulated = false;
        if (circle != null) circle.enabled = false;

        float rWorld = 0.5f;
        if (circle != null)
            rWorld = circle.radius * spawned.transform.lossyScale.x;

        Vector2 targetPos = spawnPos + normal * (rWorld + SpawnEpsilon);
        targetPos = FindNonOverlappingPosition(targetPos, rWorld);

        if (rb != null) rb.position = targetPos;
        else spawned.transform.position = targetPos;

        spawned.LockMergingFor(MergeSpawnLockTime);

        if (spawned != null)
            spawned.StartCoroutine(EnableSpawnedNextFixed(rb, circle));

        return targetPos;
    }

    private static void DisablePhysics(Fruit f)
    {
        if (!f) return;

        var col = f.GetComponent<Collider2D>();
        if (col) col.enabled = false;

        var rb = f.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    private static IEnumerator EnableSpawnedNextFixed(Rigidbody2D rb, CircleCollider2D col)
    {
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, InitialUpVelocity);
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        if (col != null)
            col.enabled = true;
    }

    // Subtle shockwave: radial impulse with smooth falloff, scaled by mass for consistent feel.
    private static void ApplyShockwaveOrbitStyle(
    Vector2 origin,
    Vector2 center,
    float radius,
    float impulse,
    int mask)
    {
        var hits = Physics2D.OverlapCircleAll(origin, radius, mask);
        if (hits == null || hits.Length == 0) return;

        float r2 = radius * radius;

        for (int i = 0; i < hits.Length; i++)
        {
            var rb = hits[i].attachedRigidbody;
            if (!rb) continue;
            if (rb.bodyType != RigidbodyType2D.Dynamic) continue;

            Vector2 toBody = rb.position - origin;
            float d2 = toBody.sqrMagnitude;

            float t = Mathf.Clamp01(d2 / r2);
            float k = (1f - t);
            k = k * k;

            // Orbit-style direction: mostly tangential around gravity center,
            // with a small outward push from the explosion origin.
            Vector2 radialFromCenter = (rb.position - center);
            if (radialFromCenter.sqrMagnitude < 1e-6f) radialFromCenter = Vector2.up;
            radialFromCenter.Normalize();

            Vector2 tangent = new Vector2(-radialFromCenter.y, radialFromCenter.x);

            Vector2 outward = (toBody.sqrMagnitude < 1e-6f) ? Vector2.up : toBody.normalized;

            Vector2 dir = (tangent * 0.7f + outward * 0.3f).normalized;

            rb.linearVelocity += dir * (impulse * k);
        }
    }

    // Spiral search around the desired position to avoid spawning inside another fruit.
    private static Vector2 FindNonOverlappingPosition(Vector2 desired, float rWorld)
    {
        int fruitMask = LayerMask.GetMask(FruitLayerName);
        if (fruitMask == 0) fruitMask = ~0;

        if (!Physics2D.OverlapCircle(desired, rWorld, fruitMask))
            return desired;

        const int rings = 10;
        const int stepsPerRing = 16;
        const float step = 0.15f;

        for (int ring = 1; ring <= rings; ring++)
        {
            float dist = ring * step;
            for (int i = 0; i < stepsPerRing; i++)
            {
                float ang = (i / (float)stepsPerRing) * Mathf.PI * 2f;
                Vector2 p = desired + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

                if (!Physics2D.OverlapCircle(p, rWorld, fruitMask))
                    return p;
            }
        }

        return desired;
    }
}
