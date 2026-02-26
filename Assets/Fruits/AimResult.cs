using UnityEngine;

public struct AimResult
{
    public Vector2 dir;
    public float power01;
    public bool invertGravity;

    public AimResult(Vector2 dir, float power01, bool invertGravity)
    {
        this.dir = (dir.sqrMagnitude < 1e-6f) ? Vector2.up : dir.normalized;
        this.power01 = Mathf.Clamp01(power01);
        this.invertGravity = invertGravity;
    }
}
