using UnityEngine;

[CreateAssetMenu(menuName = "Suika/Aim Strategy/Orbit Lob (Muzzle Anchor)")]
public class OrbitLobAimStrategy : AimStrategy
{
    [Header("Pull")]
    [SerializeField] private float maxPullDistance = 1.5f; // world units
    [SerializeField] private float deadZonePull = 0.05f;

    [Header("Direction")]
    [Tooltip("If true, ensure the launch goes upward (y >= 0). Does NOT touch x.")]
    [SerializeField] private bool forceUpward = true;

    [Header("Gravity Flip (Smooth UX)")]
    [Tooltip("Dead zone around muzzle X where we DO NOT flip sides. Bigger = less twitchy.")]
    [SerializeField] private float sideDeadZone = 0.25f;

    [Tooltip("If true: right side = invertGravity. If false: left side = invertGravity.")]
    [SerializeField] private bool invertOnRightSide = true;

    // hysteresis memory
    private bool lastRightSide;

    public override AimResult Evaluate(Vector2 muzzlePos, Vector2 pointerWorld, bool isAiming, int heldLevel)
    {
        // Left/right with hysteresis using X (not Y)
        float dx = pointerWorld.x - muzzlePos.x;

        if (dx > sideDeadZone) lastRightSide = true;
        else if (dx < -sideDeadZone) lastRightSide = false;

        bool invertGravity = invertOnRightSide ? lastRightSide : !lastRightSide;

        if (!isAiming)
            return new AimResult(Vector2.up, 0f, invertGravity);

        // Drag measured from the muzzle (anchor is the launcher)
        Vector2 drag = pointerWorld - muzzlePos;
        float dist = drag.magnitude;

        if (dist < deadZonePull)
            return new AimResult(Vector2.up, 0f, invertGravity);

        float power01 = Mathf.Clamp01(dist / Mathf.Max(0.001f, maxPullDistance));

        // Catapult direction: opposite of drag
        Vector2 dir = muzzlePos - pointerWorld;

        if (forceUpward && dir.y < 0f)
            dir.y = -dir.y;

        if (dir.sqrMagnitude < 1e-6f)
            dir = Vector2.up;

        dir.Normalize();

        return new AimResult(dir, power01, invertGravity);
    }
}
