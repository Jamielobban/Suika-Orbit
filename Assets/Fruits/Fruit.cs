using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Fruit : MonoBehaviour
{
    [Min(0)] public int level;
    public FruitDatabase database;

    [System.NonSerialized] public bool isMerging;

    private float canMergeAtTime;

    private static PointGravity2D cachedGravity; // cheap shared cache

    public void LockMergingFor(float seconds)
    {
        canMergeAtTime = Time.time + seconds;
    }

    public bool CanMergeNow() => Time.time >= canMergeAtTime;

    private void OnCollisionEnter2D(Collision2D collision) => TryHandleMerge(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryHandleMerge(collision);

    private void TryHandleMerge(Collision2D collision)
    {
        if (isMerging) return;
        if (!CanMergeNow()) return;

        var other = collision.collider.GetComponent<Fruit>();
        if (!other) return;
        if (other.isMerging) return;
        if (!other.CanMergeNow()) return;
        if (other.level != level) return;

        if (!cachedGravity)
            cachedGravity = FindFirstObjectByType<PointGravity2D>();

        Vector2 center = cachedGravity ? (Vector2)cachedGravity.Center.position : Vector2.zero;
        Debug.Log(cachedGravity.transform.position + ", " + cachedGravity.gameObject.name);

        FruitMergeSystem.TryMerge(this, other, collision, center);
    }
}
